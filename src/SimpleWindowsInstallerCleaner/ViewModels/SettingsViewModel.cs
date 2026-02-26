using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public ObservableCollection<string> Filters { get; } = new();

    [ObservableProperty] private string _newFilter = string.Empty;
    [ObservableProperty] private bool _checkPendingReboot;

    public event Action<bool?>? CloseRequested;

    public SettingsViewModel(AppSettings currentSettings, ISettingsService settingsService)
    {
        _settingsService = settingsService;

        foreach (var filter in currentSettings.ExclusionFilters)
            Filters.Add(filter);

        CheckPendingReboot = currentSettings.CheckPendingReboot;
    }

    [RelayCommand]
    private void AddFilter()
    {
        var trimmed = NewFilter.Trim();
        if (string.IsNullOrEmpty(trimmed) ||
            Filters.Any(f => string.Equals(f, trimmed, StringComparison.OrdinalIgnoreCase)))
            return;

        Filters.Add(trimmed);
        NewFilter = string.Empty;
    }

    [RelayCommand]
    private void RemoveFilter(string filter) => Filters.Remove(filter);

    [RelayCommand]
    private void Save()
    {
        var settings = _settingsService.Load();
        settings.ExclusionFilters = Filters.ToList();
        settings.CheckPendingReboot = CheckPendingReboot;

        try
        {
            _settingsService.Save(settings);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not save settings: {ex.Message}",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
