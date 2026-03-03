namespace SimpleWindowsInstallerCleaner.Services;

internal static class InstallerCacheHelpers
{
    private static readonly string InstallerFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");

    /// <summary>
    /// Deletes empty subdirectories inside C:\Windows\Installer.
    /// Processes deepest first so nested empty trees collapse in one pass.
    /// </summary>
    internal static void PruneEmptySubdirectories()
    {
        if (!Directory.Exists(InstallerFolder)) return;

        foreach (var dir in Directory.EnumerateDirectories(InstallerFolder, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length)) // deepest first
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
            catch { /* skip protected directories */ }
        }
    }
}
