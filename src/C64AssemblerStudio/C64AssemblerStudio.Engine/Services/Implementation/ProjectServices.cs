using System.Collections.Frozen;
using System.Diagnostics;
using C64AssemblerStudio.Core.Extensions;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class ProjectServices : IProjectServices
{
    private readonly ILogger<ProjectServices> _logger;
    private readonly Globals _globals;
    private readonly Core.Services.Abstract.IFileService _fileService;
    private readonly IOsDependent _osDependent;
    private readonly IDirectoryService _directoryService;
    public ProjectServices(ILogger<ProjectServices> logger, Globals globals, Core.Services.Abstract.IFileService fileService, IOsDependent osDependent, IDirectoryService directoryService)
    {
        _logger = logger;
        _globals = globals;
        _fileService = fileService;
        _osDependent = osDependent;
        _directoryService = directoryService;
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
        foreach (var f in IterateAllParsedSourceFiles())
        {
            foreach (var si in f.SegmentDefinitions)
            {
                yield return si.Name;
            }
        }
    }
    private IEnumerable<ParsedSourceFile> IterateAllParsedSourceFiles()
    {
        var allFiles = GetAllProjectFiles();
        foreach (var k in allFiles.Keys)
        {
            //Debug.WriteLine($"Looking at {k}");
            var fileWithSet = allFiles.GetValueOrDefault(k);
            if (fileWithSet is not null)
            {
                foreach (var s in fileWithSet.AllDefineSets)
                {
                    //Debug.WriteLine($"\tLooking at set {string.Join(", ", s)}");
                    var parsedSourceFile = allFiles.GetFileOrDefault(k, s);
                    if (parsedSourceFile is not null)
                    {
                        yield return parsedSourceFile;
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
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.UnionWith(f.OutDefines);
        }
        return [..result];
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
        var startDirectoryPath = Path.Combine(startDirectory, relativeDirectory);
        var projectFiles = _directoryService.GetFilteredFiles(startDirectoryPath, searchPattern, excludedFiles);
        FrozenSet<string> candidateProjectFiles = [
            ..projectFiles
                .Select(f => f.Substring(startDirectory.Length).TrimStart(Path.DirectorySeparatorChar))
                .Where(f =>
                    !excludedFiles.Contains(f)
                    && f.PathStartsWithSeparatorAgnostic(rootFileName, _osDependent.FileStringComparison)
                    && (extensions.Count == 0 || extensions.Contains(Path.GetExtension(f))))
                
        ];
        builder.Add(new(origin, startDirectory), candidateProjectFiles);
    }

    public FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingFiles(
        string relativeFilePath, string filter, FrozenSet<string> extensions, ICollection<string> excludedFiles)
    {
        string? projectDirectory = _globals.Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        var searchDirectory = GetSearchDirectoryForFileMatching(filter) ?? string.Empty;
        string directExtension = Path.GetExtension(filter);
        string fileName = GetSearchFileNameForFileMatching(filter);
        //extension = directExtension.Length > 1 ? $"{directExtension}*" : $".{extension}";
        var searchPattern = $"{fileName}*";
        var excludedFilesSet = excludedFiles.ToFrozenSet(_osDependent.FileStringComparer);
        var builder = new Dictionary<ProjectFileKey, FrozenSet<string>>();

        var projectDirectoryWithRelativeFilePath = Path.Combine(projectDirectory, relativeFilePath);
        AddMatchingFiles(builder, ProjectFileOrigin.Project, filter, searchPattern, extensions,
            projectDirectoryWithRelativeFilePath, searchDirectory, excludedFilesSet);
        foreach (var library in _globals.Settings.Libraries)
        {
            string libraryPath = Path.Combine(library.Value.Path, relativeFilePath);
            AddMatchingFiles(builder, ProjectFileOrigin.Library, filter, searchPattern, extensions, libraryPath, searchDirectory, excludedFilesSet);
        }

        return builder.ToFrozenDictionary();
    }
    internal string GetSearchDirectoryForFileMatching(string filter)
    {
        if (filter.EndsWith(".."))
        {
            return filter;
        }
        return Path.GetDirectoryName(filter) ?? string.Empty;
    }
    internal string GetSearchFileNameForFileMatching(string filter)
    {
        if (filter.EndsWith(".."))
        {
            return "";
        }
        return Path.GetFileNameWithoutExtension(filter);
    }
    internal void AddMatchingDirectories(Dictionary<ProjectFileKey, FrozenSet<string>> builder, ProjectFileOrigin origin,
        string searchPattern, string startDirectory, string relativeDirectory, string searchDirectory)
    {
        var pathUpToRelative = Path.Combine(startDirectory, relativeDirectory);
        var path = Path.Combine(pathUpToRelative, searchDirectory);
        if (_directoryService.Exists(path))
        {
            var directories = _directoryService.GetDirectories(path, searchPattern);
            var relativeDirectories = directories.Select(d =>  d.Substring(pathUpToRelative.Length).TrimStart(Path.DirectorySeparatorChar));
            builder.Add(new(origin, startDirectory), [.. relativeDirectories.Distinct(_osDependent.FileStringComparer)]);
        }
    }
    public FrozenDictionary<ProjectFileKey, FrozenSet<string>> GetMatchingDirectories(string relativeFilePath, string filter)
    {
        string? projectDirectory = _globals.Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        string searchPattern = $"{Path.GetFileNameWithoutExtension(filter)}*";
        var relativeDirectory = Path.GetDirectoryName(filter) ?? "";
        var builder = new Dictionary<ProjectFileKey, FrozenSet<string>>();

        AddMatchingDirectories(builder, ProjectFileOrigin.Project, searchPattern, projectDirectory, relativeFilePath, relativeDirectory);
        foreach (var library in _globals.Settings.Libraries)
        {
            string libraryPath = library.Value.Path;
            AddMatchingDirectories(builder, ProjectFileOrigin.Library, searchPattern, libraryPath, relativeFilePath, relativeDirectory);
        }

        return builder.ToFrozenDictionary();
    }
    /// <inheritdoc/>
    public ImmutableList<Label> CollectLabels()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new HashSet<Label>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.UnionWith(f.LabelDefinitions);
        }
        return [.. result];
    }
    /// <inheritdoc/>
    public ImmutableList<string> CollectVariables()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new HashSet<string>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.UnionWith(f.VariableDefinitions);
        }
        return [.. result];
    }
    /// <inheritdoc/>
    public ImmutableList<Constant> CollectConstants()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new HashSet<Constant>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.UnionWith(f.ConstantDefinitions);
        }
        return [.. result];
    }
    /// <inheritdoc/>
    public ImmutableList<EnumValues> CollectEnumValues()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new List<EnumValues>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.AddRange(f.EnumValuesDefinitions);
        }
        return [.. result];
    }
    /// <inheritdoc/>
    public ImmutableList<Macro> CollectMacros()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new List<Macro>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.AddRange(f.MacroDefinitions);
        }
        return [.. result];
    }
    /// <inheritdoc/>
    public ImmutableList<Function> CollectFunctions()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var result = new List<Function>();
        foreach (var f in IterateAllParsedSourceFiles())
        {
            result.AddRange(f.FunctionDefinitions);
        }
        return [.. result];
    }
}
