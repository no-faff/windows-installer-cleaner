namespace SimpleWindowsInstallerCleaner.Services;

public interface IMoveFilesService
{
    Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

public record MoveResult(
    int MovedCount,
    IReadOnlyList<MoveError> Errors);

public record MoveError(string FilePath, string Message);
