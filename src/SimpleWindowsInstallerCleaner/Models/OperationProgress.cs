namespace SimpleWindowsInstallerCleaner.Models;

public sealed record OperationProgress(
    int CurrentFile,
    int TotalFiles,
    string CurrentFileName);
