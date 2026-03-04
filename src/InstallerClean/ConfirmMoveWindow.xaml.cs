using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class ConfirmMoveWindow : Window
{
    public ConfirmMoveWindow(int fileCount, string sizeDisplay, string destination)
    {
        InitializeComponent();
        var label = DisplayHelpers.Pluralise(fileCount, "file", "files");
        MessageText.Text = $"Move {fileCount} {label} ({sizeDisplay})?";
        DestinationText.Text = $"Files will be moved to {destination}";
    }

    private void OnMove(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
