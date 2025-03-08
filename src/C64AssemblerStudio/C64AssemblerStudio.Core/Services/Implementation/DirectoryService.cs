using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Core.Services.Implementation;

public class DirectoryService : IDirectoryService
{
    private readonly ILogger<DirectoryService> _logger;
    private readonly IOSDependent _osDependent;
    public DirectoryService(ILogger<DirectoryService> logger, IOSDependent osDependent)
    {
        _logger = logger;
        _osDependent = osDependent;
    }
    public IEnumerable<string> GetDirectories(string path) =>
        Directory.GetDirectories(path);
    //.Select(d => _osDependent.NormalizePath(d));
    public IEnumerable<string> GetDirectories(string path, string searchPattern) =>
            Directory
                .GetDirectories(path, searchPattern);
                //.Select(d => _osDependent.NormalizePath(d));
    public bool Exists([NotNullWhen(true)] string? path) => Directory.Exists(path);
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory(); // _osDependent.NormalizePath(Directory.GetCurrentDirectory());
    public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory
            .GetFiles(path, searchPattern, searchOption);
            //.Select(d => _osDependent.NormalizePath(d));

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void Delete(string path, bool recursive) => Directory.Delete(path, recursive);
    /// <inheritdoc />
    public IEnumerable<string> GetFilteredFiles(string path, string searchPattern, FrozenSet<string> excludedFiles)
    {
        if (Directory.Exists(path))
        {
            IEnumerable<string> allFiles;
            try
            {
                allFiles = GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
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
                    //.Select(f => _osDependent.NormalizePath(f));
            }

            foreach (var f in validFiles)
            {
                yield return f;
            }
        }
    }

    public string[] GetFiles(string path) => Directory.GetFiles(path);
}
