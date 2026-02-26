using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
