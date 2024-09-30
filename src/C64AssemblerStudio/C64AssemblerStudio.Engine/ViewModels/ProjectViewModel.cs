using System.Collections.Frozen;
using System.ComponentModel;
using System.Windows.Input;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Models.SystemDialogs;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models.Program;
using Label = Righthand.RetroDbgDataProvider.Models.Program.Label;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface IProjectViewModel: IDisposable
{
    RelayCommand CloseCommand { get; }
    string? Path { get; set; }
    string? Directory { get; }
    Project? Configuration { get; }
    string? FullPrgPath { get; }
    public string? BreakpointsSettingsPath { get; }
    event PropertyChangedEventHandler? PropertyChanged;
    Task LoadDebugDataAsync(CancellationToken ct = default);
}
public abstract class ProjectViewModel<TConfiguration>: OverlayContentViewModel, IProjectViewModel
    where TConfiguration : Project
{
    protected readonly ILogger<ProjectViewModel<TConfiguration>> Logger;
    private readonly ISettingsManager _settingsManager;
    protected readonly ISystemDialogs SystemDialogs;
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public AssemblerAppInfo? AppInfo { get; protected set; }
    public string? Path { get; set; }
    public string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;
    public string? FullPrgPath => Directory is not null ? System.IO.Path.Combine(Directory, "build", "main.prg"): null;
    public string? BreakpointsSettingsPath => Directory is not null ? System.IO.Path.Combine(Directory, "breakpoints.json") : null;

    protected ProjectViewModel(ILogger<ProjectViewModel<TConfiguration>> logger, ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher) : base(dispatcher)
    {
        _settingsManager = settingsManager;
        Logger = logger;
        SystemDialogs = systemDialogs;
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
        ISystemDialogs systemDialogs, IDispatcher dispatcher)
        : base(logger, settingsManager, systemDialogs, dispatcher)
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
    public ImmutableArray<ByteDumpLine>? ByteDumpLines { get; private set; }
    public FrozenDictionary<string, Label>? Labels { get; private set; }
    public RelayCommandAsync OpenLibDirCommand { get; }

    public KickAssProjectViewModel(ILogger<ProjectViewModel<KickAssProject>> logger, ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher, IKickAssemblerCompiler compiler, IKickAssemblerDbgParser dbgParser,
        IKickAssemblerProgramInfoBuilder programInfoBuilder,
        IKickAssemblerByteDumpParser byteDumpParser)
        : base(logger, settingsManager, systemDialogs, dispatcher)
    {
        Compiler = compiler;
        DbgParser = dbgParser;
        ProgramInfoBuilder = programInfoBuilder;
        ByteDumpParser = byteDumpParser;
        OpenLibDirCommand = new RelayCommandAsync(OpenLibDirAsync);
    }

    private async Task OpenLibDirAsync()
    {
        if (Configuration is null)
        {
            throw new Exception("Configuration should be loaded at this point");
        }
        var newDirectory =
            await SystemDialogs.OpenDirectoryAsync(new OpenDirectory(Configuration.LibDir, "Select LibDir"));
        var path = newDirectory.SingleOrDefault();
        if (path is not null)
        {
            if (!string.IsNullOrWhiteSpace(Configuration.LibDir))
            {
                Configuration.LibDir += $";{newDirectory.SingleOrDefault()}";
            }
            else
            {
                Configuration.LibDir = newDirectory.SingleOrDefault();
            }
        }
    }

    protected override void Closing()
    {
        if (string.IsNullOrWhiteSpace(Configuration.ValueOrThrow().LibDir))
        {
            // when empty string, store rather null to clear save file of that property
            Configuration!.LibDir = null;
        }
        base.Closing();
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
                
        var dbgDataTask = DbgParser.LoadFileAsync(dbgFile, ct);
        ByteDump = await ByteDumpParser.LoadFileAsync(byteDumpFile, ct);
        DbgData = await dbgDataTask;
        AppInfo = await ProgramInfoBuilder.BuildAppInfoAsync(Directory, DbgData, ct);
        ByteDumpLines = CreateByteDumpLines(ByteDump, AppInfo);
        // merges labels from all files into single dictionary
        var allLabels = AppInfo.SourceFiles.Values.SelectMany(f => f.Labels);
        var builder = new Dictionary<string, Label>();
        foreach (var l in allLabels)
        {
            builder.Add(l.Key, l.Value);
        }
        Labels = builder.ToFrozenDictionary();
    }

    /// <summary>
    /// Maps ByteDump lines to array of SourceFile, location within text and ByteDump line
    /// </summary>
    /// <param name="byteDump"></param>
    /// <param name="appInfo"></param>
    /// <returns></returns>
    private ImmutableArray<ByteDumpLine>? CreateByteDumpLines(
        FrozenDictionary<string, AssemblySegment> byteDump,
        AssemblerAppInfo appInfo)
    {
        var appInfoBlockItems = from sf in appInfo.SourceFiles.Values
            from bi in sf.BlockItems
            select new { SourceFile = sf, BlockItem = bi };
        var appInfoBlockItemsPerAddressMap = appInfoBlockItems
            .ToFrozenDictionary(i => i.BlockItem.Start);
        
        var allByteDumpLines = byteDump.Values.SelectMany(s => s.Blocks).SelectMany(b => b.Lines)
            .ToImmutableArray();
        
        var builder = ImmutableArray.CreateBuilder<ByteDumpLine>(allByteDumpLines.Length);

        foreach (var line in allByteDumpLines)
        {
            if (appInfoBlockItemsPerAddressMap.TryGetValue(line.Address, out var mapItem))
            {
                builder.Add(new ByteDumpLine(line, mapItem.SourceFile, mapItem.BlockItem.FileLocation));
            }
            else
            {
                Logger.LogWarning("Failed matching bytedump at {Address} with {Description}", line.Address, line.Description);
            }
        }
        return builder.ToImmutableArray();
    }
}