using CommunityToolkit.Mvvm.ComponentModel;
using InstallerClean.Helpers;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.ViewModels;

public partial class OrphanedFilesViewModel : ObservableObject
{
    private readonly IMsiFileInfoService _infoService;
    private readonly Dictionary<string, MsiSummaryInfo?> _cache = new();

    public IReadOnlyList<OrphanedFile> Files { get; }
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
        IReadOnlyList<OrphanedFile> files,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;
        Files = files.OrderByDescending(f => f.SizeBytes).ToList();

        var totalSize = DisplayHelpers.FormatSize(files.Sum(f => f.SizeBytes));
        Summary = $"{files.Count} {DisplayHelpers.Pluralise(files.Count, "file", "files")} ({totalSize})";

        if (Files.Count > 0)
            SelectedFile = Files[0];
    }

    async partial void OnSelectedFileChanged(OrphanedFile? value)
    {
        if (value is null)
        {
            SelectedDetails = null;
            return;
        }

        if (_cache.TryGetValue(value.FullPath, out var cached))
        {
            SelectedDetails = cached;
            return;
        }

        try
        {
            var info = await Task.Run(() => _infoService.GetSummaryInfo(value.FullPath));

            // Selection may have changed while we were reading
            if (SelectedFile == value)
            {
                _cache[value.FullPath] = info;
                SelectedDetails = info;
            }
        }
        catch
        {
            if (SelectedFile == value)
                SelectedDetails = null;
        }
    }

}
