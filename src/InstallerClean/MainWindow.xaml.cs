using System.Windows;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
