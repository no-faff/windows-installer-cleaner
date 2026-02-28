using System.Windows;
using System.Windows.Media.Animation;

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

        // Animate the custom gradient progress bar
        var container = SplashProgressBorder.Parent as FrameworkElement;
        if (container == null) return;

        // Need to wait for layout if container hasn't been measured yet
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

    public void UpdateStep(string message) => UpdateStep(message, 0);
}
