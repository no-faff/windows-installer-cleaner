using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemScanService _scanService;
    private readonly IMoveFilesService _moveService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanMove))]
    [NotifyPropertyChangedFor(nameof(SelectedSizeDisplay))]
    private ObservableCollection<OrphanedFileViewModel> _files = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScan))]
    [NotifyPropertyChangedFor(nameof(CanMove))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Click \"Scan\" to find orphaned files.";

    [ObservableProperty]
    private string _destinationFolder = string.Empty;

    private CancellationTokenSource? _cts;

    public bool CanScan => !IsBusy;
    public bool CanMove => !IsBusy && Files.Any(f => f.IsSelected) && !string.IsNullOrWhiteSpace(DestinationFolder);

    public string SelectedSizeDisplay
    {
        get
        {
            var bytes = Files.Where(f => f.IsSelected).Sum(f => f.File.SizeBytes);
            return bytes switch
            {
                0 => "Nothing selected",
                >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB selected",
                >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB selected",
                >= 1_024 => $"{bytes / 1_024.0:F1} KB selected",
                _ => $"{bytes} B selected"
            };
        }
    }

    public MainViewModel(IFileSystemScanService scanService, IMoveFilesService moveService)
    {
        _scanService = scanService;
        _moveService = moveService;
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        IsBusy = true;
        Files.Clear();
        StatusText = "Scanning...";

        try
        {
            var progress = new Progress<string>(msg => StatusText = msg);
            var orphans = await _scanService.FindOrphanedFilesAsync(progress, _cts.Token);

            foreach (var orphan in orphans.OrderByDescending(f => f.SizeBytes))
            {
                var vm = new OrphanedFileViewModel(orphan);
                vm.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(CanMove));
                    OnPropertyChanged(nameof(SelectedSizeDisplay));
                };
                Files.Add(vm);
            }

            StatusText = orphans.Count == 0
                ? "No orphaned files found."
                : $"Found {orphans.Count} orphaned file(s). Select files to move.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var f in Files) f.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var f in Files) f.IsSelected = false;
    }

    [RelayCommand]
    private void ChooseDestination()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose destination folder for moved files"
        };
        if (dialog.ShowDialog() == true)
            DestinationFolder = dialog.FolderName;

        OnPropertyChanged(nameof(CanMove));
    }

    [RelayCommand(CanExecute = nameof(CanMove))]
    private async Task MoveSelectedAsync()
    {
        var toMove = Files.Where(f => f.IsSelected).Select(f => f.File.FullPath).ToList();

        IsBusy = true;
        StatusText = $"Moving {toMove.Count} file(s)...";

        try
        {
            var progress = new Progress<string>(msg => StatusText = msg);
            var result = await _moveService.MoveFilesAsync(toMove, DestinationFolder, progress);

            // Remove successfully moved files from the list.
            var toRemove = Files.Where(f => f.IsSelected && !result.Errors.Any(e => e.FilePath == f.File.FullPath)).ToList();
            foreach (var f in toRemove) Files.Remove(f);

            StatusText = result.Errors.Count == 0
                ? $"Moved {result.MovedCount} file(s) to {DestinationFolder}."
                : $"Moved {result.MovedCount} file(s). {result.Errors.Count} error(s) — check that the app is running as administrator.";
        }
        catch (Exception ex)
        {
            StatusText = $"Move failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
