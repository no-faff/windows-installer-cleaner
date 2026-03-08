using System.Windows;
using System.Windows.Media.Animation;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class SplashWindow : Window
{
    private int _progressMessageCount;

    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = DisplayHelpers.GetVersionString();
    }

    public void OnScanProgress(string message)
    {
        _progressMessageCount++;
        var percent = 10 + 80.0 * _progressMessageCount / (_progressMessageCount + 15);
        UpdateStep(message, percent);
    }

    public void UpdateStep(string message, double progressPercent)
    {
        StepText.Text = message;

        var container = SplashProgressBorder.Parent as FrameworkElement;
        if (container == null) return;

        container.UpdateLayout();
        var targetWidth = container.ActualWidth * (progressPercent / 100.0);

        var animation = new DoubleAnimation
        {
            To = targetWidth,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SplashProgressBorder.BeginAnimation(WidthProperty, animation);
    }

}
