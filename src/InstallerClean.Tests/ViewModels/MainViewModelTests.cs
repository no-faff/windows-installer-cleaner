using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly IFileSystemScanService _scanService = Substitute.For<IFileSystemScanService>();
    private readonly IMoveFilesService _moveService = Substitute.For<IMoveFilesService>();
    private readonly IDeleteFilesService _deleteService = Substitute.For<IDeleteFilesService>();
    private readonly ISettingsService _settingsService = Substitute.For<ISettingsService>();
    private readonly IPendingRebootService _rebootService = Substitute.For<IPendingRebootService>();
    private readonly IMsiFileInfoService _msiInfoService = Substitute.For<IMsiFileInfoService>();

    private MainViewModel CreateViewModel()
    {
        _settingsService.Load().Returns(new AppSettings());

        return new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService);
    }

    private static ScanResult EmptyScanResult() =>
        new(Array.Empty<OrphanedFile>(), Array.Empty<RegisteredPackage>(), 0);

    private static ScanResult ScanResultWithOrphans(int count)
    {
        var files = Enumerable.Range(0, count)
            .Select(i => new OrphanedFile($@"C:\Windows\Installer\orphan{i}.msi", 1024 * (i + 1), false))
            .ToList();
        return new ScanResult(files, Array.Empty<RegisteredPackage>(), 0);
    }

    [Fact]
    public async Task ScanAsync_sets_HasScanned_after_scan()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        Assert.False(vm.HasScanned);
        await vm.ScanWithProgressAsync(null);
        Assert.True(vm.HasScanned);
    }

    [Fact]
    public async Task ScanAsync_populates_counts()
    {
        var vm = CreateViewModel();
        var orphans = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false),
            new(@"C:\Windows\Installer\b.msi", 2_097_152, false),
        };
        var registered = new List<RegisteredPackage>
        {
            new(@"C:\Windows\Installer\c.msi", "Product", "{AAA}"),
        };
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(new ScanResult(orphans, registered, 5_000_000));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal(2, vm.OrphanedFileCount);
        Assert.Equal(1, vm.RegisteredFileCount);
        Assert.Equal("3.0 MB", vm.OrphanedSizeDisplay);
        Assert.Equal("4.8 MB", vm.RegisteredSizeDisplay);
    }

    [Fact]
    public async Task ScanAsync_shows_all_clear_when_no_orphans()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(EmptyScanResult());

        await vm.ScanWithProgressAsync(null);

        Assert.True(vm.IsComplete);
        Assert.Equal("All clear", vm.CompletionHeading);
    }

    [Fact]
    public async Task ScanAsync_does_not_show_completion_when_orphans_exist()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(3));

        await vm.ScanWithProgressAsync(null);

        Assert.False(vm.IsComplete);
    }

    [Fact]
    public void MoveDestination_loads_from_settings()
    {
        _settingsService.Load().Returns(new AppSettings { MoveDestination = @"D:\Backup" });

        var vm = new MainViewModel(
            _scanService, _moveService, _deleteService,
            _settingsService, _rebootService, _msiInfoService);

        Assert.Equal(@"D:\Backup", vm.MoveDestination);
    }

    [Fact]
    public void DismissCompletion_clears_state()
    {
        var vm = CreateViewModel();
        vm.DismissCompletionCommand.Execute(null);

        Assert.False(vm.IsComplete);
        Assert.Equal(string.Empty, vm.CompletionErrors);
    }

    [Fact]
    public async Task SummaryText_uses_correct_pluralisation()
    {
        var vm = CreateViewModel();
        _scanService.ScanAsync(Arg.Any<IProgress<string>?>(), Arg.Any<CancellationToken>())
            .Returns(ScanResultWithOrphans(1));

        await vm.ScanWithProgressAsync(null);

        Assert.Equal("1 file to clean up", vm.OrphanedSummaryText);
    }
}
