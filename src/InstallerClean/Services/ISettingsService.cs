using InstallerClean.Models;

namespace InstallerClean.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
