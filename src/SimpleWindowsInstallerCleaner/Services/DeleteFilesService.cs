using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            int deleted = 0;
            var errors = new List<DeleteError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var filePath = pathList[i];

                try
                {
                    if (!File.Exists(filePath))
                    {
                        errors.Add(new DeleteError(filePath, "File not found."));
                        continue;
                    }
                    var fileName = Path.GetFileName(filePath);
                    progress?.Report(new OperationProgress(i + 1, total, fileName));
                    File.Delete(filePath);
                    deleted++;
                }
                catch (Exception ex)
                {
                    errors.Add(new DeleteError(filePath, ex.Message));
                }
            }

            return new DeleteResult(deleted, errors.AsReadOnly());
        }, cancellationToken);
    }
}
