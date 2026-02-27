using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public interface IDeleteFilesService
{
    Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record DeleteResult(int DeletedCount, IReadOnlyList<DeleteError> Errors);
public record DeleteError(string FilePath, string Message);
