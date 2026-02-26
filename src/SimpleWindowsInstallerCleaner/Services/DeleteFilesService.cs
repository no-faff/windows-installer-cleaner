namespace SimpleWindowsInstallerCleaner.Services;

public sealed class DeleteFilesService : IDeleteFilesService
{
    public Task<DeleteResult> DeleteFilesAsync(
        IEnumerable<string> filePaths,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            int deleted = 0;
            var errors = new List<DeleteError>();

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (!File.Exists(filePath))
                    {
                        errors.Add(new DeleteError(filePath, "File not found."));
                        continue;
                    }
                    progress?.Report($"Deleting {Path.GetFileName(filePath)}...");
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
