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
    /// SIDs are typically ~45 chars (e.g. S-1-5-21-xxx-xxx-xxx-xxxx).
    /// Pre-allocating 256 avoids re-enumerating just to get the SID.
    /// </summary>
    private const int SidBufferLength = 256;

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
                claimed.TryAdd(localPackage, new RegisteredPackage(localPackage, productName, productCode));
            }

            // Enumerate patches for this product
            var patches = EnumeratePatches(productCode, userSid, context, ct);

            foreach (var (patchCode, patchUserSid, patchContext) in patches)
            {
                ct.ThrowIfCancellationRequested();

                var patchPath = GetPatchProperty(patchCode, productCode, patchUserSid, patchContext, MsiInstallProperty.LocalPackage);

                if (!string.IsNullOrEmpty(patchPath))
                {
                    claimed.TryAdd(patchPath, new RegisteredPackage(patchPath, productName, productCode));
                }
            }
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
        var sidBuffer = new StringBuilder(SidBufferLength);

        for (uint index = 0; ; index++)
        {
            ct.ThrowIfCancellationRequested();

            productCode.Clear();
            productCode.EnsureCapacity(GuidBufferLength);
            sidBuffer.Clear();
            sidBuffer.EnsureCapacity(SidBufferLength);
            uint sidLen = (uint)(SidBufferLength - 1);

            var error = MsiNativeMethods.MsiEnumProductsEx(
                szProductCode: null,
                szUserSid: AllUsersSid,
                dwContext: MsiInstallContext.All,
                dwIndex: index,
                szInstalledProductCode: productCode,
                pdwInstalledContext: out var installedContext,
                szSid: sidBuffer,
                pcchSid: ref sidLen);

            if (error == MsiError.NoMoreItems)
                break;

            if (error == MsiError.AccessDenied)
                throw new UnauthorizedAccessException(
                    "Access denied enumerating installed products. Run as administrator.");

            if (error == MsiError.Success || error == MsiError.MoreData)
            {
                var sid = (installedContext != MsiInstallContext.Machine && sidLen > 0)
                    ? sidBuffer.ToString()
                    : null;
                results.Add((productCode.ToString(), sid, installedContext));
            }
            // Skip products with other errors (e.g. bad config) but don't spin
            // forever — if we've seen too many consecutive failures, bail out.
            else if (results.Count == 0 && index > 10)
            {
                throw new InvalidOperationException(
                    $"Windows Installer API returned error {error}. Unable to enumerate products.");
            }
        }

        return results;
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

            if (error == MsiError.AccessDenied)
                break; // skip patches we can't access

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
