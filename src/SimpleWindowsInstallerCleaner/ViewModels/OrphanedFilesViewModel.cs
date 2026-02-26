using CommunityToolkit.Mvvm.ComponentModel;
using SimpleWindowsInstallerCleaner.Helpers;
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

        var orphanedSize = DisplayHelpers.FormatSize(actionableFiles.Sum(f => f.SizeBytes));
        var excludedSize = DisplayHelpers.FormatSize(excludedFiles.Sum(f => f.SizeBytes));

        Summary = excludedFiles.Count > 0
            ? $"{actionableFiles.Count} orphaned ({orphanedSize}) · {excludedFiles.Count} excluded ({excludedSize})"
            : $"{actionableFiles.Count} orphaned ({orphanedSize})";

        if (ActionableFiles.Count > 0)
            SelectedFile = ActionableFiles[0];
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

}
