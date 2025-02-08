using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace C64AssemblerStudio.Core.Services.Implementation;
public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly IDirectoryService _directoryService;
    private readonly IOsDependent _osDependent;

    public FileService(ILogger<FileService> logger, IDirectoryService directoryService, IOsDependent osDependent)
    {
        _logger = logger;
        _directoryService = directoryService;
        _osDependent = osDependent;
    }

    public ImmutableArray<string> ReadAllLines(string path) => [..File.ReadAllLines(path)];

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, text, ct);

    public void Delete(string path) => File.Delete(path);
}
