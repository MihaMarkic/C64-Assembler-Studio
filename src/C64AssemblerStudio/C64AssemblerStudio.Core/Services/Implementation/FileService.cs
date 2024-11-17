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
    public ImmutableArray<string> GetFilteredFiles(string path, string searchPattern, string? excludedFile = null)
    {
        if (Directory.Exists(path))
        {
            try
            {
                var files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
                if (excludedFile is not null)
                {
                    return [..files.Where(f => !f.Equals(excludedFile, OsDependent.FileStringComparison))];
                }
                return [..files];
            }
            catch (DirectoryNotFoundException)
            {
                _logger.LogError("Directory {Directory} does not exist", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't enumerate files in directory {Directory} with pattern {Pattern}", path,
                    searchPattern);
            }
        }

        return ImmutableArray<string>.Empty;
    }
}
