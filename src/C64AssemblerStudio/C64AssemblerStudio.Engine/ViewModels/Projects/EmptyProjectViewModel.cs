using System.Collections.Frozen;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels.Projects;

public class EmptyProjectViewModel : ProjectViewModel<EmptyProject, ParsedSourceFile>
{
    private class EmptySourceCodeParser : DisposableObject, ISourceCodeParser<ParsedSourceFile>
    {
        public event EventHandler<FilesChangedEventArgs>? FilesChanged;
        public IParsedFilesIndex<ParsedSourceFile> AllFiles => ImmutableParsedFilesIndex<ParsedSourceFile>.Empty;

        public Task InitialParseAsync(string projectDirectory, FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent, FrozenSet<string> inDefines,
            ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task ParseAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent, FrozenSet<string> inDefines, ImmutableArray<string> libraryDirectories,
            CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }
    public EmptyProjectViewModel(ILogger<ProjectViewModel<EmptyProject, ParsedSourceFile>> logger, ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher)
        : base(logger, settingsManager, systemDialogs, dispatcher, new EmptySourceCodeParser())
    {
    }

    public override Task LoadDebugDataAsync(CancellationToken ct = default) => throw new NotImplementedException();
}