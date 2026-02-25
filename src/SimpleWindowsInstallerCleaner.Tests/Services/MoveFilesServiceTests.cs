using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.Tests.Services;

public class MoveFilesServiceTests : IDisposable
{
    private readonly string _sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MoveFilesServiceTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
    }

    [Fact]
    public async Task MoveFilesAsync_moves_file_to_destination()
    {
        var file = Path.Combine(_sourceDir, "test.msi");
        await File.WriteAllTextAsync(file, "content");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Empty(results.Errors);
        Assert.False(File.Exists(file));
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));
    }

    [Fact]
    public async Task MoveFilesAsync_handles_name_collision_by_appending_number()
    {
        var file1 = Path.Combine(_sourceDir, "test.msi");
        var existing = Path.Combine(_destDir, "test.msi");
        await File.WriteAllTextAsync(file1, "source");
        await File.WriteAllTextAsync(existing, "existing");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file1 }, _destDir);

        Assert.Empty(results.Errors);
        Assert.True(File.Exists(Path.Combine(_destDir, "test.msi")));         // original
        Assert.True(File.Exists(Path.Combine(_destDir, "test (1).msi")));     // moved with suffix
    }

    [Fact]
    public async Task MoveFilesAsync_reports_error_for_missing_source()
    {
        var file = Path.Combine(_sourceDir, "nonexistent.msi");

        var svc = new MoveFilesService();
        var results = await svc.MoveFilesAsync(new[] { file }, _destDir);

        Assert.Single(results.Errors);
        Assert.Equal(file, results.Errors[0].FilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_destDir)) Directory.Delete(_destDir, recursive: true);
    }
}
