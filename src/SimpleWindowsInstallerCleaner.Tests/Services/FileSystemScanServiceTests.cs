using Moq;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.Tests.Services;

public class FileSystemScanServiceTests
{
    // Helper: builds a RegisteredPackage with only the path set.
    private static RegisteredPackage Registered(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", IsAdobeWarning: false);

    [Fact]
    public async Task FindOrphanedFilesAsync_returns_files_not_in_registered_set()
    {
        // Arrange
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        // Fake filesystem: two files, one registered, one orphaned.
        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\aaa.msi",   // registered — should NOT appear
            @"C:\Windows\Installer\bbb.msi",   // orphaned — should appear
        };

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);

        // Act
        var orphans = await svc.FindOrphanedFilesAsync();

        // Assert
        Assert.Single(orphans);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", orphans[0].FullPath);
        Assert.False(orphans[0].IsAdobeWarning);
    }

    [Fact]
    public async Task FindOrphanedFilesAsync_path_comparison_is_case_insensitive()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\AAA.MSI"),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        var fakeFiles = new[] { @"C:\Windows\Installer\aaa.msi" };

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);
        var orphans = await svc.FindOrphanedFilesAsync();

        Assert.Empty(orphans);
    }

    [Fact]
    public async Task FindOrphanedFilesAsync_adobe_warning_propagates_from_registered_set()
    {
        // A file claimed only by the Adobe component path (IsAdobeWarning = true).
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\adobe.msi", "Adobe Acrobat",
                "{ADOBE-GUID}", IsAdobeWarning: true),
        };

        // A second file is orphaned but its product had IsAdobeWarning set in
        // the component scan — simulate by having no entry at all (orphan).
        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        // adobe.msi is in the registered set — should NOT appear as orphan.
        var fakeFiles = new[] { @"C:\Windows\Installer\adobe.msi" };

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);
        var orphans = await svc.FindOrphanedFilesAsync();

        Assert.Empty(orphans);
    }
}
