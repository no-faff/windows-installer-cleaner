using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IMsiFileInfoService
{
    MsiSummaryInfo? GetSummaryInfo(string filePath);
}
