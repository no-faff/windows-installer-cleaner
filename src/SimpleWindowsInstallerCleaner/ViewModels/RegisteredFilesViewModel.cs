using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using SimpleWindowsInstallerCleaner.Helpers;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public sealed record ProductRow(
    string ProductName,
    string FileName,
    string FullPath,
    string SizeDisplay,
    int PatchCount,
    IReadOnlyList<PatchRow> Patches);

public sealed record PatchRow(
    string FileName,
    string FullPath,
    string SizeDisplay);

public partial class RegisteredFilesViewModel : ObservableObject
{
    private readonly IMsiFileInfoService _infoService;
    private readonly Dictionary<string, MsiSummaryInfo?> _cache = new();

    public IReadOnlyList<ProductRow> Products { get; }
    public string Summary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    [NotifyPropertyChangedFor(nameof(SelectedPatches))]
    [NotifyPropertyChangedFor(nameof(HasPatches))]
    private ProductRow? _selectedProduct;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    [NotifyPropertyChangedFor(nameof(ShowNoMetadata))]
    private MsiSummaryInfo? _selectedDetails;

    public bool HasSelection => SelectedProduct is not null;
    public bool HasPatches => SelectedProduct is not null && SelectedProduct.Patches.Count > 0;
    public bool ShowDetails => SelectedProduct is not null && SelectedDetails is not null;
    public bool ShowNoMetadata => SelectedProduct is not null && SelectedDetails is null;
    public IReadOnlyList<PatchRow> SelectedPatches => SelectedProduct?.Patches ?? Array.Empty<PatchRow>();

    public RegisteredFilesViewModel(
        IReadOnlyList<RegisteredPackage> packages,
        long totalBytes,
        IMsiFileInfoService infoService)
    {
        _infoService = infoService;

        var groups = packages.GroupBy(p => p.ProductCode, StringComparer.OrdinalIgnoreCase);

        var products = new List<ProductRow>();
        foreach (var group in groups.OrderBy(g => g.First().ProductName, StringComparer.OrdinalIgnoreCase))
        {
            var items = group.ToList();

            var msi = items.FirstOrDefault(p =>
                p.LocalPackagePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            var patches = items
                .Where(p => p.LocalPackagePath.EndsWith(".msp", StringComparison.OrdinalIgnoreCase))
                .Select(p => new PatchRow(
                    Path.GetFileName(p.LocalPackagePath),
                    p.LocalPackagePath,
                    GetSizeDisplay(p.LocalPackagePath)))
                .ToList();

            if (msi is null && patches.Count == 0) continue;

            var productName = items.First().ProductName;
            if (string.IsNullOrEmpty(productName)) productName = "(unknown)";

            var msiPath = msi?.LocalPackagePath ?? items.First().LocalPackagePath;

            products.Add(new ProductRow(
                productName,
                Path.GetFileName(msiPath),
                msiPath,
                GetSizeDisplay(msiPath),
                patches.Count,
                patches));
        }

        Products = products;
        Summary = $"{packages.Count} registered file(s) ({DisplayHelpers.FormatSize(totalBytes)})";
    }

    partial void OnSelectedProductChanged(ProductRow? value)
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
        try { return DisplayHelpers.FormatSize(new FileInfo(path).Length); }
        catch { return string.Empty; }
    }
}
