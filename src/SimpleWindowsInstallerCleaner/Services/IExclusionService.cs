using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface IExclusionService
{
    FilteredResult ApplyFilters(IReadOnlyList<OrphanedFile> files, IReadOnlyList<string> filters);
}

public record FilteredResult(
    IReadOnlyList<OrphanedFile> Actionable,
    IReadOnlyList<OrphanedFile> Excluded);
