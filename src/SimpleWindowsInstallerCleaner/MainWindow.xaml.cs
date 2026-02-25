using System.Windows;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
