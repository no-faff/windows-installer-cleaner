namespace SimpleWindowsInstallerCleaner.Models;

public record ScanResult(
    IReadOnlyList<OrphanedFile> OrphanedFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes);
