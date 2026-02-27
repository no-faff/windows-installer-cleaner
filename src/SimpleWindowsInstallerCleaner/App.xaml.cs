using System.Windows;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var splash = new SplashWindow();
        splash.Show();

        try
        {
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

            var stepNumber = 0;
            var progress = new Progress<string>(msg =>
            {
                if (msg.StartsWith("Checking") || msg.StartsWith("Starting"))
                    splash.UpdateStep($"Step 1/5: {msg}");
                else if (msg.StartsWith("Enumerating installed"))
                {
                    stepNumber = 2;
                    splash.UpdateStep($"Step 2/5: {msg}");
                }
                else if (msg.StartsWith("Found") && msg.Contains("product"))
                {
                    stepNumber = 3;
                    splash.UpdateStep("Step 3/5: Enumerating patches...");
                }
                else if (msg.StartsWith("Scanning"))
                {
                    stepNumber = 4;
                    splash.UpdateStep("Step 4/5: Finding installation files...");
                }
                else if (msg.StartsWith("Found") && msg.Contains("orphaned"))
                {
                    stepNumber = 5;
                    splash.UpdateStep("Step 5/5: Calculating orphaned files...");
                }
                else if (stepNumber == 2)
                {
                    splash.UpdateStep($"Step 2/5: {msg}");
                }
            });

            splash.UpdateStep("Step 1/5: Checking system status...");
            await viewModel.ScanWithProgressAsync(progress);

            var window = new MainWindow(viewModel);
            window.Show();
            splash.Close();
        }
        catch (UnauthorizedAccessException)
        {
            splash.Close();
            MessageBox.Show(
                "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
                "Administrator rights required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
        catch (Exception ex)
        {
            splash.Close();
            MessageBox.Show(
                $"Failed to start: {ex.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
