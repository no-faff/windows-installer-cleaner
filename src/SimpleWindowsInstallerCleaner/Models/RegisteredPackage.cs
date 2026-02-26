namespace SimpleWindowsInstallerCleaner.Models;

/// <summary>
/// Represents a cached installer package (.msi or .msp) that is registered
/// with the Windows Installer API — i.e. still needed by an installed product.
/// </summary>
/// <param name="LocalPackagePath">
/// Full path to the cached package file in <c>%windir%\Installer</c>.
/// </param>
/// <param name="ProductName">
/// Display name of the product that owns this package, or an empty string
/// if the name could not be retrieved.
/// </param>
/// <param name="ProductCode">
/// Product code GUID string (e.g. <c>{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}</c>).
/// </param>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode);
