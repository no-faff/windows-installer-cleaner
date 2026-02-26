using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IMoveFilesService _moveService;
    private readonly IDeleteFilesService _deleteService;
    private readonly IExclusionService _exclusionService;
    private readonly ISettingsService _settingsService;
    private readonly IPendingRebootService _rebootService;

    // Scan state
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanProgress = string.Empty;

    // Summary line data
    [ObservableProperty] private int _registeredFileCount;
    [ObservableProperty] private string _registeredSizeDisplay = string.Empty;
    [ObservableProperty] private int _excludedFileCount;
    [ObservableProperty] private string _excludedSizeDisplay = string.Empty;
    [ObservableProperty] private int _orphanedFileCount;
    [ObservableProperty] private string _orphanedSizeDisplay = string.Empty;

    // Pending reboot
    [ObservableProperty] private bool _hasPendingReboot;

    // Move destination (persisted)
    [ObservableProperty] private string _moveDestination = string.Empty;

    // Busy state for move/delete operations
    [ObservableProperty] private bool _isOperating;
    [ObservableProperty] private string _operationProgress = string.Empty;

    // Whether scan has completed at least once
    [ObservableProperty] private bool _hasScanned;

    private ScanResult? _lastScanResult;
    private FilteredResult? _lastFilteredResult;
    private AppSettings _settings = new();

    public MainViewModel(
        IFileSystemScanService scanService,
        IMoveFilesService moveService,
        IDeleteFilesService deleteService,
        IExclusionService exclusionService,
        ISettingsService settingsService,
        IPendingRebootService rebootService)
    {
        _scanService = scanService;
        _moveService = moveService;
        _deleteService = deleteService;
        _exclusionService = exclusionService;
        _settingsService = settingsService;
        _rebootService = rebootService;

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

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        ScanProgress = "Starting scan...";

        try
        {
            if (_settings.CheckPendingReboot)
                HasPendingReboot = _rebootService.HasPendingReboot();

            var progress = new Progress<string>(msg => ScanProgress = msg);
            _lastScanResult = await _scanService.ScanAsync(progress);

            _lastFilteredResult = _exclusionService.ApplyFilters(
                _lastScanResult.OrphanedFiles, _settings.ExclusionFilters);

            RegisteredFileCount = _lastScanResult.RegisteredFileCount;
            RegisteredSizeDisplay = FormatSize(_lastScanResult.RegisteredTotalBytes);

            ExcludedFileCount = _lastFilteredResult.Excluded.Count;
            ExcludedSizeDisplay = FormatSize(_lastFilteredResult.Excluded.Sum(f => f.SizeBytes));

            OrphanedFileCount = _lastFilteredResult.Actionable.Count;
            OrphanedSizeDisplay = FormatSize(_lastFilteredResult.Actionable.Sum(f => f.SizeBytes));

            HasScanned = true;
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
            _settingsService.Save(_settings);
        }
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveAllAsync()
    {
        if (_lastFilteredResult is null) return;

        var filePaths = _lastFilteredResult.Actionable.Select(f => f.FullPath).ToList();
        IsOperating = true;
        OperationProgress = $"Moving {filePaths.Count} file(s)...";

        try
        {
            var progress = new Progress<string>(msg => OperationProgress = msg);
            var result = await _moveService.MoveFilesAsync(filePaths, MoveDestination, progress);

            OperationProgress = result.Errors.Count == 0
                ? $"Moved {result.MovedCount} file(s) to {MoveDestination}."
                : $"Moved {result.MovedCount} file(s). {result.Errors.Count} error(s).";

            await ScanAsync();
        }
        catch (Exception ex)
        {
            OperationProgress = $"Move failed: {ex.Message}";
        }
        finally
        {
            IsOperating = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAllAsync()
    {
        if (_lastFilteredResult is null) return;

        var count = _lastFilteredResult.Actionable.Count;
        var sizeDisplay = OrphanedSizeDisplay;

        var confirm = MessageBox.Show(
            $"Permanently delete {count} file(s) ({sizeDisplay})?\n\nThis cannot be undone.",
            "Confirm delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var filePaths = _lastFilteredResult.Actionable.Select(f => f.FullPath).ToList();
        IsOperating = true;
        OperationProgress = $"Deleting {filePaths.Count} file(s)...";

        try
        {
            var progress = new Progress<string>(msg => OperationProgress = msg);
            var result = await _deleteService.DeleteFilesAsync(filePaths, progress);

            OperationProgress = result.Errors.Count == 0
                ? $"Deleted {result.DeletedCount} file(s)."
                : $"Deleted {result.DeletedCount} file(s). {result.Errors.Count} error(s).";

            await ScanAsync();
        }
        catch (Exception ex)
        {
            OperationProgress = $"Delete failed: {ex.Message}";
        }
        finally
        {
            IsOperating = false;
        }
    }

    [RelayCommand]
    private void OpenOrphanedDetails()
    {
        // Implemented in Task 10.
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // Implemented in Task 11.
    }

    [RelayCommand]
    private async Task RefreshAsync() => await ScanAsync();

    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
