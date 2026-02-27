using System.Windows;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ThemeService.ApplySystemTheme();

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

            // Step 1: brief pause so the user sees it
            splash.UpdateStep("Step 1/5: Checking system status...", 10);
            await Task.Delay(400);

            // Step 2: the actual scan (this is where the time is spent)
            splash.UpdateStep("Step 2/5: Enumerating installed products...", 20);
            var scanTask = viewModel.ScanWithProgressAsync(null);
            // Ensure step 2 shows for at least 400ms even on very fast machines
            await Task.WhenAll(scanTask, Task.Delay(400));

            // Steps 3–5: post-scan, blaze through visibly
            splash.UpdateStep("Step 3/5: Enumerating patches...", 50);
            await Task.Delay(400);

            splash.UpdateStep("Step 4/5: Finding installation files...", 70);
            await Task.Delay(400);

            splash.UpdateStep("Step 5/5: Calculating results...", 90);
            await Task.Delay(400);

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
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
