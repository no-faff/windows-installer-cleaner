using System.Runtime.InteropServices;
using System.Text;

namespace SimpleWindowsInstallerCleaner.Interop;

/// <summary>
/// P/Invoke declarations for the Windows Installer API (msi.dll).
/// All methods use the Unicode ("W") entry points.
/// </summary>
internal static partial class MsiNativeMethods
{
    private const string MsiLib = "msi.dll";

    /// <summary>
    /// Enumerates products installed on the system across all user contexts.
    /// </summary>
    /// <param name="szProductCode">
    /// Optional product code filter. Pass <c>null</c> to enumerate all products.
    /// </param>
    /// <param name="szUserSid">
    /// User SID to enumerate for. Pass <c>"s-1-1-0"</c> for all users,
    /// or <c>null</c> for the current user only.
    /// </param>
    /// <param name="dwContext">Installation context filter flags.</param>
    /// <param name="dwIndex">Zero-based index into the enumeration.</param>
    /// <param name="szInstalledProductCode">
    /// Buffer receiving the product code GUID. Must be at least 39 chars.
    /// </param>
    /// <param name="pdwInstalledContext">Receives the installation context of the product.</param>
    /// <param name="szSid">Buffer receiving the user SID, or <c>null</c>.</param>
    /// <param name="pcchSid">
    /// On input, size of <paramref name="szSid"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>
    /// <see cref="MsiError.Success"/>, <see cref="MsiError.MoreData"/>,
    /// or <see cref="MsiError.NoMoreItems"/>.
    /// </returns>
    [DllImport(MsiLib, EntryPoint = "MsiEnumProductsExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiEnumProductsEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwIndex,
        StringBuilder? szInstalledProductCode,
        out MsiInstallContext pdwInstalledContext,
        StringBuilder? szSid,
        ref uint pcchSid);

    /// <summary>
    /// Retrieves a property value for an installed product.
    /// Uses the double-call pattern: first call with <c>null</c> buffer to
    /// get the required size, then allocate and call again.
    /// </summary>
    /// <param name="szProductCode">Product code GUID.</param>
    /// <param name="szUserSid">
    /// User SID, or <c>null</c> for per-machine products.
    /// </param>
    /// <param name="dwContext">Installation context of the product.</param>
    /// <param name="szProperty">Property name string.</param>
    /// <param name="szValue">Buffer receiving the property value.</param>
    /// <param name="pcchValue">
    /// On input, size of <paramref name="szValue"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>
    /// <see cref="MsiError.Success"/>, <see cref="MsiError.MoreData"/>,
    /// <see cref="MsiError.UnknownProduct"/>, or <see cref="MsiError.UnknownProperty"/>.
    /// </returns>
    [DllImport(MsiLib, EntryPoint = "MsiGetProductInfoExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiGetProductInfoEx(
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        StringBuilder? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Enumerates patches applied to products across all user contexts.
    /// </summary>
    /// <param name="szProductCode">
    /// Optional product code filter. Pass <c>null</c> to enumerate patches
    /// for all products.
    /// </param>
    /// <param name="szUserSid">
    /// User SID to enumerate for. Pass <c>"s-1-1-0"</c> for all users,
    /// or <c>null</c> for the current user only.
    /// </param>
    /// <param name="dwContext">Installation context filter flags.</param>
    /// <param name="dwFilter">Patch state filter flags.</param>
    /// <param name="dwIndex">Zero-based index into the enumeration.</param>
    /// <param name="szPatchCode">
    /// Buffer receiving the patch code GUID. Must be at least 39 chars.
    /// </param>
    /// <param name="szTargetProductCode">
    /// Buffer receiving the target product code GUID. Must be at least 39 chars.
    /// </param>
    /// <param name="pdwTargetProductContext">Receives the installation context.</param>
    /// <param name="szTargetUserSid">Buffer receiving the user SID, or <c>null</c>.</param>
    /// <param name="pcchTargetUserSid">
    /// On input, size of <paramref name="szTargetUserSid"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>
    /// <see cref="MsiError.Success"/>, <see cref="MsiError.MoreData"/>,
    /// or <see cref="MsiError.NoMoreItems"/>.
    /// </returns>
    [DllImport(MsiLib, EntryPoint = "MsiEnumPatchesExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiEnumPatchesEx(
        string? szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        MsiPatchFilter dwFilter,
        uint dwIndex,
        StringBuilder? szPatchCode,
        StringBuilder? szTargetProductCode,
        out MsiInstallContext pdwTargetProductContext,
        StringBuilder? szTargetUserSid,
        ref uint pcchTargetUserSid);

    /// <summary>
    /// Retrieves a property value for an applied patch.
    /// </summary>
    /// <param name="szPatchCode">Patch code GUID.</param>
    /// <param name="szProductCode">Product code the patch is applied to.</param>
    /// <param name="szUserSid">
    /// User SID, or <c>null</c> for per-machine products.
    /// </param>
    /// <param name="dwContext">Installation context.</param>
    /// <param name="szProperty">Property name string.</param>
    /// <param name="szValue">Buffer receiving the property value.</param>
    /// <param name="pcchValue">
    /// On input, size of <paramref name="szValue"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>
    /// <see cref="MsiError.Success"/>, <see cref="MsiError.MoreData"/>,
    /// or <see cref="MsiError.UnknownProduct"/>.
    /// </returns>
    [DllImport(MsiLib, EntryPoint = "MsiGetPatchInfoExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiGetPatchInfoEx(
        string szPatchCode,
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        StringBuilder? szValue,
        ref uint pcchValue);

    /// <summary>
    /// Enumerates component GUIDs installed across all user contexts.
    /// </summary>
    /// <param name="szUserSid">
    /// User SID to enumerate for. Pass <c>"s-1-1-0"</c> for all users.
    /// </param>
    /// <param name="dwContext">Installation context filter flags.</param>
    /// <param name="dwIndex">Zero-based index into the enumeration.</param>
    /// <param name="szInstalledComponentCode">
    /// Buffer receiving the component code GUID. Must be at least 39 chars.
    /// </param>
    /// <param name="pdwInstalledContext">Receives the installation context.</param>
    /// <param name="szSid">Buffer receiving the user SID, or <c>null</c>.</param>
    /// <param name="pcchSid">
    /// On input, size of <paramref name="szSid"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>
    /// <see cref="MsiError.Success"/> or <see cref="MsiError.NoMoreItems"/>.
    /// </returns>
    [DllImport(MsiLib, EntryPoint = "MsiEnumComponentsExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiEnumComponentsEx(
        string? szUserSid,
        MsiInstallContext dwContext,
        uint dwIndex,
        StringBuilder? szInstalledComponentCode,
        out MsiInstallContext pdwInstalledContext,
        StringBuilder? szSid,
        ref uint pcchSid);

    /// <summary>
    /// Retrieves the installed path of a component, including context-aware
    /// lookups across all user scopes.
    /// </summary>
    /// <param name="szProductCode">
    /// Product code GUID, or <c>null</c> to query any product providing the component.
    /// </param>
    /// <param name="szComponentCode">Component code GUID.</param>
    /// <param name="szUserSid">
    /// User SID, or <c>null</c> for the current user / per-machine.
    /// </param>
    /// <param name="dwContext">Installation context filter flags.</param>
    /// <param name="lpOutPathBuffer">Buffer receiving the component path.</param>
    /// <param name="pcchOutPathBuffer">
    /// On input, size of <paramref name="lpOutPathBuffer"/> in chars (excluding null).
    /// On output, number of chars written (excluding null).
    /// </param>
    /// <returns>An INSTALLSTATE value. Values >= 0 indicate a valid path.</returns>
    [DllImport(MsiLib, EntryPoint = "MsiGetComponentPathExW", CharSet = CharSet.Unicode)]
    public static extern int MsiGetComponentPathEx(
        string? szProductCode,
        string szComponentCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        StringBuilder? lpOutPathBuffer,
        ref uint pcchOutPathBuffer);

    /// <summary>
    /// INSTALLSTATE values returned by MsiGetComponentPathEx.
    /// Values >= 0 mean the component path is valid.
    /// </summary>
    public const int InstallStateLocal = 3;
    public const int InstallStateSource = 1;
}
