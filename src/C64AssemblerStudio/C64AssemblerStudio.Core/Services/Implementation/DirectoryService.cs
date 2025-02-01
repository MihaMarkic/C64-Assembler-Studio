using C64AssemblerStudio.Core.Services.Abstract;
using System.Diagnostics.CodeAnalysis;

namespace C64AssemblerStudio.Core.Services.Implementation;

public class DirectoryService : IDirectoryService
{
    public string[] GetDirectories(string path, string searchPattern) => Directory.GetDirectories(path, searchPattern);
    public bool Exists([NotNullWhen(true)] string? path) => Directory.Exists(path);
    public string GetCurrentDirectory() => Directory.GetCurrentDirectory();
    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.GetFiles(path, searchPattern, searchOption);
}
