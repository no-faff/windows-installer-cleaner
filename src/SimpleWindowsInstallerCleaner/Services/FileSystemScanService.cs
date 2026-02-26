using System.IO;
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

        // Size registered files that exist on disk.
        long registeredBytes = 0;
        foreach (var pkg in registered)
        {
            try
            {
                if (File.Exists(pkg.LocalPackagePath))
                    registeredBytes += new FileInfo(pkg.LocalPackagePath).Length;
            }
            catch { /* skip inaccessible */ }
        }

        progress?.Report("Scanning installer cache folder...");

        var diskFiles = _overrideFiles ?? GetInstallerFiles();
        var orphans = new List<OrphanedFile>();

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

            orphans.Add(new OrphanedFile(
                FullPath: filePath,
                SizeBytes: size,
                IsPatch: ext.Equals(".msp", StringComparison.OrdinalIgnoreCase)));
        }

        progress?.Report($"Found {orphans.Count} orphaned file(s).");
        return new ScanResult(orphans.AsReadOnly(), registered, registeredBytes);
    }

    private static IEnumerable<string> GetInstallerFiles()
    {
        if (!Directory.Exists(InstallerFolder))
            return Enumerable.Empty<string>();

        return Directory.EnumerateFiles(InstallerFolder, "*.msi")
            .Concat(Directory.EnumerateFiles(InstallerFolder, "*.msp"));
    }
}
