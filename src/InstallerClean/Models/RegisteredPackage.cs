namespace InstallerClean.Models;

/// <summary>
/// A cached installer package (.msi or .msp) still registered with the
/// Windows Installer API — i.e. still needed by an installed product.
/// PatchState: 0 = not a patch, 1 = applied, 2 = superseded, 4 = obsoleted.
/// </summary>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    int PatchState = 0,
    bool IsRemovable = false);
