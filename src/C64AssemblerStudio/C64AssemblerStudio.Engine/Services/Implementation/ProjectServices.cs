using Antlr4.Runtime;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Models.Projects;
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

    IEnumerable<string> IProjectServices.CollectSegments()
    {
        var project = (IProjectViewModel<ParsedSourceFile>)_globals.Project;
        var parser = project.SourceCodeParser;
        var allFiles = parser.AllFiles;
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

    public FrozenDictionary<string, FrozenSet<string>> GetMatchingFiles(string filter, FrozenSet<string> extensions, ICollection<string> excludedFiles)
    {
        string? projectDirectory = _globals.Project.Directory;
        ArgumentNullException.ThrowIfNull(projectDirectory);
        var searchDirectory = Path.GetDirectoryName(filter) ?? string.Empty;
        string directExtension = Path.GetExtension(filter);
        string fileName = Path.GetFileNameWithoutExtension(filter);
        //extension = directExtension.Length > 1 ? $"{directExtension}*" : $".{extension}";
        var searchPattern = $"{fileName}*";
        var excludedFilesSet = excludedFiles.ToFrozenSet(_osDependent.FileStringComparer);
        var projectFiles = _fileService.GetFilteredFiles(Path.Combine(projectDirectory, searchDirectory), searchPattern,
            excludedFilesSet);
        var candidateProjectFiles = projectFiles
            .Where(f => Path.GetFileName(f).StartsWith(filter, OsDependent.FileStringComparison))
            .ToFrozenSet();
        var builder = new Dictionary<string, FrozenSet<string>>
        {
            { "Project", candidateProjectFiles },
        };
        foreach (var library in _globals.Settings.Libraries)
        {
            var libraryFiles = _fileService.GetFilteredFiles(Path.Combine(library.Value.Path, searchDirectory),
                searchPattern, excludedFilesSet);
            var validLibraryFiles = libraryFiles
                .Where(f => Path.GetFileName(f).StartsWith(filter, OsDependent.FileStringComparison))
                .ToFrozenSet();
            builder.Add(library.Key, validLibraryFiles);
        }

        return builder.ToFrozenDictionary(_osDependent.FileStringComparer);
    }
}
