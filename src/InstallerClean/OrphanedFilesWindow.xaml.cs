using System.Windows;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class OrphanedFilesWindow : Window
{
    public OrphanedFilesWindow(OrphanedFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
