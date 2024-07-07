using C64AssemblerStudio.Core.Services.Abstract;
using System.Collections.Immutable;

namespace C64AssemblerStudio.Core.Services.Implementation;
public class FileService : IFileService
{
    public ImmutableArray<string> ReadAllLines(string path) => File.ReadAllLines(path).ToImmutableArray();
}
