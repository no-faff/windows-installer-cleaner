using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleWindowsInstallerCleaner.Helpers;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly ISettingsService _settingsService;
    private readonly IPendingRebootService _rebootService;
    private readonly IMsiFileInfoService _msiInfoService;

    // Scan state
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;

    // Summary line data
    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    // Pending reboot
    [ObservableProperty] private bool _hasPendingReboot;

    // Move destination (persisted)
    [ObservableProperty] private string _moveDestination = string.Empty;

    // Busy state for move/delete operations
    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;
    [ObservableProperty] private int _operationCurrentFile;
    [ObservableProperty] private int _operationTotalFiles;
    [ObservableProperty] private string _operationCurrentFileName = string.Empty;
    [ObservableProperty] private double _operationProgressPercent;

    private CancellationTokenSource? _operationCts;

    // Whether scan has completed at least once
    [ObservableProperty] private bool _hasScanned;

    // Completion screen state
    [ObservableProperty] private bool _isComplete;
    [ObservableProperty] private string _completionHeading = string.Empty;
    [ObservableProperty] private string _completionSummary = string.Empty;
    [ObservableProperty] private string _completionRestore = string.Empty;
    [ObservableProperty] private string _completionErrors = string.Empty;

    private ScanResult? _lastScanResult;
    private AppSettings _settings = new();

    public MainViewModel(
        IFileSystemScanService scanService,
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        ISettingsService settingsService,
        IPendingRebootService rebootService,
        IMsiFileInfoService msiInfoService)
    {
        _scanService = scanService;
        _moveService = moveService;
        _deleteService = deleteService;
        _settingsService = settingsService;
        _rebootService = rebootService;
        _msiInfoService = msiInfoService;

        _settings = settingsService.Load();
        MoveDestination = _settings.MoveDestination;
    }

    partial void OnIsScanningChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsOperatingChanged(bool value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnOrphanedFileCountChanged(int value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
        DeleteAllCommand.NotifyCanExecuteChanged();
    }

    partial void OnMoveDestinationChanged(string value)
    {
        MoveAllCommand.NotifyCanExecuteChanged();
    }

    private bool CanMove() =>
        !IsScanning && !IsOperating && OrphanedFileCount > 0 && !string.IsNullOrWhiteSpace(MoveDestination);

    private bool CanDelete() =>
        !IsScanning && !IsOperating && OrphanedFileCount > 0;

    private async Task RunScanCoreAsync(IProgress<string>? progress)
    {
        HasPendingReboot = _rebootService.HasPendingReboot();

        _lastScanResult = await _scanService.ScanAsync(progress);

        RegisteredFileCount = _lastScanResult.RegisteredPackages.Count;
        RegisteredSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RegisteredTotalBytes);

        OrphanedFileCount = _lastScanResult.RemovableFiles.Count;
        OrphanedSizeDisplay = DisplayHelpers.FormatSize(_lastScanResult.RemovableFiles.Sum(f => f.SizeBytes));

        HasScanned = true;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        ScanProgress = "Starting scan...";
        var sw = Stopwatch.StartNew();

        try
        {
            var progress = new Progress<string>(msg => ScanProgress = msg);
            var scanTask = RunScanCoreAsync(progress);
            if (await Task.WhenAny(scanTask, Task.Delay(200)) != scanTask)
                IsScanning = true;
            await scanTask;

            sw.Stop();
            ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";

            // Show "all clear" when no orphaned files and not mid-operation
            if (OrphanedFileCount == 0 && !IsOperating)
            {
                CompletionHeading = "All clear";
                CompletionSummary = "Nothing to clean up in C:\\Windows\\Installer";
                CompletionRestore = "Nothing to clean up. You're all good.";
                CompletionErrors = string.Empty;
                IsComplete = true;
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                "This app requires administrator privileges.\n\nPlease right-click and choose 'Run as administrator'.",
                "Administrator rights required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            ScanProgress = "Access denied. Run as administrator.";
        }
        catch (Exception ex)
        {
            ScanProgress = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void BrowseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose destination folder for moved files"
        };
        if (dialog.ShowDialog() == true)
        {
            MoveDestination = dialog.FolderName;
            _settings.MoveDestination = MoveDestination;

            try
            {
                _settingsService.Save(_settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not save settings: {ex.Message}",
                    "Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    [RelayCommand]
    private void CancelOperation()
    {
        _operationCts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_lastScanResult is null) return;

        var dest = MoveDestination;
        if (dest.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Equals(InstallerFolder.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "The destination cannot be the Windows Installer folder itself.",
                "Invalid destination", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate destination exists and is writable
        try
        {
            Directory.CreateDirectory(dest);
            var testFile = Path.Combine(dest, ".installerclean-write-test");
            File.WriteAllText(testFile, "");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Cannot write to {dest}:\n{ex.Message}",
                "Invalid destination", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var filePaths = _lastScanResult.RemovableFiles.Select(f => f.FullPath).ToList();
        var count = filePaths.Count;
        var totalBytes = _lastScanResult.RemovableFiles.Sum(f => f.SizeBytes);
        var sizeDisplay = OrphanedSizeDisplay;

        // Check free space
        var driveInfo = new DriveInfo(Path.GetPathRoot(dest)!);
        if (driveInfo.AvailableFreeSpace < totalBytes)
        {
            MessageBox.Show(
                $"Not enough space on {driveInfo.Name}\n\n" +
                $"Required: {DisplayHelpers.FormatSize(totalBytes)}\n" +
                $"Available: {DisplayHelpers.FormatSize(driveInfo.AvailableFreeSpace)}",
                "Not enough space", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmDialog = new ConfirmMoveWindow(count, sizeDisplay, MoveDestination)
        {
            Owner = Application.Current.MainWindow
        };
        if (confirmDialog.ShowDialog() != true) return;

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        OperationProgress = $"Moving {count} {DisplayHelpers.Pluralise(count, "file", "files")}...";

        try
        {
            var progress = new Progress<Models.OperationProgress>(p =>
            {
                OperationCurrentFile = p.CurrentFile;
                OperationTotalFiles = p.TotalFiles;
                OperationCurrentFileName = p.CurrentFileName;
                OperationProgressPercent = (double)p.CurrentFile / p.TotalFiles * 100;
                OperationProgress = $"{p.CurrentFile} of {p.TotalFiles} files";
            });
            var result = await _moveService.MoveFilesAsync(filePaths, MoveDestination, progress, _operationCts.Token);
            var movedSize = sizeDisplay;
            var movedCount = result.MovedCount;
            var movedDest = MoveDestination;
            var errorCount = result.Errors.Count;

            await ScanAsync();

            // Show completion screen
            CompletionHeading = "All done";
            var movedLabel = DisplayHelpers.Pluralise(movedCount, "file", "files");
            CompletionSummary = errorCount == 0
                ? $"{movedCount} {movedLabel} ({movedSize}) moved to {movedDest}"
                : $"{movedCount} {movedLabel} ({movedSize}) moved. {errorCount} {DisplayHelpers.Pluralise(errorCount, "error", "errors")}.";
            CompletionRestore = $"If anything stops working, copy them back from {movedDest}";
            CompletionErrors = errorCount > 0
                ? string.Join("\n", result.Errors.Select(e => $"{Path.GetFileName(e.FilePath)}: {e.Message}"))
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = "Move cancelled.";
            await ScanAsync();
        }
        catch (Exception ex)
        {
            OperationProgress = $"Move failed: {ex.Message}";
        }
        finally
        {
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            IsOperating = false;
            OperationProgressPercent = 0;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAllAsync()
    {
        if (_lastScanResult is null) return;

        var count = _lastScanResult.RemovableFiles.Count;
        var sizeDisplay = OrphanedSizeDisplay;
        var totalBytes = _lastScanResult.RemovableFiles.Sum(f => f.SizeBytes);

        var dialog = new ConfirmDeleteWindow(count, sizeDisplay, totalBytes)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        IsOperating = true;
        _operationCts = new CancellationTokenSource();
        var filePaths = _lastScanResult.RemovableFiles.Select(f => f.FullPath).ToList();
        OperationProgress = $"Deleting {filePaths.Count} {DisplayHelpers.Pluralise(filePaths.Count, "file", "files")}...";

        try
        {
            var progress = new Progress<Models.OperationProgress>(p =>
            {
                OperationCurrentFile = p.CurrentFile;
                OperationTotalFiles = p.TotalFiles;
                OperationCurrentFileName = p.CurrentFileName;
                OperationProgressPercent = (double)p.CurrentFile / p.TotalFiles * 100;
                OperationProgress = $"{p.CurrentFile} of {p.TotalFiles} files";
            });
            var result = await _deleteService.DeleteFilesAsync(filePaths, progress, _operationCts.Token);
            var deletedSize = sizeDisplay;
            var deletedCount = result.DeletedCount;
            var errorCount = result.Errors.Count;

            await ScanAsync();

            // Show completion screen
            CompletionHeading = "All done";
            var deletedLabel = DisplayHelpers.Pluralise(deletedCount, "file", "files");
            CompletionSummary = errorCount == 0
                ? $"{deletedCount} {deletedLabel} ({deletedSize}) sent to Recycle Bin"
                : $"{deletedCount} {deletedLabel} ({deletedSize}) deleted. {errorCount} {DisplayHelpers.Pluralise(errorCount, "error", "errors")}.";
            CompletionRestore = "Files are in your Recycle Bin if you need them back";
            CompletionErrors = errorCount > 0
                ? string.Join("\n", result.Errors.Select(e => $"{Path.GetFileName(e.FilePath)}: {e.Message}"))
                : string.Empty;
            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            OperationProgress = "Delete cancelled.";
            await ScanAsync();
        }
        catch (Exception ex)
        {
            OperationProgress = $"Delete failed: {ex.Message}";
        }
        finally
        {
            var cts = _operationCts;
            _operationCts = null;
            cts?.Dispose();
            IsOperating = false;
            OperationProgressPercent = 0;
        }
    }

    [RelayCommand]
    private void OpenOrphanedDetails()
    {
        if (_lastScanResult is null) return;

        var viewModel = new OrphanedFilesViewModel(
            _lastScanResult.RemovableFiles,
            _msiInfoService);

        var window = new OrphanedFilesWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenRegisteredDetails()
    {
        if (_lastScanResult is null) return;

        var viewModel = new RegisteredFilesViewModel(
            _lastScanResult.RegisteredPackages,
            _lastScanResult.RegisteredTotalBytes,
            _msiInfoService);

        var window = new RegisteredFilesWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var window = new AboutWindow
        {
            Owner = Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void StarOnGitHub()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/no-faff/windows-installer-cleaner",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void Donate()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://ko-fi.com/nofaff",
            UseShellExecute = true
        });
    }

    public async Task ScanWithProgressAsync(IProgress<string>? progress)
    {
        var sw = Stopwatch.StartNew();
        await RunScanCoreAsync(progress);
        sw.Stop();
        ScanProgress = $"Scan complete ({sw.Elapsed.TotalSeconds:F1}s)";

        if (OrphanedFileCount == 0)
        {
            CompletionHeading = "All clear";
            CompletionSummary = "Nothing to clean up in C:\\Windows\\Installer";
            CompletionRestore = "Nothing to clean up. You're all good.";
            CompletionErrors = string.Empty;
            IsComplete = true;
        }
    }

    [RelayCommand]
    private void DismissCompletion()
    {
        IsComplete = false;
        CompletionErrors = string.Empty;
    }

    [RelayCommand]
    private void CloseApp()
    {
        Application.Current.MainWindow?.Close();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await ScanAsync();

}
