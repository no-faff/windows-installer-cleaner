using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

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
    }

    [Fact]
    public void Save_then_Load_round_trips()
    {
        var svc = new SettingsService(_tempFile);
        var original = new AppSettings
        {
            MoveDestination = @"D:\Backup"
        };

        svc.Save(original);
        var loaded = svc.Load();

        Assert.Equal(@"D:\Backup", loaded.MoveDestination);
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_corrupt()
    {
        File.WriteAllText(_tempFile, "this is not valid json {{{");
        var svc = new SettingsService(_tempFile);

        var settings = svc.Load();

        Assert.Equal(string.Empty, settings.MoveDestination);
    }
}
