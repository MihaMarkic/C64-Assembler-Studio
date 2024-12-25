using System.Collections;
using System.Collections.Frozen;
using C64AssemblerStudio.Core.Services.Abstract;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Core.Services.Implementation;
public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    public ImmutableArray<string> ReadAllLines(string path) => [..File.ReadAllLines(path)];

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, text, ct);

    /// <inheritdoc />
    public IEnumerable<string> GetFilteredFiles(string path, string searchPattern, FrozenSet<string> excludedFiles)
    {
        if (Directory.Exists(path))
        {
            string[] allFiles;
            try
            {
                allFiles = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogError("Directory {Directory} does not exist", path);
                yield break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't enumerate files in directory {Directory} with pattern {Pattern}", path,
                    searchPattern);
                yield break;
            }

            var validFiles = allFiles.AsEnumerable();
            if (excludedFiles.Count > 0)
            {
                validFiles = validFiles.Where(f => !excludedFiles.Contains(f));
            }

            foreach (var f in validFiles)
            {
                yield return f;
            }
        }
    }
}
