using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;

namespace SimpleWindowsInstallerCleaner.Tests.Services;

public class ExclusionServiceTests
{
    private static OrphanedFile File(string name) =>
        new($@"C:\Windows\Installer\{name}", 1024 * 1024, name.EndsWith(".msp"));

    private readonly ExclusionService _svc = new();

    [Fact]
    public void ApplyFilters_separates_matching_files()
    {
        var files = new[] { File("AdobePatch.msp"), File("OfficeUpdate.msi") };
        var filters = new[] { "Adobe" };

        var result = _svc.ApplyFilters(files, filters);

        Assert.Single(result.Excluded);
        Assert.Equal("AdobePatch.msp", result.Excluded[0].FileName);
        Assert.Single(result.Actionable);
        Assert.Equal("OfficeUpdate.msi", result.Actionable[0].FileName);
    }

    [Fact]
    public void ApplyFilters_is_case_insensitive()
    {
        var files = new[] { File("ADOBE_PATCH.msp") };
        var filters = new[] { "adobe" };

        var result = _svc.ApplyFilters(files, filters);

        Assert.Single(result.Excluded);
        Assert.Empty(result.Actionable);
    }

    [Fact]
    public void ApplyFilters_with_no_filters_returns_all_as_actionable()
    {
        var files = new[] { File("AdobePatch.msp"), File("OfficeUpdate.msi") };

        var result = _svc.ApplyFilters(files, Array.Empty<string>());

        Assert.Equal(2, result.Actionable.Count);
        Assert.Empty(result.Excluded);
    }

    [Fact]
    public void ApplyFilters_with_multiple_filters()
    {
        var files = new[] { File("AdobePatch.msp"), File("AcrobatUpdate.msi"), File("OfficeUpdate.msi") };
        var filters = new[] { "Adobe", "Acrobat" };

        var result = _svc.ApplyFilters(files, filters);

        Assert.Equal(2, result.Excluded.Count);
        Assert.Single(result.Actionable);
        Assert.Equal("OfficeUpdate.msi", result.Actionable[0].FileName);
    }

    [Fact]
    public void ApplyFilters_no_match_returns_all_actionable()
    {
        var files = new[] { File("OfficeUpdate.msi"), File("VS2022.msi") };
        var filters = new[] { "Adobe" };

        var result = _svc.ApplyFilters(files, filters);

        Assert.Equal(2, result.Actionable.Count);
        Assert.Empty(result.Excluded);
    }
}
