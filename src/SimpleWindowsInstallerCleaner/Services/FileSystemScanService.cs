using System.IO;
using SimpleWindowsInstallerCleaner.Helpers;
using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class FileSystemScanService : IFileSystemScanService
{
    private static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    private readonly IInstallerQueryService _queryService;
    private readonly IEnumerable<string>? _overrideFiles;

    /// <summary>Production constructor.</summary>
    public FileSystemScanService(IInstallerQueryService queryService)
        : this(queryService, null) { }

    /// <summary>Test constructor — injects a fake file list.</summary>
    internal FileSystemScanService(IInstallerQueryService queryService, IEnumerable<string>? overrideFiles)
    {
        _queryService = queryService;
        _overrideFiles = overrideFiles;
    }

    public async Task<ScanResult> ScanAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Querying Windows Installer API...");

        var registered = await _queryService.GetRegisteredPackagesAsync(progress, cancellationToken);

        var registeredPaths = new HashSet<string>(
            registered.Select(p => p.LocalPackagePath),
            StringComparer.OrdinalIgnoreCase);

        progress?.Report("Scanning installer cache folder...");

        var diskFiles = _overrideFiles ?? GetInstallerFiles();
        var removable = new List<OrphanedFile>();

        foreach (var filePath in diskFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (registeredPaths.Contains(filePath))
                continue;

            var ext = Path.GetExtension(filePath);
            if (!ext.Equals(".msi", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".msp", StringComparison.OrdinalIgnoreCase))
                continue;

            long size = 0;
            try { size = new FileInfo(filePath).Length; } catch { /* skip inaccessible files */ }

            removable.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase)));
        }

        // Superseded registered patches that are safe to remove
        foreach (var pkg in registered.Where(p => p.IsRemovable))
        {
            cancellationToken.ThrowIfCancellationRequested();

            long size = 0;
            try { if (File.Exists(pkg.LocalPackagePath)) size = new FileInfo(pkg.LocalPackagePath).Length; }
            catch { }

            var ext = Path.GetExtension(pkg.LocalPackagePath);
            removable.Add(new OrphanedFile(
                FullPath: pkg.LocalPackagePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase),
                Reason: "Superseded"));
        }

        // Filter removable packages out of the registered list for the "still used" count
        var stillUsed = registered.Where(p => !p.IsRemovable).ToList().AsReadOnly();
        long stillUsedBytes = 0;
        foreach (var pkg in stillUsed)
        {
            try
            {
                if (File.Exists(pkg.LocalPackagePath))
                    stillUsedBytes += new FileInfo(pkg.LocalPackagePath).Length;
            }
            catch { }
        }

        progress?.Report($"Found {removable.Count} {DisplayHelpers.Pluralise(removable.Count, "file", "files")} to clean up.");
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes);
    }

    private static IEnumerable<string> GetInstallerFiles()
    {
        if (!Directory.Exists(InstallerFolder))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(InstallerFolder, "*.msi", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(InstallerFolder, "*.msp", SearchOption.AllDirectories));
    }
}
