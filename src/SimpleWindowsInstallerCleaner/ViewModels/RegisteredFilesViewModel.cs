using System.IO;
using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public sealed class RegisteredFilesViewModel
{
    public IReadOnlyList<RegisteredFileRow> Packages { get; }
    public string Summary { get; }

    public RegisteredFilesViewModel(IReadOnlyList<RegisteredPackage> packages, long totalBytes)
    {
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
