using System.Diagnostics.CodeAnalysis;

namespace C64AssemblerStudio.Core.Services.Abstract;

public interface IDirectoryService
{
    bool Exists([NotNullWhen(true)] string? path);
    string GetCurrentDirectory();
    string[] GetDirectories(string path, string searchPattern);
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
}
