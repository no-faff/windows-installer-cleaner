using System.Runtime.InteropServices;
using System.Text;

namespace InstallerClean.Interop;

/// <summary>
/// P/Invoke declarations for the Windows Installer API (msi.dll).
/// All methods use the Unicode ("W") entry points.
/// </summary>
internal static partial class MsiNativeMethods
{
    private const string MsiLib = "msi.dll";

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

    [DllImport(MsiLib, EntryPoint = "MsiGetProductInfoExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiGetProductInfoEx(
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        StringBuilder? szValue,
        ref uint pcchValue);

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

    [DllImport(MsiLib, EntryPoint = "MsiGetPatchInfoExW", CharSet = CharSet.Unicode)]
    public static extern uint MsiGetPatchInfoEx(
        string szPatchCode,
        string szProductCode,
        string? szUserSid,
        MsiInstallContext dwContext,
        string szProperty,
        StringBuilder? szValue,
        ref uint pcchValue);

    [DllImport(MsiLib, EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode)]
    public static extern uint MsiGetSummaryInformation(
        IntPtr hDatabase,
        string? szDatabasePath,
        uint uiUpdateCount,
        out IntPtr phSummaryInfo);

    [DllImport(MsiLib, EntryPoint = "MsiSummaryInfoGetPropertyW", CharSet = CharSet.Unicode)]
    public static extern uint MsiSummaryInfoGetProperty(
        IntPtr hSummaryInfo,
        uint uiProperty,
        out uint puiDataType,
        out int piValue,
        IntPtr pftValue,
        StringBuilder? szValueBuf,
        ref uint pcchValueBuf);

    [DllImport(MsiLib, EntryPoint = "MsiCloseHandle")]
    public static extern uint MsiCloseHandle(IntPtr hAny);

}
