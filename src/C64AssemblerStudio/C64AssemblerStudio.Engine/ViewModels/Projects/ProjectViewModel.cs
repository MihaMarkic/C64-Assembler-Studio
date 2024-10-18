using System.Text.RegularExpressions;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Program;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels.Projects;

public abstract class ProjectViewModel<TConfiguration, TParsedFileType> : OverlayContentViewModel,
    IProjectViewModel<TParsedFileType>
    where TConfiguration : Project
    where TParsedFileType : ParsedSourceFile
{
    protected readonly ILogger<ProjectViewModel<TConfiguration, TParsedFileType>> Logger;
    private readonly ISettingsManager _settingsManager;
    protected readonly ISystemDialogs SystemDialogs;
    public event EventHandler? ProjectChanged;
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public AssemblerAppInfo? AppInfo { get; protected set; }
    public ISourceCodeParser<TParsedFileType> SourceCodeParser { get; }
    public string? Path { get; set; }
    public ImmutableArray<string> Libraries => Configuration?.LibDirArray ?? ImmutableArray<string>.Empty;
    public string? Caption { get; set; }
    public string? LibDirs { get; set; }
    public string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;
    public string? FullPrgPath => Directory is not null ? System.IO.Path.Combine(Directory, "build", "main.prg") : null;
    public string? BreakpointsSettingsPath => Directory is not null ? System.IO.Path.Combine(Directory, "breakpoints.json") : null;

    protected ProjectViewModel(ILogger<ProjectViewModel<TConfiguration, TParsedFileType>> logger, ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher,
        ISourceCodeParser<TParsedFileType> sourceCodeParser) : base(dispatcher)
    {
        _settingsManager = settingsManager;
        Logger = logger;
        SystemDialogs = systemDialogs;
        SourceCodeParser = sourceCodeParser;
    }
    public void Init(TConfiguration configuration, string? path)
    {
        Configuration = configuration;
        Path = path;
        LibDirs = configuration.LibDirs;
        Caption = configuration.Caption;
    }
    private void OnProjectChanged(EventArgs e) => ProjectChanged?.Invoke(this, e);
    public abstract Task LoadDebugDataAsync(CancellationToken ct = default);

    protected override async Task ClosingAsync(CancellationToken ct = default)
    {
        bool hasChanges = false;
        if (!string.Equals(Configuration!.LibDirs, LibDirs, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(LibDirs))
            {
                // when empty string, store rather null to clear save file of that property
                Configuration!.LibDirs = string.Empty;
            }
            else
            {
                Configuration.LibDirs = LibDirs;
            }

            hasChanges = true;
        }

        if (!string.Equals(Configuration.Caption, Caption, StringComparison.Ordinal))
        {
            Configuration.Caption = Caption;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _settingsManager.SaveAsync<Project>(Configuration.ValueOrThrow(), Path.ValueOrThrow(), false, ct);
            OnProjectChanged(EventArgs.Empty);
        }

        await base.ClosingAsync(ct).ConfigureAwait(false);
    }

    protected override async Task DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            await SourceCodeParser.DisposeAsync();
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}