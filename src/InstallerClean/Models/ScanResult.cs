namespace InstallerClean.Models;

public record ScanResult(
    IReadOnlyList<OrphanedFile> RemovableFiles,
    IReadOnlyList<RegisteredPackage> RegisteredPackages,
    long RegisteredTotalBytes);
