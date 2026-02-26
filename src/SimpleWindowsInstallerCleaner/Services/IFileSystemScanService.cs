using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface IFileSystemScanService
{
    /// <summary>
    /// Enumerates C:\Windows\Installer, queries the MSI API to discover
    /// which files are registered, and returns a ScanResult with orphaned
    /// files and registered file statistics.
    /// </summary>
    Task<ScanResult> ScanAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
