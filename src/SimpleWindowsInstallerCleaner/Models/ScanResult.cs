namespace SimpleWindowsInstallerCleaner.Models;

public record ScanResult(
    IReadOnlyList<OrphanedFile> OrphanedFiles,
    int RegisteredFileCount,
    long RegisteredTotalBytes);
