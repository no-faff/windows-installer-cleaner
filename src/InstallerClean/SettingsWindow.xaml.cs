using System.Windows;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
