using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using InstallerClean.Helpers;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class App : Application
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // CLI mode: /d (delete), /m (move to saved location), /m <path> (move to path)
        if (e.Args.Length > 0)
        {
            await RunCliAsync(e.Args);
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"An unexpected error occurred and InstallerClean needs to close.\n\n{args.Exception.Message}",
                "InstallerClean", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred and InstallerClean needs to close.\n\n{ex.Message}",
                    "InstallerClean", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
        };

        SplashWindow? splash = null;
        try
        {
            // Dark titlebar and app icon on all windows
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

            splash = new SplashWindow();
            splash.Show();

            splash.UpdateStep("Scanning...", 10);

            var settingsService = new SettingsService();
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);
            var moveService = new MoveFilesService();
            var deleteService = new DeleteFilesService();
            var rebootService = new PendingRebootService();
            var msiInfoService = new MsiFileInfoService();

            var viewModel = new MainViewModel(
                scanService, moveService, deleteService,
                settingsService, rebootService, msiInfoService);

            int messageCount = 0;
            var splashProgress = new Progress<string>(msg =>
            {
                messageCount++;
                var percent = 10 + 80.0 * messageCount / (messageCount + 15);
                splash.UpdateStep(msg, percent);
            });
            var scanTask = viewModel.ScanWithProgressAsync(splashProgress);
            await Task.WhenAll(scanTask, Task.Delay(800));
            splash.UpdateStep("Done", 100);
            await Task.Delay(200);

            var window = new MainWindow(viewModel);
            Application.Current.MainWindow = window;
            window.Show();
            splash.Close();
        }
        catch (UnauthorizedAccessException)
        {
            splash?.Close();
            MessageBox.Show(
                "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
                "Administrator rights required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Shutdown();
        }
        catch (Exception ex)
        {
            splash?.Close();
            MessageBox.Show(
                $"Failed to start: {ex.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task RunCliAsync(string[] args)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);

        var arg = args[0].ToLowerInvariant();
        if (arg is not "/d" and not "/m" and not "--help" and not "/?" and not "-h")
        {
            Console.WriteLine($"Unknown argument: {args[0]}");
            Console.WriteLine();
            PrintUsage();
            Shutdown(1);
            return;
        }

        if (arg is "--help" or "/?" or "-h")
        {
            PrintUsage();
            Shutdown();
            return;
        }

        try
        {
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            var queryService = new InstallerQueryService();
            var scanService = new FileSystemScanService(queryService);

            Console.WriteLine("Scanning C:\\Windows\\Installer...");
            var scanResult = await scanService.ScanAsync();

            var count = scanResult.RemovableFiles.Count;
            var size = DisplayHelpers.FormatSize(scanResult.RemovableFiles.Sum(f => f.SizeBytes));
            Console.WriteLine($"Found {count} {DisplayHelpers.Pluralise(count, "file", "files")} to clean up ({size}).");

            if (count == 0)
            {
                Console.WriteLine("Nothing to do.");
                Shutdown(0);
                return;
            }

            var filePaths = scanResult.RemovableFiles.Select(f => f.FullPath).ToList();

            if (arg == "/d")
            {
                var deleteService = new DeleteFilesService();
                Console.WriteLine($"Deleting {count} files...");
                var result = await deleteService.DeleteFilesAsync(filePaths, null, CancellationToken.None);
                Console.WriteLine($"Deleted {result.DeletedCount} {DisplayHelpers.Pluralise(result.DeletedCount, "file", "files")}.");
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {err}");
                }
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
            else if (arg == "/m")
            {
                var dest = args.Length > 1 ? args[1] : settings.MoveDestination;
                if (string.IsNullOrWhiteSpace(dest))
                {
                    Console.WriteLine("Error: no move destination specified. Use /m PATH or set a default in the GUI.");
                    Shutdown(1);
                    return;
                }

                var moveService = new MoveFilesService();
                Console.WriteLine($"Moving {count} files to {dest}...");
                var result = await moveService.MoveFilesAsync(filePaths, dest, null, CancellationToken.None);
                Console.WriteLine($"Moved {result.MovedCount} {DisplayHelpers.Pluralise(result.MovedCount, "file", "files")}.");
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine($"{result.Errors.Count} {DisplayHelpers.Pluralise(result.Errors.Count, "error", "errors")}:");
                    foreach (var err in result.Errors)
                        Console.WriteLine($"  {err}");
                }
                Shutdown(result.Errors.Count > 0 ? 1 : 0);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: administrator privileges required. Run from an elevated command prompt.");
            Shutdown(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Shutdown(1);
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("InstallerClean — clean up C:\\Windows\\Installer");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  InstallerClean.exe          Launch the GUI");
        Console.WriteLine("  InstallerClean.exe /d       Delete removable files (Recycle Bin)");
        Console.WriteLine("  InstallerClean.exe /m       Move to saved default location");
        Console.WriteLine("  InstallerClean.exe /m PATH  Move to specified path");
        Console.WriteLine();
    }
}
