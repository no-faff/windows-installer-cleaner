using System.Diagnostics;
using System.Windows;

namespace SimpleWindowsInstallerCleaner;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/no-faff/windows-installer-cleaner",
            UseShellExecute = true
        });
    }
}
