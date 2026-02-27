using System.Windows;

namespace SimpleWindowsInstallerCleaner;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void UpdateStep(string message)
    {
        StepText.Text = message;
    }
}
