using InstallerClean.Models;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Tests the MsiSummaryInfo record, which holds metadata read from
/// MSI/MSP files by MsiFileInfoService.
/// </summary>
public class MsiSummaryInfoTests
{
    [Fact]
    public void Constructor_sets_all_properties()
    {
        var info = new MsiSummaryInfo(
            "Installation Database",
            "Microsoft Office",
            "Microsoft Corporation",
            "This is a comment",
            "CN=Microsoft");

        Assert.Equal("Installation Database", info.Title);
        Assert.Equal("Microsoft Office", info.Subject);
        Assert.Equal("Microsoft Corporation", info.Author);
        Assert.Equal("This is a comment", info.Comments);
        Assert.Equal("CN=Microsoft", info.DigitalSignature);
    }

    [Fact]
    public void Empty_strings_are_valid()
    {
        var info = new MsiSummaryInfo("", "", "", "", "");

        Assert.Equal("", info.Title);
        Assert.Equal("", info.Subject);
        Assert.Equal("", info.Author);
        Assert.Equal("", info.Comments);
        Assert.Equal("", info.DigitalSignature);
    }

    [Fact]
    public void Record_equality_by_value()
    {
        var a = new MsiSummaryInfo("T", "S", "A", "C", "D");
        var b = new MsiSummaryInfo("T", "S", "A", "C", "D");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_inequality_on_any_property_difference()
    {
        var baseline = new MsiSummaryInfo("T", "S", "A", "C", "D");

        Assert.NotEqual(baseline, new MsiSummaryInfo("X", "S", "A", "C", "D"));
        Assert.NotEqual(baseline, new MsiSummaryInfo("T", "X", "A", "C", "D"));
        Assert.NotEqual(baseline, new MsiSummaryInfo("T", "S", "X", "C", "D"));
        Assert.NotEqual(baseline, new MsiSummaryInfo("T", "S", "A", "X", "D"));
        Assert.NotEqual(baseline, new MsiSummaryInfo("T", "S", "A", "C", "X"));
    }

    [Fact]
    public void With_expression_creates_modified_copy()
    {
        var original = new MsiSummaryInfo("T", "S", "A", "C", "D");
        var modified = original with { Author = "New Author" };

        Assert.Equal("New Author", modified.Author);
        Assert.Equal("T", modified.Title);
        Assert.NotEqual(original, modified);
    }
}
