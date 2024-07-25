using System.Collections.Frozen;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface IProjectViewModel: IDisposable
{
    RelayCommand CloseCommand { get; }
    string? Path { get; set; }
    string? Directory { get; }
    Project? Configuration { get; }
    event PropertyChangedEventHandler? PropertyChanged;
    Task LoadDebugDataAsync(CancellationToken ct = default);
}
public abstract class ProjectViewModel<TConfiguration>: OverlayContentViewModel, IProjectViewModel
    where TConfiguration : Project
{
    protected readonly ILogger<ProjectViewModel<TConfiguration>> Logger;
    private readonly ISettingsManager _settingsManager;
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public AssemblerAppInfo? AppInfo { get; protected set; }
    public string? Path { get; set; }
    public string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;

    protected ProjectViewModel(ILogger<ProjectViewModel<TConfiguration>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher) : base(dispatcher)
    {
        _settingsManager = settingsManager;
        Logger = logger;
    }
    public void Init(TConfiguration configuration, string? path)
    {
        Configuration = configuration;
        Path = path;
    }

    public abstract Task LoadDebugDataAsync(CancellationToken ct = default);
    protected override void Closing()
    {
        _settingsManager.Save<Project>(Configuration.ValueOrThrow(), Path.ValueOrThrow(), false);
        base.Closing();
    }
}

public class EmptyProjectViewModel : ProjectViewModel<EmptyProject>
{
    public EmptyProjectViewModel(ILogger<ProjectViewModel<EmptyProject>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher)
        : base(logger, settingsManager, dispatcher)
    {
    }

    public override Task LoadDebugDataAsync(CancellationToken ct = default) => throw new NotImplementedException();
}

public class KickAssProjectViewModel : ProjectViewModel<KickAssProject>
{
    public IKickAssemblerCompiler Compiler { get; }
    public IKickAssemblerDbgParser DbgParser { get; }
    public IKickAssemblerProgramInfoBuilder ProgramInfoBuilder { get; }
    public IKickAssemblerByteDumpParser ByteDumpParser { get; }
    public DbgData? DbgData { get; private set; }
    public FrozenDictionary<string, AssemblySegment>? ByteDump { get; private set; }

    public KickAssProjectViewModel(ILogger<ProjectViewModel<KickAssProject>> logger, ISettingsManager settingsManager,
        IDispatcher dispatcher, IKickAssemblerCompiler compiler, IKickAssemblerDbgParser dbgParser,
        IKickAssemblerProgramInfoBuilder programInfoBuilder,
        IKickAssemblerByteDumpParser byteDumpParser)
        : base(logger, settingsManager, dispatcher)
    {
        Compiler = compiler;
        DbgParser = dbgParser;
        ProgramInfoBuilder = programInfoBuilder;
        ByteDumpParser = byteDumpParser;
    }

    public override async Task LoadDebugDataAsync(CancellationToken ct = default)
    {
        if (Directory is null)
        {
            Logger.LogError("Project directory is null");
            return;
        }
        string outDirectory = System.IO.Path.Combine(Directory, "build");
        string dbgFile = System.IO.Path.Combine(outDirectory, "main.dbg");
        string byteDumpFile = System.IO.Path.Combine(outDirectory, "bytedump.dmp");
                
        var byteDumpTask = ByteDumpParser.LoadFileAsync(byteDumpFile, ct);
        DbgData = await DbgParser.LoadFileAsync(dbgFile, ct);
        AppInfo = await ProgramInfoBuilder.BuildAppInfoAsync(DbgData, ct);
        ByteDump = await byteDumpTask;
    }
}