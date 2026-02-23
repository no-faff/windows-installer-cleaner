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
/// <param name="IsAdobeWarning">
/// <c>true</c> if this package was claimed only via component registration
/// and the owning product name contains "Adobe". These files may appear
/// orphaned under simpler enumeration but are likely still needed.
/// </param>
public record RegisteredPackage(
    string LocalPackagePath,
    string ProductName,
    string ProductCode,
    bool IsAdobeWarning);
