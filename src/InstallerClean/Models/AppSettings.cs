namespace InstallerClean.Models;

public sealed class AppSettings
{
    public string MoveDestination { get; set; } = string.Empty;
    public WindowSize? OrphanedWindowSize { get; set; }
    public WindowSize? RegisteredWindowSize { get; set; }
}

public sealed class WindowSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}
