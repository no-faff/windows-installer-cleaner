using CommunityToolkit.Mvvm.ComponentModel;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class OrphanedFilesViewModel : ObservableObject
{
    private readonly IMsiFileInfoService _infoService;
    private readonly Dictionary<string, MsiSummaryInfo?> _cache = new();

    public IReadOnlyList<OrphanedFile> ActionableFiles { get; }
    public IReadOnlyList<OrphanedFile> ExcludedFiles { get; }
    public bool HasExcludedFiles => ExcludedFiles.Count > 0;
    public string Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private OrphanedFile? _selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private MsiSummaryInfo? _selectedDetails;

    public bool HasSelection => SelectedFile is not null;
    public bool ShowDetails => SelectedFile is not null && SelectedDetails is not null;
    public bool ShowNoMetadata => SelectedFile is not null && SelectedDetails is null;

    public OrphanedFilesViewModel(
        IReadOnlyList<OrphanedFile> actionableFiles,
        IReadOnlyList<OrphanedFile> excludedFiles,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;

        ActionableFiles = actionableFiles.OrderByDescending(f => f.SizeBytes).ToList();
        ExcludedFiles = excludedFiles.OrderByDescending(f => f.SizeBytes).ToList();

        var orphanedSize = FormatSize(actionableFiles.Sum(f => f.SizeBytes));
        var excludedSize = FormatSize(excludedFiles.Sum(f => f.SizeBytes));

        Summary = excludedFiles.Count > 0
            ? $"{actionableFiles.Count} orphaned ({orphanedSize}) · {excludedFiles.Count} excluded ({excludedSize})"
            : $"{actionableFiles.Count} orphaned ({orphanedSize})";
    }

    partial void OnSelectedFileChanged(OrphanedFile? value)
    {
        if (value is null)
        {
            SelectedDetails = null;
            return;
        }

        if (!_cache.TryGetValue(value.FullPath, out var info))
        {
            info = _infoService.GetSummaryInfo(value.FullPath);
            _cache[value.FullPath] = info;
        }

        SelectedDetails = info;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
