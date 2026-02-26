using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class ExclusionService : IExclusionService
{
    public FilteredResult ApplyFilters(
        IReadOnlyList<OrphanedFile> files,
        IReadOnlyList<string> filters,
        IMsiFileInfoService? infoService = null)
    {
        if (filters.Count == 0)
            return new FilteredResult(files, Array.Empty<OrphanedFile>());

        var actionable = new List<OrphanedFile>();
        var excluded = new List<OrphanedFile>();

        foreach (var file in files)
        {
            // Always check filename first (fast)
            var isExcluded = filters.Any(f =>
                file.FileName.Contains(f, StringComparison.OrdinalIgnoreCase));

            // If not excluded by filename and we have an info service, check metadata
            if (!isExcluded && infoService is not null)
            {
                var info = infoService.GetSummaryInfo(file.FullPath);
                if (info is not null)
                {
                    isExcluded = filters.Any(f =>
                        info.Author.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                        info.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                        info.Subject.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                        info.DigitalSignature.Contains(f, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (isExcluded)
                excluded.Add(file);
            else
                actionable.Add(file);
        }

        return new FilteredResult(actionable.AsReadOnly(), excluded.AsReadOnly());
    }
}
