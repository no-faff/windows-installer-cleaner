using System.Windows;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var queryService = new InstallerQueryService();
        var scanService = new FileSystemScanService(queryService);
        var moveService = new MoveFilesService();
        var viewModel = new MainViewModel(scanService, moveService);

        var window = new MainWindow(viewModel);
        window.Show();
    }
}
