using System.Text;
using SimpleWindowsInstallerCleaner.Interop;
using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

/// <summary>
/// Queries the Windows Installer API to build the complete set of registered
/// .msi and .msp files across all installation contexts. This service only
/// talks to the MSI API — it does not touch the filesystem.
/// </summary>
public sealed class InstallerQueryService : IInstallerQueryService
{
    /// <summary>
    /// SID meaning "all users". When passed to MsiEnumProductsEx /
    /// MsiEnumPatchesEx / MsiEnumComponentsEx, the API enumerates across
    /// every user profile on the machine. Requires admin elevation.
    /// </summary>
    private const string AllUsersSid = "s-1-1-0";

    /// <summary>
    /// A GUID is 38 chars ({xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}) plus a
    /// null terminator. We allocate 39 to be safe.
    /// </summary>
    private const int GuidBufferLength = 39;

    /// <summary>
    /// The folder prefix we look for when deciding whether a component path
    /// is inside the Windows Installer cache.
    /// </summary>
    private static readonly string InstallerFolderPrefix =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer")
        + Path.DirectorySeparatorChar;

    /// <inheritdoc />
    public Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Run the entire enumeration on a thread-pool thread so the caller
        // (typically the UI thread) stays responsive.
        return Task.Run(() => GetRegisteredPackagesCore(progress, cancellationToken), cancellationToken);
    }

    private IReadOnlyList<RegisteredPackage> GetRegisteredPackagesCore(
        IProgress<string>? progress,
        CancellationToken ct)
    {
        // Key = case-insensitive local package path → value = RegisteredPackage.
        // We use a dictionary so that if two different code paths claim the
        // same file, we keep the first entry (which will be the non-Adobe one
        // from the primary enumeration).
        var claimed = new Dictionary<string, RegisteredPackage>(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------
        // Phase 1: enumerate all products via MsiEnumProductsEx
        // ------------------------------------------------------------------
        progress?.Report("Enumerating installed products...");

        var products = EnumerateProducts(ct);

        progress?.Report($"Found {products.Count} installed product(s). Scanning local packages...");

        foreach (var (productCode, userSid, context) in products)
        {
            ct.ThrowIfCancellationRequested();

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName);

            // Get the product's cached .msi path
            var localPackage = GetProductProperty(productCode, userSid, context, MsiInstallProperty.LocalPackage);

            if (!string.IsNullOrEmpty(localPackage))
            {
                progress?.Report(productName.Length > 0 ? productName : productCode);
                claimed.TryAdd(localPackage, new RegisteredPackage(localPackage, productName, productCode, IsAdobeWarning: false));
            }

            // Enumerate patches for this product
            var patches = EnumeratePatches(productCode, userSid, context, ct);

            foreach (var (patchCode, patchUserSid, patchContext) in patches)
            {
                ct.ThrowIfCancellationRequested();

                var patchPath = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

                if (!string.IsNullOrEmpty(patchPath))
                {
                    claimed.TryAdd(patchPath, new RegisteredPackage(patchPath, productName, productCode, IsAdobeWarning: false));
                }
            }
        }

        // ------------------------------------------------------------------
        // Phase 2: component-registered packages (Adobe workaround)
        //
        // Some vendors (notably Adobe) register cached packages via
        // component records rather than INSTALLPROPERTY_LOCALPACKAGE.
        // We enumerate all components and check whether their installed
        // path points into %windir%\Installer.
        // ------------------------------------------------------------------
        progress?.Report("Scanning component registrations...");

        var componentPaths = EnumerateComponentPathsInInstallerFolder(ct);

        foreach (var (componentPath, productCode, userSid, context) in componentPaths)
        {
            ct.ThrowIfCancellationRequested();

            // If the primary enumeration already claimed this file, skip it.
            if (claimed.ContainsKey(componentPath))
                continue;

            var productName = GetProductProperty(productCode, userSid, context, MsiInstallProperty.ProductName);
            var isAdobe = productName.Contains("Adobe", StringComparison.OrdinalIgnoreCase);

            claimed.TryAdd(componentPath, new RegisteredPackage(componentPath, productName, productCode, IsAdobeWarning: isAdobe));
        }

        progress?.Report($"Scan complete. {claimed.Count} registered package(s) found.");

        return claimed.Values.ToList().AsReadOnly();
    }

    // ==================================================================
    //  Product enumeration
    // ==================================================================

    /// <summary>
    /// Enumerates all installed products across all users and contexts.
    /// Returns a list of (productCode, userSid, context) tuples.
    /// </summary>
    private static List<(string ProductCode, string? UserSid, MsiInstallContext Context)> EnumerateProducts(
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var productCode = new StringBuilder(GuidBufferLength);

        for (uint index = 0; ; index++)
        {
            ct.ThrowIfCancellationRequested();

            productCode.Clear();
            productCode.EnsureCapacity(GuidBufferLength);
            uint sidLen = 0;

            var error = MsiNativeMethods.MsiEnumProductsEx(
                szProductCode: null,
                szUserSid: AllUsersSid,
                dwContext: MsiInstallContext.All,
                dwIndex: index,
                szInstalledProductCode: productCode,
                pdwInstalledContext: out var installedContext,
                szSid: null,
                pcchSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                // We don't need the SID string itself for the primary query;
                // for per-machine products the SID is empty. For per-user
                // products we re-query the SID properly.
                var sid = GetEnumeratedSid(productCode.ToString(), installedContext);
                results.Add((productCode.ToString(), sid, installedContext));
            }
            // Silently skip products that return other errors (e.g. bad config).
        }

        return results;
    }

    /// <summary>
    /// For per-user products, retrieves the user SID that owns the installation.
    /// For per-machine products, returns <c>null</c>.
    /// </summary>
    private static string? GetEnumeratedSid(string productCode, MsiInstallContext context)
    {
        if (context == MsiInstallContext.Machine)
            return null;

        // Re-enumerate to get the SID. We call MsiEnumProductsEx with this
        // specific product code to get its SID.
        var pc = new StringBuilder(GuidBufferLength);
        uint sidLen = 0;

        // First call: get required SID buffer size.
        MsiNativeMethods.MsiEnumProductsEx(
            szProductCode: productCode,
            szUserSid: AllUsersSid,
            dwContext: context,
            dwIndex: 0,
            szInstalledProductCode: pc,
            pdwInstalledContext: out _,
            szSid: null,
            pcchSid: ref sidLen);

        if (sidLen == 0)
            return null;

        sidLen++; // space for null terminator
        var sidBuffer = new StringBuilder((int)sidLen);

        var error = MsiNativeMethods.MsiEnumProductsEx(
            szProductCode: productCode,
            szUserSid: AllUsersSid,
            dwContext: context,
            dwIndex: 0,
            szInstalledProductCode: pc,
            pdwInstalledContext: out _,
            szSid: sidBuffer,
            pcchSid: ref sidLen);

        return error == MsiError.Success ? sidBuffer.ToString() : null;
    }

    // ==================================================================
    //  Patch enumeration
    // ==================================================================

    /// <summary>
    /// Enumerates all patches applied to a given product.
    /// </summary>
    private static List<(string PatchCode, string? UserSid, MsiInstallContext Context)> EnumeratePatches(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        CancellationToken ct)
    {
        var results = new List<(string, string?, MsiInstallContext)>();
        var patchCode = new StringBuilder(GuidBufferLength);
        var targetProductCode = new StringBuilder(GuidBufferLength);

        for (uint index = 0; ; index++)
        {
            ct.ThrowIfCancellationRequested();

            patchCode.Clear();
            patchCode.EnsureCapacity(GuidBufferLength);
            targetProductCode.Clear();
            targetProductCode.EnsureCapacity(GuidBufferLength);
            uint sidLen = 0;

            var error = MsiNativeMethods.MsiEnumPatchesEx(
                szProductCode: productCode,
                szUserSid: userSid,
                dwContext: context,
                dwFilter: MsiPatchFilter.All,
                dwIndex: index,
                szPatchCode: patchCode,
                szTargetProductCode: targetProductCode,
                pdwTargetProductContext: out var patchContext,
                szTargetUserSid: null,
                pcchTargetUserSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                // The patch inherits the user SID and context from its
                // target product in most cases.
                results.Add((patchCode.ToString(), userSid, patchContext));
            }
        }

        return results;
    }

    // ==================================================================
    //  Component path enumeration (Adobe workaround)
    // ==================================================================

    /// <summary>
    /// Enumerates all components across all users, retrieves each component's
    /// installed path, and returns only those whose path falls inside
    /// <c>%windir%\Installer\</c>.
    /// </summary>
    private static List<(string Path, string ProductCode, string? UserSid, MsiInstallContext Context)>
        EnumerateComponentPathsInInstallerFolder(CancellationToken ct)
    {
        var results = new List<(string, string, string?, MsiInstallContext)>();
        var componentCode = new StringBuilder(GuidBufferLength);

        // We also need to know which product owns the component so we can
        // retrieve its name. MsiEnumComponentsEx does not give us that
        // directly; we must call MsiGetComponentPathEx to discover it.

        for (uint index = 0; ; index++)
        {
            ct.ThrowIfCancellationRequested();

            componentCode.Clear();
            componentCode.EnsureCapacity(GuidBufferLength);
            uint sidLen = 0;

            var error = MsiNativeMethods.MsiEnumComponentsEx(
                szUserSid: AllUsersSid,
                dwContext: MsiInstallContext.All,
                dwIndex: index,
                szInstalledComponentCode: componentCode,
                pdwInstalledContext: out var compContext,
                szSid: null,
                pcchSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error != MsiError.Success && error != MsiError.MoreData)
                continue;

            // Retrieve the component path using the double-call pattern.
            var code = componentCode.ToString();
            var compSid = compContext == MsiInstallContext.Machine ? null : GetComponentSid(code, compContext);
            var path = GetComponentPath(code, compSid, compContext);

            if (string.IsNullOrEmpty(path))
                continue;

            if (!path.StartsWith(InstallerFolderPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            // We need the owning product code. Look it up by re-enumerating
            // products that provide this component.
            var owningProduct = FindOwningProduct(code, compSid, compContext);

            results.Add((path, owningProduct ?? string.Empty, compSid, compContext));
        }

        return results;
    }

    /// <summary>
    /// Gets the SID for a per-user component by re-enumerating with the
    /// specific component's context.
    /// </summary>
    private static string? GetComponentSid(string componentCode, MsiInstallContext context)
    {
        var cc = new StringBuilder(GuidBufferLength);
        uint sidLen = 0;

        MsiNativeMethods.MsiEnumComponentsEx(
            szUserSid: AllUsersSid,
            dwContext: context,
            dwIndex: 0,
            szInstalledComponentCode: cc,
            pdwInstalledContext: out _,
            szSid: null,
            pcchSid: ref sidLen);

        if (sidLen == 0)
            return null;

        sidLen++;
        var sidBuffer = new StringBuilder((int)sidLen);

        var error = MsiNativeMethods.MsiEnumComponentsEx(
            szUserSid: AllUsersSid,
            dwContext: context,
            dwIndex: 0,
            szInstalledComponentCode: cc,
            pdwInstalledContext: out _,
            szSid: sidBuffer,
            pcchSid: ref sidLen);

        return error == MsiError.Success ? sidBuffer.ToString() : null;
    }

    /// <summary>
    /// Retrieves the installed path for a component using the double-call
    /// buffer pattern.
    /// </summary>
    private static string? GetComponentPath(
        string componentCode,
        string? userSid,
        MsiInstallContext context)
    {
        uint bufferLen = 0;

        // First call: get required buffer size.
        var state = MsiNativeMethods.MsiGetComponentPathEx(
            szProductCode: null,
            szComponentCode: componentCode,
            szUserSid: userSid,
            dwContext: context,
            lpOutPathBuffer: null,
            pcchOutPathBuffer: ref bufferLen);

        // state >= 1 means a valid path exists (INSTALLSTATE_LOCAL = 3,
        // INSTALLSTATE_SOURCE = 1). We accept either.
        if (state < MsiNativeMethods.InstallStateSource || bufferLen == 0)
            return null;

        bufferLen++; // space for null terminator
        var buffer = new StringBuilder((int)bufferLen);

        state = MsiNativeMethods.MsiGetComponentPathEx(
            szProductCode: null,
            szComponentCode: componentCode,
            szUserSid: userSid,
            dwContext: context,
            lpOutPathBuffer: buffer,
            pcchOutPathBuffer: ref bufferLen);

        return state >= MsiNativeMethods.InstallStateSource ? buffer.ToString() : null;
    }

    /// <summary>
    /// Finds the product code that owns a given component by iterating
    /// through all products and checking if they provide this component.
    /// </summary>
    private static string? FindOwningProduct(
        string componentCode,
        string? userSid,
        MsiInstallContext context)
    {
        var productCode = new StringBuilder(GuidBufferLength);

        for (uint index = 0; ; index++)
        {
            productCode.Clear();
            productCode.EnsureCapacity(GuidBufferLength);
            uint sidLen = 0;

            var error = MsiNativeMethods.MsiEnumProductsEx(
                szProductCode: null,
                szUserSid: userSid ?? AllUsersSid,
                dwContext: context,
                dwIndex: index,
                szInstalledProductCode: productCode,
                pdwInstalledContext: out _,
                szSid: null,
                pcchSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error != MsiError.Success && error != MsiError.MoreData)
                continue;

            // Check if this product provides the component.
            uint pathLen = 0;
            var state = MsiNativeMethods.MsiGetComponentPathEx(
                szProductCode: productCode.ToString(),
                szComponentCode: componentCode,
                szUserSid: userSid,
                dwContext: context,
                lpOutPathBuffer: null,
                pcchOutPathBuffer: ref pathLen);

            if (state >= MsiNativeMethods.InstallStateSource)
                return productCode.ToString();
        }

        return null;
    }

    // ==================================================================
    //  Property retrieval helpers (double-call buffer pattern)
    // ==================================================================

    /// <summary>
    /// Retrieves a product property using the double-call buffer pattern.
    /// Returns an empty string if the property cannot be read.
    /// </summary>
    private static string GetProductProperty(
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        // First call: get required buffer size.
        var error = MsiNativeMethods.MsiGetProductInfoEx(
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: null,
            pcchValue: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

        bufferLen++; // space for null terminator
        var buffer = new StringBuilder((int)bufferLen);

        error = MsiNativeMethods.MsiGetProductInfoEx(
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

        return error == MsiError.Success ? buffer.ToString() : string.Empty;
    }

    /// <summary>
    /// Retrieves a patch property using the double-call buffer pattern.
    /// Returns an empty string if the property cannot be read.
    /// </summary>
    private static string GetPatchProperty(
        string patchCode,
        string productCode,
        string? userSid,
        MsiInstallContext context,
        string propertyName)
    {
        uint bufferLen = 0;

        // First call: get required buffer size.
        var error = MsiNativeMethods.MsiGetPatchInfoEx(
            szPatchCode: patchCode,
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: null,
            pcchValue: ref bufferLen);

        if (error != MsiError.Success && error != MsiError.MoreData)
            return string.Empty;

        if (bufferLen == 0)
            return string.Empty;

        bufferLen++; // space for null terminator
        var buffer = new StringBuilder((int)bufferLen);

        error = MsiNativeMethods.MsiGetPatchInfoEx(
            szPatchCode: patchCode,
            szProductCode: productCode,
            szUserSid: userSid,
            dwContext: context,
            szProperty: propertyName,
            szValue: buffer,
            pcchValue: ref bufferLen);

        return error == MsiError.Success ? buffer.ToString() : string.Empty;
    }
}
