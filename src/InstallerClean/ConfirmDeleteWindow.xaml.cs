using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(int fileCount, string sizeDisplay, long totalBytes = 0)
    {
        InitializeComponent();
        var label = DisplayHelpers.Pluralise(fileCount, "file", "files");
        MessageText.Text = $"Delete {fileCount} {label} ({sizeDisplay})?";

        // Warn if total size exceeds 1 GB — Recycle Bin may permanently delete large files
        if (totalBytes > 1_073_741_824)
            LargeSizeWarning.Visibility = Visibility.Visible;
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
