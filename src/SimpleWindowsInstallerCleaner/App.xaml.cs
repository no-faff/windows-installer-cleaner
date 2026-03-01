using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class App : Application
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "InstallerClean", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Force dark titlebar and app icon on all windows
        var appIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/splash-icon.png"));
        EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent,
            new RoutedEventHandler((s, _) =>
            {
                if (s is Window w)
                {
                    var hwnd = new WindowInteropHelper(w).Handle;
                    int value = 1;
                    DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
                    w.Icon = appIcon;
                }
            }));

        // Show splash immediately and force a render so it paints before scan work begins
        var splash = new SplashWindow();
        splash.Show();
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);

        try
        {
            // Step 1: show immediately while services are constructed
            splash.UpdateStep("Step 1/5: Initialising...", 10);

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
