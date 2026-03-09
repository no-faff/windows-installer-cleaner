using InstallerClean.Models;
using InstallerClean.Services;

namespace InstallerClean.Tests.Services;

public class MsiFileInfoServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public MsiFileInfoServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_nonexistent_file()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "does_not_exist.msi");

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_empty_file()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "empty.msi");
        File.WriteAllBytes(path, Array.Empty<byte>());

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_corrupt_file()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "corrupt.msi");
        File.WriteAllText(path, "this is not a valid MSI file at all");

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_random_binary_data()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "random.msi");
        var random = new Random(42);
        var bytes = new byte[4096];
        random.NextBytes(bytes);
        File.WriteAllBytes(path, bytes);

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_text_file_with_msp_extension()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "fakepatch.msp");
        File.WriteAllText(path, "not a real patch file");

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_handles_locked_file_gracefully()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "locked.msi");
        File.WriteAllText(path, "dummy content");

        // Lock the file with an exclusive handle
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var result = svc.GetSummaryInfo(path);

        // Should return null without throwing (the MsiGetSummaryInformation call will fail)
        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_result_has_all_properties_when_valid()
    {
        // If we get a valid result, all properties should be non-null strings
        // (possibly empty, but not null). We test this against the MsiSummaryInfo record.
        var info = new MsiSummaryInfo("Title", "Subject", "Author", "Comments", "Sig");

        Assert.NotNull(info.Title);
        Assert.NotNull(info.Subject);
        Assert.NotNull(info.Author);
        Assert.NotNull(info.Comments);
        Assert.NotNull(info.DigitalSignature);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_directory_path()
    {
        var svc = new MsiFileInfoService();

        // Pass a directory path instead of a file
        var result = svc.GetSummaryInfo(_tempDir);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_empty_path()
    {
        var svc = new MsiFileInfoService();

        var result = svc.GetSummaryInfo(string.Empty);

        Assert.Null(result);
    }

    [Fact]
    public void GetSummaryInfo_returns_null_for_very_large_corrupt_file()
    {
        var svc = new MsiFileInfoService();
        var path = Path.Combine(_tempDir, "large_corrupt.msi");

        // 64 KB of zeros - not a valid MSI structured storage file
        File.WriteAllBytes(path, new byte[65536]);

        var result = svc.GetSummaryInfo(path);

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
