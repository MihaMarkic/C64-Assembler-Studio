using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace C64AssemblerStudio.Core.Services.Abstract;

public interface IDirectoryService
{
    bool Exists([NotNullWhen(true)] string? path);
    string GetCurrentDirectory();
    IEnumerable<string> GetDirectories(string path, string searchPattern);
    IEnumerable<string> GetDirectories(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption);
    string[] GetFiles(string path);
    void CreateDirectory(string path);
    void Delete(string path, bool recursive);
    /// <summary>
    /// Finds all file names that match <param name="searchPattern"/> within directory <param name="path"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="searchPattern"></param>
    /// <param name="excludedFiles">Full file name to be omitted from results.</param>
    /// <returns>An <see cref="ImmutableArray&lt;String&gt;"/> with all matched file names.</returns>
    /// <remarks>
    /// Excluded file <param name="excludedFiles"/> should be excluded from results. Make sure it uses a proper file comparer
    /// from <see cref="OsDependent"/> support class.
    /// </remarks>
    IEnumerable<string> GetFilteredFiles(string path, string searchPattern, FrozenSet<string> excludedFiles);
}
