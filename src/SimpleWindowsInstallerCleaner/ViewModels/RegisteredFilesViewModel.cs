using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class RegisteredFilesViewModel : ObservableObject
{
    private readonly IMsiFileInfoService _infoService;
    private readonly Dictionary<string, MsiSummaryInfo?> _cache = new();

    public IReadOnlyList<RegisteredFileRow> Packages { get; }
    public string Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private RegisteredFileRow? _selectedPackage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private MsiSummaryInfo? _selectedDetails;

    public bool HasSelection => SelectedPackage is not null;
    public bool ShowDetails => SelectedPackage is not null && SelectedDetails is not null;
    public bool ShowNoMetadata => SelectedPackage is not null && SelectedDetails is null;

    public RegisteredFilesViewModel(
        IReadOnlyList<RegisteredPackage> packages,
        long totalBytes,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;

        Packages = packages
            .OrderBy(p => p.ProductName)
            .Select(p => new RegisteredFileRow(
                ProductName: string.IsNullOrEmpty(p.ProductName) ? "(unknown)" : p.ProductName,
                FileName: Path.GetFileName(p.LocalPackagePath),
                FullPath: p.LocalPackagePath,
                SizeDisplay: GetSizeDisplay(p.LocalPackagePath)))
            .ToList();

        Summary = $"{packages.Count} registered file(s) ({FormatSize(totalBytes)})";
    }

    partial void OnSelectedPackageChanged(RegisteredFileRow? value)
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

    private static string GetSizeDisplay(string path)
    {
        try { return FormatSize(new FileInfo(path).Length); }
        catch { return string.Empty; }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}

public sealed record RegisteredFileRow(
    string ProductName,
    string FileName,
    string FullPath,
    string SizeDisplay);
