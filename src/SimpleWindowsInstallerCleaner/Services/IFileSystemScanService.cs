using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface IFileSystemScanService
{
    /// <summary>
    /// Enumerates C:\Windows\Installer, queries the MSI API to discover
    /// which files are registered, and returns those that are not.
    /// </summary>
    Task<IReadOnlyList<OrphanedFile>> FindOrphanedFilesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
