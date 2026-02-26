using System.Windows;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
