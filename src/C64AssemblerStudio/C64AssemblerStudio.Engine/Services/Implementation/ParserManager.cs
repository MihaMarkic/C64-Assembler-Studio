using System.Collections.Frozen;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Files;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.Services.Implementation;

/// <inheritdoc cref="IParserManager"/>
/// <remarks>
/// Handles project settings changes actively.
/// </remarks>
public class ParserManager : DisposableObject, IParserManager
{
    private readonly Globals _globals;
    private readonly FilesViewModel _filesViewModel;
    private readonly ISubscription _projectChangedSubscription;
    private Settings Settings => _globals.Settings;
    private IProjectViewModel<ParsedSourceFile> Project => (IProjectViewModel<ParsedSourceFile>)_globals.Project;
    public ParserManager(Globals globals, FilesViewModel filesViewModel, IDispatcher dispatcher)
    {
        _globals = globals;
        _filesViewModel = filesViewModel;
        _projectChangedSubscription = dispatcher.Subscribe<ProjectSettingsChangedMessage>(ProjectSettingsChanged);
    }
    private async Task ProjectSettingsChanged(ProjectSettingsChangedMessage message, CancellationToken ct)
    {
        await ReparseChangesAsync(ct);
    }

    public async Task RunInitialParseAsync(CancellationToken ct)
    {
        var targetProject = Project;
        ImmutableArray<string> librariesDirectories =
            [..Settings.Libraries.Values.OrderBy(l => l.Order).Select(l => l.Path)];
        await targetProject.SourceCodeParser.InitialParseAsync(targetProject.Directory!,
            FrozenDictionary<string, InMemoryFileContent>.Empty, targetProject.Configuration!.SymbolsDefineSet,
            librariesDirectories, ct);
    }

    private CancellationTokenSource? _reparseCts;
    public async Task ReparseChangesAsync(CancellationToken ct)
    {
        if (_reparseCts is not null)
        {
            await _reparseCts.CancelAsync();
            _reparseCts.Dispose();
        }
        _reparseCts = new();
        try
        {
            await Task.Delay(200, ct);
            var targetProject = Project;
            ImmutableArray<string> librariesDirectories =
                [..Settings.Libraries.Values.OrderBy(l => l.Order).Select(l => l.Path)];
            var inMemoryFileContent = _filesViewModel.CollectAllOpenContent();
            await targetProject.SourceCodeParser.ParseAsync(inMemoryFileContent,
                targetProject.Configuration!.SymbolsDefineSet,
                librariesDirectories, ct);
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projectChangedSubscription.Dispose();
        }
        base.Dispose(disposing);
    }
}