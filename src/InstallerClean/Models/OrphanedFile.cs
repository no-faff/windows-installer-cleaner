using System.IO;
using InstallerClean.Helpers;

namespace InstallerClean.Models;

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
    bool IsPatch,
    /// <summary>Why this file is removable: "Orphaned" or "Superseded".</summary>
    string Reason = "Orphaned")
{
    /// <summary>File name without directory.</summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>Human-readable file size (e.g. "14.2 MB").</summary>
    public string SizeDisplay => DisplayHelpers.FormatSize(SizeBytes);

    /// <summary>File type label: ".msp" for patches, ".msi" for installer packages.</summary>
    public string TypeLabel => IsPatch ? ".msp" : ".msi";
}
