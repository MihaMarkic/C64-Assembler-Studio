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
        /// <inheritdoc />
#pragma warning disable 0067
        public event EventHandler<FilesChangedEventArgs>? FilesChanged;
#pragma  warning restore 0067
        /// <inheritdoc />
        public IParsedFilesIndex<ParsedSourceFile> AllFiles => ImmutableParsedFilesIndex<ParsedSourceFile>.Empty;
        /// <inheritdoc />
        public Task ParsingTask => Task.CompletedTask;
        /// <inheritdoc />
        public Task InitialParseAsync(string projectDirectory, FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent, FrozenSet<string> inDefines,
            ImmutableArray<string> libraryDirectories, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public Task ParseAsync(FrozenDictionary<string, InMemoryFileContent> inMemoryFilesContent, FrozenSet<string> inDefines, ImmutableArray<string> libraryDirectories,
            CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
        /// <inheritdoc />
        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }

    public EmptyProjectViewModel(ILogger<ProjectViewModel<EmptyProject, ParsedSourceFile>> logger,
        ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher)
        : base(logger, settingsManager, systemDialogs, dispatcher, new EmptySourceCodeParser())
    {
    }

    public override Task LoadDebugDataAsync(CancellationToken ct = default) => throw new NotImplementedException();
}