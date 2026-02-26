using Moq;
using SimpleWindowsInstallerCleaner.Models;
using SimpleWindowsInstallerCleaner.Services;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner.Tests.ViewModels;

public class RegisteredFilesViewModelTests
{
    private static RegisteredPackage Pkg(string path, string name, string code) =>
        new(path, name, code);

    private static Mock<IMsiFileInfoService> NullInfoService()
    {
        var mock = new Mock<IMsiFileInfoService>();
        mock.Setup(s => s.GetSummaryInfo(It.IsAny<string>())).Returns((MsiSummaryInfo?)null);
        return mock;
    }

    [Fact]
    public void Groups_products_by_ProductCode()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService().Object);

        Assert.Equal(2, vm.Products.Count);
        Assert.All(vm.Products, p => Assert.Equal(0, p.PatchCount));
    }

    [Fact]
    public void Counts_patches_per_product()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch1.msp", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch2.msp", "Product A", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService().Object);

        var product = Assert.Single(vm.Products);
        Assert.Equal(2, product.PatchCount);
        Assert.Equal(2, product.Patches.Count);
    }

    [Fact]
    public void Handles_product_with_only_patches_and_no_msi()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\patch1.msp", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\patch2.msp", "Product A", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService().Object);

        Assert.Single(vm.Products);
    }

    [Fact]
    public void Empty_product_name_becomes_unknown()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "", "{AAA}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 0, NullInfoService().Object);

        Assert.Equal("(unknown)", vm.Products[0].ProductName);
    }

    [Fact]
    public void Summary_shows_total_count_and_size()
    {
        var packages = new List<RegisteredPackage>
        {
            Pkg(@"C:\Windows\Installer\aaa.msi", "Product A", "{AAA}"),
            Pkg(@"C:\Windows\Installer\bbb.msi", "Product B", "{BBB}"),
        };

        var vm = new RegisteredFilesViewModel(packages, 1_048_576, NullInfoService().Object);

        Assert.Equal("2 registered files (1.0 MB)", vm.Summary);
    }
}
