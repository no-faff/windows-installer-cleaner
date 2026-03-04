using Moq;
using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class FileSystemScanServiceTests
{
    private static RegisteredPackage Registered(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}");

    private static RegisteredPackage Superseded(string path) =>
        new(path, "Test Product", "{00000000-0000-0000-0000-000000000001}", PatchState: 2, IsRemovable: true);

    [Fact]
    public async Task ScanAsync_returns_files_not_in_registered_set()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        var fakeFiles = new[]
        {
            @"C:\Windows\Installer\aaa.msi",   // registered — should NOT appear
            @"C:\Windows\Installer\bbb.msi",   // orphaned — should appear
        };

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);

        var result = await svc.ScanAsync();

        Assert.Single(result.RemovableFiles);
        Assert.Equal(@"C:\Windows\Installer\bbb.msi", result.RemovableFiles[0].FullPath);
    }

    [Fact]
    public async Task ScanAsync_path_comparison_is_case_insensitive()
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
        var result = await svc.ScanAsync();

        Assert.Empty(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_registered_packages_contains_all_api_packages()
    {
        // Registered packages pointing to paths that don't exist on disk.
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\aaa.msi"),
            Registered(@"C:\Windows\Installer\bbb.msi"),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        var fakeFiles = new[] { @"C:\Windows\Installer\ccc.msi" }; // orphan

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);
        var result = await svc.ScanAsync();

        // All API packages are included regardless of disk presence.
        Assert.Equal(2, result.RegisteredPackages.Count);
        // Files don't exist on disk, so total bytes is 0.
        Assert.Equal(0, result.RegisteredTotalBytes);
        Assert.Single(result.RemovableFiles);
    }

    [Fact]
    public async Task ScanAsync_superseded_patches_appear_in_removable_list()
    {
        var registered = new List<RegisteredPackage>
        {
            Registered(@"C:\Windows\Installer\applied.msp"),
            Superseded(@"C:\Windows\Installer\superseded.msp"),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        // No orphaned files on disk — only the registered ones
        var fakeFiles = Array.Empty<string>();

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);
        var result = await svc.ScanAsync();

        // The superseded patch should appear in RemovableFiles with Reason="Superseded"
        Assert.Single(result.RemovableFiles);
        Assert.Equal("Superseded", result.RemovableFiles[0].Reason);

        // The applied patch stays in RegisteredPackages
        Assert.Single(result.RegisteredPackages);
    }

    [Fact]
    public async Task ScanAsync_registry_fallback_entries_are_not_removable()
    {
        // Simulate a registry-fallback entry: PatchState=0, IsRemovable=false
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\fallback.msi", "", ""),
        };

        var mockQuery = new Mock<IInstallerQueryService>();
        mockQuery
            .Setup(s => s.GetRegisteredPackagesAsync(It.IsAny<IProgress<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(registered.AsReadOnly());

        var fakeFiles = Array.Empty<string>();

        var svc = new FileSystemScanService(mockQuery.Object, fakeFiles);
        var result = await svc.ScanAsync();

        // Fallback entries (PatchState=0, IsRemovable=false) stay in registered, not removable
        Assert.Empty(result.RemovableFiles);
        Assert.Single(result.RegisteredPackages);
    }
}
