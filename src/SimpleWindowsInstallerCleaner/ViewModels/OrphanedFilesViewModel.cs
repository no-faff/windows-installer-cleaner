using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public sealed class OrphanedFilesViewModel
{
    public IReadOnlyList<OrphanedFile> ActionableFiles { get; }
    public IReadOnlyList<OrphanedFile> ExcludedFiles { get; }
    public bool HasExcludedFiles => ExcludedFiles.Count > 0;
    public string Summary { get; }

    public OrphanedFilesViewModel(
        IReadOnlyList<OrphanedFile> actionableFiles,
        IReadOnlyList<OrphanedFile> excludedFiles)
    {
        ActionableFiles = actionableFiles.OrderByDescending(f => f.SizeBytes).ToList();
        ExcludedFiles = excludedFiles.OrderByDescending(f => f.SizeBytes).ToList();

        var orphanedSize = FormatSize(actionableFiles.Sum(f => f.SizeBytes));
        var excludedSize = FormatSize(excludedFiles.Sum(f => f.SizeBytes));

        Summary = excludedFiles.Count > 0
            ? $"{actionableFiles.Count} orphaned ({orphanedSize}) · {excludedFiles.Count} excluded ({excludedSize})"
            : $"{actionableFiles.Count} orphaned ({orphanedSize})";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
