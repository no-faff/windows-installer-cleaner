using System.IO;
using InstallerClean.Helpers;
using InstallerClean.Models;

namespace InstallerClean.Services;

public sealed class FileSystemScanService : IFileSystemScanService
{
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
            try { size = new FileInfo(filePath).Length; } catch (Exception) { /* skip inaccessible files */ }

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
            catch (Exception) { }

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
            catch (Exception) { }
        }

        progress?.Report($"Found {removable.Count} {DisplayHelpers.Pluralise(removable.Count, "file", "files")} to clean up.");
        return new ScanResult(removable.AsReadOnly(), stillUsed, stillUsedBytes);
    }

    private static IEnumerable<string> GetInstallerFiles()
    {
        if (!Directory.Exists(InstallerCacheHelpers.InstallerFolder))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(InstallerCacheHelpers.InstallerFolder, "*.msi", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(InstallerCacheHelpers.InstallerFolder, "*.msp", SearchOption.AllDirectories));
    }
}
