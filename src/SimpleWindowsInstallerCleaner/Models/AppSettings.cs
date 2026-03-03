namespace SimpleWindowsInstallerCleaner.Models;

public sealed class AppSettings
{
    public string MoveDestination { get; set; } = string.Empty;
    public List<string> ExclusionFilters { get; set; } = new();
}
