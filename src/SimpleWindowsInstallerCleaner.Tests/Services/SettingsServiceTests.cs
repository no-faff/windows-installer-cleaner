using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"settings-test-{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void Load_returns_defaults_when_no_file_exists()
    {
        var svc = new SettingsService(_tempFile);

        var settings = svc.Load();

        Assert.Equal(string.Empty, settings.MoveDestination);
        Assert.Contains("Adobe", settings.ExclusionFilters);
        Assert.Contains("Acrobat", settings.ExclusionFilters);
        Assert.True(settings.CheckPendingReboot);
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var svc = new SettingsService(_tempFile);
        var original = new AppSettings
        {
            MoveDestination = @"D:\Backup",
            ExclusionFilters = new List<string> { "Adobe", "Norton" },
            CheckPendingReboot = false
        };

        svc.Save(original);
        var loaded = svc.Load();

        Assert.Equal(@"D:\Backup", loaded.MoveDestination);
        Assert.Equal(new[] { "Adobe", "Norton" }, loaded.ExclusionFilters);
        Assert.False(loaded.CheckPendingReboot);
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_corrupt()
    {
        File.WriteAllText(_tempFile, "this is not valid json {{{");
        var svc = new SettingsService(_tempFile);

        var settings = svc.Load();

        Assert.Equal(string.Empty, settings.MoveDestination);
        Assert.Contains("Adobe", settings.ExclusionFilters);
    }
}
