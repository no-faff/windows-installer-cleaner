using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

/// <summary>
/// Queries the Windows Installer API to enumerate all registered (still-needed)
/// cached packages in <c>%windir%\Installer</c>.
/// </summary>
public interface IInstallerQueryService
{
    /// <summary>
    /// Enumerates all .msi and .msp files that are registered with the
    /// Windows Installer API across all installation contexts (per-machine,
    /// per-user managed and per-user unmanaged for all users).
    /// </summary>
    /// <param name="progress">
    /// Optional progress reporter. Receives human-readable status messages
    /// such as "Enumerating products..." or product names as they are processed.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to abort the scan early.
    /// </param>
    /// <returns>
    /// A read-only list of all registered packages.
    /// </returns>
    Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
