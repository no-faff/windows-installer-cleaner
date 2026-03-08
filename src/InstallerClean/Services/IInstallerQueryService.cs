using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IInstallerQueryService
{
    Task<IReadOnlyList<RegisteredPackage>> GetRegisteredPackagesAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
