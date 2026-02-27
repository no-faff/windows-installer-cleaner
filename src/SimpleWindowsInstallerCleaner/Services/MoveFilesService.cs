using SimpleWindowsInstallerCleaner.Models;

namespace SimpleWindowsInstallerCleaner.Services;

public sealed class MoveFilesService : IMoveFilesService
{
    public Task<MoveResult> MoveFilesAsync(
        IEnumerable<string> filePaths,
        string destinationFolder,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(destinationFolder);

            int moved = 0;
            var errors = new List<MoveError>();
            var pathList = filePaths as IReadOnlyList<string> ?? filePaths.ToList();
            var total = pathList.Count;

            for (int i = 0; i < total; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourcePath = pathList[i];

                try
                {
                    var fileName = Path.GetFileName(sourcePath);
                    var destPath = GetUniqueDestPath(destinationFolder, fileName);
                    progress?.Report(new OperationProgress(i + 1, total, fileName));
                    File.Move(sourcePath, destPath);
                    moved++;
                }
                catch (Exception ex)
                {
                    errors.Add(new MoveError(sourcePath, ex.Message));
                }
            }

            return new MoveResult(moved, errors.AsReadOnly());
        }, cancellationToken);
    }

    private static string GetUniqueDestPath(string folder, string fileName)
    {
        var candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate)) return candidate;

        var nameWithout = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        for (int i = 1; ; i++)
        {
            candidate = Path.Combine(folder, $"{nameWithout} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
