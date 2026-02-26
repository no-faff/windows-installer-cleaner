namespace SimpleWindowsInstallerCleaner.Models;

/// <summary>
/// Metadata read from an MSI/MSP file's Summary Information Stream.
/// </summary>
public record MsiSummaryInfo(
    string Title,
    string Subject,
    string Author,
    string Comments,
    string DigitalSignature);
