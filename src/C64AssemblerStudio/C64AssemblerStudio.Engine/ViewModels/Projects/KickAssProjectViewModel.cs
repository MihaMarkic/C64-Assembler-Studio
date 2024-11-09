using System.Collections.Frozen;
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

namespace C64AssemblerStudio.Engine.ViewModels.Projects;

public class KickAssProjectViewModel : ProjectViewModel<KickAssProject, KickAssemblerParsedSourceFile>
{
    public IKickAssemblerCompiler Compiler { get; }
    public IKickAssemblerDbgParser DbgParser { get; }
    public IKickAssemblerProgramInfoBuilder ProgramInfoBuilder { get; }
    public IKickAssemblerByteDumpParser ByteDumpParser { get; }
    public DbgData? DbgData { get; private set; }
    public FrozenDictionary<string, AssemblySegment>? ByteDump { get; private set; }
    public ImmutableArray<ByteDumpLine>? ByteDumpLines { get; private set; }
    public FrozenDictionary<string, Label>? Labels { get; private set; }

    public KickAssProjectViewModel(ILogger<ProjectViewModel<KickAssProject, KickAssemblerParsedSourceFile>> logger, ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher, IKickAssemblerCompiler compiler, IKickAssemblerDbgParser dbgParser,
        IKickAssemblerProgramInfoBuilder programInfoBuilder,
        IKickAssemblerByteDumpParser byteDumpParser,
        IKickAssemblerSourceCodeParser sourceCodeParser)
        : base(logger, settingsManager, systemDialogs, dispatcher, sourceCodeParser)
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