using InstallerClean.Helpers;

namespace InstallerClean.Tests.Helpers;

public class DisplayHelpersTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1_023, "1023 B")]
    [InlineData(1_024, "1.0 KB")]
    [InlineData(5_500, "5.4 KB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(52_428_800, "50.0 MB")]
    [InlineData(1_073_741_824, "1.00 GB")]
    [InlineData(5_368_709_120, "5.00 GB")]
    [InlineData(107_374_182_400, "100.00 GB")]
    public void FormatSize_formats_correctly(long bytes, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.FormatSize(bytes));
    }

    [Theory]
    [InlineData(0, "files")]
    [InlineData(1, "file")]
    [InlineData(2, "files")]
    [InlineData(100, "files")]
    public void Pluralise_returns_correct_form(int count, string expected)
    {
        Assert.Equal(expected, DisplayHelpers.Pluralise(count, "file", "files"));
    }
}
