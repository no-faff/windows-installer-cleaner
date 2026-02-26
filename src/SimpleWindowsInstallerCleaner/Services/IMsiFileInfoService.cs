using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface IMsiFileInfoService
{
    MsiSummaryInfo? GetSummaryInfo(string filePath);
}
