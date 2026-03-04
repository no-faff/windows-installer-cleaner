using System.Diagnostics;
using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = DisplayHelpers.GetVersionString();
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = link.NavigateUri.AbsoluteUri,
                UseShellExecute = true
            });
        }
    }
}
