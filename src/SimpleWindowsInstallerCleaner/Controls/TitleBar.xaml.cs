using System.Windows;
using System.Windows.Controls;

namespace SimpleWindowsInstallerCleaner.Controls;

public partial class TitleBar : UserControl
{
    public static readonly DependencyProperty WindowTitleProperty =
        DependencyProperty.Register(
            nameof(WindowTitle), typeof(string), typeof(TitleBar),
            new PropertyMetadata(string.Empty));

    public string WindowTitle
    {
        get => (string)GetValue(WindowTitleProperty);
        set => SetValue(WindowTitleProperty, value);
    }

    public static readonly DependencyProperty ShowMaximiseProperty =
        DependencyProperty.Register(
            nameof(ShowMaximise), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true, OnShowMaximiseChanged));

    public bool ShowMaximise
    {
        get => (bool)GetValue(ShowMaximiseProperty);
        set => SetValue(ShowMaximiseProperty, value);
    }

    public static readonly DependencyProperty ShowMinimiseProperty =
        DependencyProperty.Register(
            nameof(ShowMinimise), typeof(bool), typeof(TitleBar),
            new PropertyMetadata(true, OnShowMinimiseChanged));

    public bool ShowMinimise
    {
        get => (bool)GetValue(ShowMinimiseProperty);
        set => SetValue(ShowMinimiseProperty, value);
    }

    public TitleBar()
    {
        InitializeComponent();
    }

    private void MinimiseButton_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;

    private void MaximiseButton_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;
        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)!.Close();

    private static void OnShowMaximiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBar tb)
            tb.MaximiseButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnShowMinimiseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TitleBar tb)
            tb.MinimiseButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
    }
}
