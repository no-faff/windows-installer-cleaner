namespace SimpleWindowsInstallerCleaner.Interop;

/// <summary>
/// Windows Installer API error codes returned by Msi* functions.
/// </summary>
public static class MsiError
{
    public const uint Success = 0;
    public const uint MoreData = 234;
    public const uint NoMoreItems = 259;
    public const uint UnknownProduct = 1605;
    public const uint UnknownProperty = 1608;
    public const uint BadConfiguration = 1610;
    public const uint InvalidParameter = 87;
    public const uint FunctionFailed = 1627;
}

/// <summary>
/// Installation context flags for MsiEnumProductsEx / MsiEnumPatchesEx /
/// MsiEnumComponentsEx.
/// </summary>
[Flags]
public enum MsiInstallContext : uint
{
    /// <summary>Per-machine installation context.</summary>
    Machine = 0x00000004,

    /// <summary>Per-user unmanaged installation context.</summary>
    UserUnmanaged = 0x00000001,

    /// <summary>Per-user managed installation context.</summary>
    UserManaged = 0x00000002,

    /// <summary>All installation contexts.</summary>
    All = Machine | UserUnmanaged | UserManaged
}

/// <summary>
/// Filter flags for MsiEnumPatchesEx.
/// </summary>
[Flags]
public enum MsiPatchFilter : uint
{
    /// <summary>Include applied patches.</summary>
    Applied = 0x00000001,

    /// <summary>All patch states.</summary>
    All = Applied
}

/// <summary>
/// Install property name strings used with MsiGetProductInfoEx and
/// MsiGetPatchInfoEx.
/// </summary>
public static class MsiInstallProperty
{
    /// <summary>Local cached package path in %windir%\Installer.</summary>
    public const string LocalPackage = "LocalPackage";

    /// <summary>Display name of the installed product.</summary>
    public const string ProductName = "ProductName";

    /// <summary>Publisher / manufacturer of the installed product.</summary>
    public const string Publisher = "Publisher";
}

/// <summary>
/// Component registration source for MsiGetComponentPath.
/// </summary>
public static class MsiComponentClient
{
    /// <summary>
    /// Pass as the product code to MsiGetComponentPathEx to query any
    /// product that provides the component.
    /// </summary>
    public const string AnyProduct = null!;
}
