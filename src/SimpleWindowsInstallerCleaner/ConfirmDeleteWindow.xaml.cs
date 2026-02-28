using System.Windows;

namespace SimpleWindowsInstallerCleaner;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(int fileCount, string sizeDisplay)
    {
        InitializeComponent();
        MessageText.Text = $"Permanently delete {fileCount} {(fileCount == 1 ? "file" : "files")} ({sizeDisplay})?";
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
