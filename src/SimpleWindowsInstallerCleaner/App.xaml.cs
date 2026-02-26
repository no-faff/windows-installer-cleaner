using System.Windows;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new SettingsService();
        var queryService = new InstallerQueryService();
        var scanService = new FileSystemScanService(queryService);
        var moveService = new MoveFilesService();
        var deleteService = new DeleteFilesService();
        var exclusionService = new ExclusionService();
        var rebootService = new PendingRebootService();
        var msiInfoService = new MsiFileInfoService();

        var viewModel = new MainViewModel(
            scanService, moveService, deleteService,
            exclusionService, settingsService, rebootService, msiInfoService);

        var window = new MainWindow(viewModel);
        window.Show();

        // Auto-scan on startup.
        _ = viewModel.ScanCommand.ExecuteAsync(null);
    }
}
