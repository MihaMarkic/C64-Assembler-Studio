using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using System.Collections.Frozen;
using System.Diagnostics;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class ProjectServices : IProjectServices
{
    private readonly ILogger<ProjectServices> _logger;
    private readonly Globals _globals;
    private readonly Core.Services.Abstract.IFileService _fileService;
    private readonly IOsDependent _osDependent;
    public ProjectServices(ILogger<ProjectServices> logger, Globals globals, Core.Services.Abstract.IFileService fileService, IOsDependent osDependent)
    {
        _logger = logger;
        _globals = globals;
        _fileService = fileService;
        _osDependent = osDependent;
    }

    /// <summary>
    /// Returns all project files.
    /// </summary>
    /// <returns></returns>
    private IParsedFilesIndex<ParsedSourceFile> GetAllProjectFiles()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var parser = project.SourceCodeParser;
        return parser.AllFiles;
    }
    /// <inheritdoc/>
    IEnumerable<string> IProjectServices.CollectSegments()
    {
        var allFiles = GetAllProjectFiles();
        foreach (var k in allFiles.Keys)
        {
            Debug.WriteLine($"Looking at {k}");
            var fileWithSet = allFiles.GetValueOrDefault(k);
            if (fileWithSet is not null)
            {
                foreach (var s in fileWithSet.AllDefineSets)
                {
                    Debug.WriteLine($"\tLooking at set {string.Join(", ", s)}");
                    var parsedSourceFile = allFiles.GetFileOrDefault(k, s);
                    if (parsedSourceFile is not null)
                    {
                        foreach (var si in parsedSourceFile.SegmentDefinitions)
                        {
                            yield return si.Name;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get parsed source file");
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public FrozenSet<string> CollectPreprocessorSymbols()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new HashSet<string>(project.SymbolsDefineSet);
        var allFiles = GetAllProjectFiles();
        foreach (var k in allFiles.Keys)
        {
            Debug.WriteLine($"Looking at {k}");
            var fileWithSet = allFiles.GetValueOrDefault(k);
            if (fileWithSet is not null)
            {
                foreach (var s in fileWithSet.AllDefineSets)
                {
                    Debug.WriteLine($"\tLooking at set {string.Join(", ", s)}");
                    var parsedSourceFile = allFiles.GetFileOrDefault(k, s);
                    if (parsedSourceFile is not null)
                    {
                        result.UnionWith(parsedSourceFile.OutDefines);
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get parsed source file");
                    }
                }
            }
        }
        return result.ToFrozenSet();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="origin"></param>
    /// <param name="rootFileName"></param>
    /// <param name="searchPattern"></param>
    /// <param name="extensions"></param>
    /// <param name="startDirectory"></param>
    /// <param name="relativeDirectory"></param>
    /// <param name="excludedFiles">Excluded files relative paths to either project or library</param>
    internal void AddMatchingFiles(Dictionary<ProjectFileKey, FrozenSet<string>> builder, ProjectFileOrigin origin, 
        string rootFileName, string searchPattern, FrozenSet<string> extensions,
        string startDirectory, string relativeDirectory, FrozenSet<string> excludedFiles)
    {
        var projectFiles = _fileService.GetFilteredFiles(Path.Combine(startDirectory, relativeDirectory), searchPattern, excludedFiles);
        FrozenSet<string> candidateProjectFiles = [
            ..projectFiles
                .Select(f => f.Substring(startDirectory.Length).TrimStart(Path.DirectorySeparatorChar))
                .Where(f =>
                    !excludedFiles.Contains(f)
                    && f.StartsWith(rootFileName, OsDependent.FileStringComparison)
                    && (extensions.Count == 0 || extensions.Contains(Path.GetExtension(f))))
                
        ];
        builder.Add(new(origin, startDirectory), candidateProjectFiles);
    }

    public FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingFiles(string filter, FrozenSet<string> extensions, ICollection<string> excludedFiles)
    {
        string? projectDirectory = _globals.Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        var searchDirectory = Path.GetDirectoryName(filter) ?? string.Empty;
        string directExtension = Path.GetExtension(filter);
        string fileName = Path.GetFileNameWithoutExtension(filter);
        //extension = directExtension.Length > 1 ? $"{directExtension}*" : $".{extension}";
        var searchPattern = $"{fileName}*";
        var excludedFilesSet = excludedFiles.ToFrozenSet(_osDependent.FileStringComparer);
        var builder = new Dictionary<ProjectFileKey, FrozenSet<string>>();

        AddMatchingFiles(builder, ProjectFileOrigin.Project, filter, searchPattern, extensions, projectDirectory, searchDirectory, excludedFilesSet);
        foreach (var library in _globals.Settings.Libraries)
        {
            string libraryPath = library.Value.Path;
            AddMatchingFiles(builder, ProjectFileOrigin.Library, filter, searchPattern, extensions, libraryPath, searchDirectory, excludedFilesSet);
        }

        return builder.ToFrozenDictionary();
    }
    internal void AddMatchingDirectories(Dictionary<ProjectFileKey, FrozenSet<string>> builder, ProjectFileOrigin origin,
        string searchPattern, string startDirectory, string relativeDirectory)
    {
        var path = Path.Combine(startDirectory, relativeDirectory);
        var directories = Directory.GetDirectories(path, searchPattern);
        var relativeDirectories = directories.Select(d => d.Substring(startDirectory.Length).TrimStart(Path.DirectorySeparatorChar));
        builder.Add(new(origin, startDirectory), [.. relativeDirectories.Distinct(_osDependent.FileStringComparer)]);

    }
    public FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingDirectories(string filter)
    {
        string? projectDirectory = _globals.Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        string searchPattern = $"{Path.GetFileNameWithoutExtension(filter)}*";
        var relativeDirectory = Path.GetDirectoryName(filter) ?? "";
        var builder = new Dictionary<ProjectFileKey, FrozenSet<string>>();

        AddMatchingDirectories(builder, ProjectFileOrigin.Project, searchPattern, projectDirectory, relativeDirectory);
        foreach (var library in _globals.Settings.Libraries)
        {
            string libraryPath = library.Value.Path;
            AddMatchingDirectories(builder, ProjectFileOrigin.Library, searchPattern, libraryPath, relativeDirectory);
        }

        return builder.ToFrozenDictionary();
    }
}
