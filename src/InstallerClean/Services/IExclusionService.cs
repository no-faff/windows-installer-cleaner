using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IExclusionService
{
    FilteredResult ApplyFilters(
        IReadOnlyList<OrphanedFile> files,
        IReadOnlyList<string> filters,
        IMsiFileInfoService? infoService = null);
}

public record FilteredResult(
    IReadOnlyList<OrphanedFile> Actionable,
    IReadOnlyList<OrphanedFile> Excluded);
