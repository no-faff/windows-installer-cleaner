using System.Windows;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class OrphanedFilesWindow : Window
{
    public OrphanedFilesWindow(OrphanedFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
