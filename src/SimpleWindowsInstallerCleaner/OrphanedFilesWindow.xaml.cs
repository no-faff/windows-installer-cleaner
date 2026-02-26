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

    private void ActionableList_GotFocus(object sender, RoutedEventArgs e)
    {
        ExcludedList.UnselectAll();
    }

    private void ExcludedList_GotFocus(object sender, RoutedEventArgs e)
    {
        ActionableList.UnselectAll();
    }
}
