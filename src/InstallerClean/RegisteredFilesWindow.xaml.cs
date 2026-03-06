using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using InstallerClean.ViewModels;

namespace InstallerClean;

public partial class RegisteredFilesWindow : Window
{
    private string? _lastSortProperty;
    private ListSortDirection _lastSortDirection;

    public RegisteredFilesWindow(RegisteredFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

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

    private void ColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
            return;

        var sortProperty = header.Column.Header switch
        {
            "Product name" => "ProductName",
            "File" => "FileName",
            "Size" => "SizeDisplay",
            "Patches" => "PatchCount",
            _ => null
        };

        if (sortProperty is null) return;

        var direction = sortProperty == _lastSortProperty && _lastSortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        var view = CollectionViewSource.GetDefaultView(ProductsList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(sortProperty, direction));

        _lastSortProperty = sortProperty;
        _lastSortDirection = direction;
    }
}
