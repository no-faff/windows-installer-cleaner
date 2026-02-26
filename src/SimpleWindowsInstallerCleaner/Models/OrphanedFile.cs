using System.IO;

namespace SimpleWindowsInstallerCleaner.Models;

/// <summary>
/// A file found in C:\Windows\Installer that is not registered with the
/// Windows Installer API and is therefore safe to move or delete.
/// </summary>
public record OrphanedFile(
    /// <summary>Full path to the file.</summary>
    string FullPath,
    /// <summary>File size in bytes.</summary>
    long SizeBytes,
    /// <summary>True for .msp patch files; false for .msi.</summary>
    bool IsPatch)
{
    /// <summary>File name without directory.</summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>Human-readable file size (e.g. "14.2 MB").</summary>
    public string SizeDisplay => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{SizeBytes / 1_048_576.0:F1} MB",
        >= 1_024 => $"{SizeBytes / 1_024.0:F1} KB",
        _ => $"{SizeBytes} B"
    };

    /// <summary>File type label: ".msp" for patches, ".msi" for installer packages.</summary>
    public string TypeLabel => IsPatch ? ".msp" : ".msi";
}
