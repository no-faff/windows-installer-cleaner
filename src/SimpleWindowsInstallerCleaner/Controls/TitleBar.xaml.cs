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
        Loaded += TitleBar_Loaded;
    }

    private void TitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
            window.StateChanged += Window_StateChanged;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        var window = sender as Window;
        MaximiseButton.Content = window?.WindowState == WindowState.Maximized
            ? "\uE923"   // restore
            : "\uE922";  // maximise
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
