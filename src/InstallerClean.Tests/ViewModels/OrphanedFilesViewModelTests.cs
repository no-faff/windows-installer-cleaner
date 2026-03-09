using NSubstitute;
using InstallerClean.Models;
using InstallerClean.Services;
using InstallerClean.ViewModels;

namespace InstallerClean.Tests.ViewModels;

public class OrphanedFilesViewModelTests
{
    private static IMsiFileInfoService NullInfoService()
    {
        var mock = Substitute.For<IMsiFileInfoService>();
        mock.GetSummaryInfo(Arg.Any<string>()).Returns((MsiSummaryInfo?)null);
        return mock;
    }

    [Fact]
    public void Files_sorted_by_size_descending()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\small.msi", 100, false),
            new(@"C:\Windows\Installer\large.msi", 10_000, false),
            new(@"C:\Windows\Installer\medium.msi", 1_000, false),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("large.msi", vm.Files[0].FileName);
        Assert.Equal("medium.msi", vm.Files[1].FileName);
        Assert.Equal("small.msi", vm.Files[2].FileName);
    }

    [Fact]
    public void Summary_shows_count_and_total_size()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 524_288, false),
            new(@"C:\Windows\Installer\b.msi", 524_288, false),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("2 files (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void Summary_singular_for_one_file()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1_048_576, false),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.Equal("1 file (1.0 MB)", vm.Summary);
    }

    [Fact]
    public void First_file_is_selected_by_default()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\small.msi", 100, false),
            new(@"C:\Windows\Installer\large.msi", 10_000, false),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.NotNull(vm.SelectedFile);
        Assert.Equal("large.msi", vm.SelectedFile!.FileName); // largest first
    }

    [Fact]
    public void Empty_list_has_no_selection()
    {
        var vm = new OrphanedFilesViewModel(
            new List<OrphanedFile>(), NullInfoService());

        Assert.Null(vm.SelectedFile);
        Assert.False(vm.HasSelection);
    }

    [Fact]
    public void HasSelection_true_when_file_selected()
    {
        var files = new List<OrphanedFile>
        {
            new(@"C:\Windows\Installer\a.msi", 1024, false),
        };

        var vm = new OrphanedFilesViewModel(files, NullInfoService());

        Assert.True(vm.HasSelection);
    }
}
