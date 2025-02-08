using System.Collections.Immutable;

namespace C64AssemblerStudio.Core.Services.Abstract;
public interface IFileService
{
    ImmutableArray<string> ReadAllLines(string path);
    Task<string> ReadAllTextAsync(string path, CancellationToken ct = default);
    Task WriteAllTextAsync(string path, string text, CancellationToken ct = default);
    void Delete(string path);
}
