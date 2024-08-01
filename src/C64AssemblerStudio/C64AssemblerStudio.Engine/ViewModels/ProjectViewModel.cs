using System.Collections.Frozen;
using System.ComponentModel;
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
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public AssemblerAppInfo? AppInfo { get; protected set; }
    public string? Path { get; set; }
    public string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;
    public string? FullPrgPath => Directory is not null ? System.IO.Path.Combine(Directory, "build", "main.prg"): null;
    public string? BreakpointsSettingsPath => Directory is not null ? System.IO.Path.Combine(Directory, "breakpoints.json") : null;

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
    public ImmutableArray<ByteDumpLine>? ByteDumpLines { get; private set; }

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
                
        var dbgDataTask = DbgParser.LoadFileAsync(dbgFile, ct);
        ByteDump = await ByteDumpParser.LoadFileAsync(byteDumpFile, ct);
        DbgData = await dbgDataTask;
        AppInfo = await ProgramInfoBuilder.BuildAppInfoAsync(Directory, DbgData, ct);
        ByteDumpLines = CreateByteDumpLines(ByteDump, AppInfo);
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