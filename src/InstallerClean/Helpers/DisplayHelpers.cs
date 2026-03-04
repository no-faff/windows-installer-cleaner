using System.Reflection;

namespace InstallerClean.Helpers;

internal static class DisplayHelpers
{
    internal static string GetVersionString()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is not null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    }

    internal static string FormatSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{bytes / 1_024.0:F1} KB",
        _ => $"{bytes} B"
    };

    internal static string Pluralise(int count, string singular, string plural) =>
        count == 1 ? singular : plural;
}
