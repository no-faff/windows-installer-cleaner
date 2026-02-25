using CommunityToolkit.Mvvm.ComponentModel;
using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.ViewModels;

public partial class OrphanedFileViewModel : ObservableObject
{
    public OrphanedFile File { get; }

    [ObservableProperty]
    private bool _isSelected;

    public OrphanedFileViewModel(OrphanedFile file)
    {
        File = file;
    }
}
