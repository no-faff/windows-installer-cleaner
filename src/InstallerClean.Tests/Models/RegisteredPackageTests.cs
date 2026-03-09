using InstallerClean.Models;

namespace InstallerClean.Tests.Models;

/// <summary>
/// Tests the RegisteredPackage record, which models the data transformation
/// logic produced by InstallerQueryService. The patch state and removability
/// rules are critical to safe orphan detection.
/// </summary>
public class RegisteredPackageTests
{
    [Fact]
    public void Default_PatchState_is_zero()
    {
        var pkg = new RegisteredPackage(@"C:\Windows\Installer\test.msi", "Product", "{AAA}");

        Assert.Equal(0, pkg.PatchState);
    }

    [Fact]
    public void Default_IsRemovable_is_false()
    {
        var pkg = new RegisteredPackage(@"C:\Windows\Installer\test.msi", "Product", "{AAA}");

        Assert.False(pkg.IsRemovable);
    }

    [Theory]
    [InlineData(0, false)]   // not a patch - never removable by default
    [InlineData(1, false)]   // applied - still needed
    [InlineData(2, true)]    // superseded - safe to remove
    [InlineData(4, true)]    // obsoleted - safe to remove
    public void Removability_matches_expected_for_patch_state(int patchState, bool expectedRemovable)
    {
        // This mirrors the logic in InstallerQueryService:
        // isSuperseded = patchState is 2 or 4
        // isRemovable = isSuperseded && !isUninstallable
        var isSuperseded = patchState is 2 or 4;
        var isRemovable = isSuperseded; // assuming not uninstallable

        var pkg = new RegisteredPackage(
            @"C:\Windows\Installer\patch.msp", "Product", "{AAA}",
            patchState, isRemovable);

        Assert.Equal(expectedRemovable, pkg.IsRemovable);
    }

    [Fact]
    public void Superseded_but_uninstallable_is_not_removable()
    {
        // InstallerQueryService sets isRemovable = isSuperseded && !isUninstallable.
        // When Uninstallable="1", the patch is kept (can be individually uninstalled).
        var patchState = 2; // superseded
        var isUninstallable = true;
        var isRemovable = (patchState is 2 or 4) && !isUninstallable;

        var pkg = new RegisteredPackage(
            @"C:\Windows\Installer\patch.msp", "Product", "{AAA}",
            patchState, isRemovable);

        Assert.False(pkg.IsRemovable);
    }

    [Fact]
    public void Record_equality_by_value()
    {
        var a = new RegisteredPackage(@"C:\Windows\Installer\test.msi", "Product", "{AAA}");
        var b = new RegisteredPackage(@"C:\Windows\Installer\test.msi", "Product", "{AAA}");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Record_inequality_on_path_difference()
    {
        var a = new RegisteredPackage(@"C:\Windows\Installer\a.msi", "Product", "{AAA}");
        var b = new RegisteredPackage(@"C:\Windows\Installer\b.msi", "Product", "{AAA}");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Record_inequality_on_removable_difference()
    {
        var a = new RegisteredPackage(@"C:\Windows\Installer\a.msp", "Product", "{AAA}", 2, true);
        var b = new RegisteredPackage(@"C:\Windows\Installer\a.msp", "Product", "{AAA}", 2, false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Empty_product_name_and_code_is_valid()
    {
        // Registry fallback entries may have empty name and code
        var pkg = new RegisteredPackage(@"C:\Windows\Installer\fallback.msi", "", "");

        Assert.Equal("", pkg.ProductName);
        Assert.Equal("", pkg.ProductCode);
        Assert.Equal(0, pkg.PatchState);
        Assert.False(pkg.IsRemovable);
    }

    [Fact]
    public void Case_sensitive_path_comparison_in_record_equality()
    {
        // Record equality is case-sensitive by default
        var lower = new RegisteredPackage(@"c:\windows\installer\test.msi", "Product", "{AAA}");
        var upper = new RegisteredPackage(@"C:\Windows\Installer\test.msi", "Product", "{AAA}");

        // Records use ordinal comparison, so these are NOT equal
        Assert.NotEqual(lower, upper);
    }
}
