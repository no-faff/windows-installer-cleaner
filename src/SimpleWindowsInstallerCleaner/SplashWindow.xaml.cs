using System.Windows;

namespace SimpleWindowsInstallerCleaner;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void UpdateStep(string message, double progressPercent)
    {
        StepText.Text = message;
        SplashProgress.Value = progressPercent;
    }

    public void UpdateStep(string message) => UpdateStep(message, 0);
}
