using System.IO;
using InstallerClean.Helpers;

namespace InstallerClean.Models;

public record OrphanedFile(
    string FullPath,
    long SizeBytes,
    bool IsPatch,
    string Reason = "Orphaned")
{
    public string FileName => Path.GetFileName(FullPath);
    public string SizeDisplay => DisplayHelpers.FormatSize(SizeBytes);
}
