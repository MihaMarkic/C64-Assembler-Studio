using C64AssemblerStudio.Core.Services.Abstract;
using System.Collections.Immutable;

namespace C64AssemblerStudio.Core.Services.Implementation;
public class FileService : IFileService
{
    public ImmutableArray<string> ReadAllLines(string path) => [..File.ReadAllLines(path)];

    public Task<string> ReadAllTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, ct);

    public Task WriteAllTextAsync(string path, string text, CancellationToken ct = default)
        => File.WriteAllTextAsync(path, text, ct);
}
