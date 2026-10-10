using System.Collections.Frozen;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using CommunityToolkit.Diagnostics;
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
    protected readonly IFileService FileService;
    public TConfiguration? Configuration { get; private set; }
    Project? IProjectViewModel.Configuration => Configuration;
    public AssemblerAppInfo? AppInfo { get; protected set; }
    public ISourceCodeParser<TParsedFileType> SourceCodeParser { get; }
    public string? Path { get; set; }
    public string? Caption { get; set; }
    public string SymbolsDefine { get; set; } = string.Empty;
    public string? Directory => Path is not null ? System.IO.Path.GetDirectoryName(Path) : null;
    public string? FullPrgPath => Directory is not null ? System.IO.Path.Combine(Directory, "build", "main.prg") : null;
    /// <inheritdoc/>
    public UInt16? ProgramEntryAddress { get; protected set; }
    public string? BreakpointsSettingsPath =>
        Directory is not null ? System.IO.Path.Combine(Directory, "breakpoints.json") : null;

    protected ProjectViewModel(ILogger<ProjectViewModel<TConfiguration, TParsedFileType>> logger,
        ISettingsManager settingsManager,
        ISystemDialogs systemDialogs, IDispatcher dispatcher,
        IFileService fileService,
        ISourceCodeParser<TParsedFileType> sourceCodeParser) : base(dispatcher)
    {
        _settingsManager = settingsManager;
        Logger = logger;
        SystemDialogs = systemDialogs;
        SourceCodeParser = sourceCodeParser;
        FileService = fileService;
    }

    /// <summary>
    /// <see cref="SymbolsDefine"/> presented as set.
    /// </summary>
    public FrozenSet<string> SymbolsDefineSet
    {
        get
        {
            return SymbolsDefine.Split(';').Select(d => d.Trim()).ToFrozenSet();
        }
    }

    public void Init(TConfiguration configuration, string? path)
    {
        Configuration = configuration;
        Path = path;
        Caption = configuration.Caption;
        SymbolsDefine = configuration.SymbolsDefine;
    }
    
    /// <summary>
    /// Retrieves starting address from assembled prg file. 
    /// </summary>
    /// <param name="prgFile"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <remarks>
    /// It expects standard <c>N SYS ADDRESS</c> call at <c>0x0801</c> address.
    /// </remarks>
    internal async ValueTask<UInt16?> GetStartingAddressAsync(string prgFile, CancellationToken ct)
    {
	    if (!FileService.FileExists(prgFile))
	    {
		    Logger.LogError("Can't find prg file {PrgFile} to extract starting address from", prgFile);
	    }

	    try
	    {
		    await using var stream = FileService.OpenRead(prgFile);
		    var segment = stream.ReadUShort();
		    if (segment == 0x0801)
		    {
			    // basic bootstrap
			    var basicStart = stream.ReadUShort();
			    if (basicStart != 0x080c)
			    {
				    throw new Exception("Basic address isn't 0x080c");
			    }

			    // line number is not really important
			    var lineNumber = stream.ReadUShort();
			    var basicCommand = stream.ReadByte();
			    // 0x9e is SYS command
			    if (basicCommand == 0x9e)
			    {
				    // skip spaces
				    int first;
				    while ((first = stream.ReadByte()) == 0x20);
				    Span<byte> textAddressData = stackalloc byte[6]; // 5 is max address possible
				    textAddressData[0] = (byte)first;
				    stream.ReadExactly(textAddressData[1..]);
				    return GetAddressFromSysCommand(textAddressData);
			    }
		    }
	    }
	    catch (Exception ex)
	    {
		    Logger.LogError("Failed to read starting address from prg file {PrgFile}", prgFile);
	    }

	    return null;
    }

    /// <summary>
    /// Expect SYS start address as text.
    /// </summary>
    /// <param name="textBuffer"></param>
    /// <returns></returns>
    /// <example>
    /// $32,$30,$36,$32,$00 equals to 2026 aka 0x080e
    /// </example>
    internal static ushort GetAddressFromSysCommand(ReadOnlySpan<byte> textBuffer)
    {
	    ushort address = 0;
	    int i;
	    for (i = 0; i < 5 && textBuffer[i] != 0x00; i++)
	    {
		    address = (ushort)(address * 10 + (textBuffer[i] - 0x30));
	    }

	    // address string has to be null terminated
	    if (textBuffer[i] != 0x00)
	    {
		    throw new Exception("SYS address was not null terminated");
	    }
	    
	    return address;
    }
    
      


    private void RaiseSettingsProjectChanged() => Dispatcher.Dispatch(new ProjectSettingsChangedMessage());
    public abstract Task LoadDebugDataAsync(CancellationToken ct = default);

    protected override async Task ClosingAsync(CancellationToken ct = default)
    {
        Guard.IsNotNull(Configuration);
        bool hasChanges = false;
        if (!string.Equals(Configuration.Caption, Caption, StringComparison.Ordinal))
        {
            Configuration.Caption = Caption;
            hasChanges = true;
        }

        if (!string.Equals(Configuration.SymbolsDefine, SymbolsDefine, StringComparison.Ordinal))
        {
            Configuration.SymbolsDefine = SymbolsDefine;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _settingsManager.SaveAsync<Project>(Configuration.ValueOrThrow(), Path.ValueOrThrow(), false, ct);
            RaiseSettingsProjectChanged();
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