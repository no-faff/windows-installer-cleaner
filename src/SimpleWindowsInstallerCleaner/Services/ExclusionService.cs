using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class ExclusionService : IExclusionService
{
    public FilteredResult ApplyFilters(IReadOnlyList<OrphanedFile> files, IReadOnlyList<string> filters)
    {
        if (filters.Count == 0)
            return new FilteredResult(files, Array.Empty<OrphanedFile>());

        var actionable = new List<OrphanedFile>();
        var excluded = new List<OrphanedFile>();

        foreach (var file in files)
        {
            var isExcluded = filters.Any(f =>
                file.FileName.Contains(f, StringComparison.OrdinalIgnoreCase));

            if (isExcluded)
                excluded.Add(file);
            else
                actionable.Add(file);
        }

        return new FilteredResult(actionable.AsReadOnly(), excluded.AsReadOnly());
    }
}
