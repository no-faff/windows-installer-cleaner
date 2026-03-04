using System.Windows;
using System.Windows.Controls;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class RegisteredFilesWindow : Window
{
    public RegisteredFilesWindow(RegisteredFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (ProductsList.Items.Count > 0)
        {
            ProductsList.SelectedIndex = 0;
            ProductsList.ScrollIntoView(ProductsList.Items[0]);
            var container = (ListViewItem?)ProductsList.ItemContainerGenerator
                .ContainerFromIndex(0);
            container?.Focus();
        }
    }
}
