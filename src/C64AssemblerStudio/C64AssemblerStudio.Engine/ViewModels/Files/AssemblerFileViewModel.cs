using System.Collections.Frozen;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Antlr4.Runtime;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Common.Compiler;
using C64AssemblerStudio.Core.Extensions;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Models.Parsing;
using Righthand.RetroDbgDataProvider.Models.Program;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using IFileService = C64AssemblerStudio.Core.Services.Abstract.IFileService;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class AssemblerFileViewModel : ProjectFileViewModel
{
    
    private readonly ErrorsOutputViewModel _errorsOutput;
    private readonly IVice _vice;
    private readonly CallStackViewModel _callStack;
    private readonly IParserManager _parserManager;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public BreakpointsViewModel Breakpoints { get; }
    public event EventHandler? BreakpointsChanged;
    public FrozenDictionary<int, SyntaxLine> Lines { get; private set; } = FrozenDictionary<int, SyntaxLine>.Empty;
    public FrozenDictionary<int, ImmutableArray<SingleLineTextRange>> IgnoredContent { get; private set; } =
        FrozenDictionary<int, ImmutableArray<SingleLineTextRange>>.Empty;

    public ImmutableArray<FrozenSet<string>> DefineSymbols { get; private set; } =
        ImmutableArray<FrozenSet<string>>.Empty;

    public FrozenSet<string>? SelectedDefineSymbols { get; set; }
    public FrozenDictionary<int, SyntaxErrorLine> Errors { get; private set; }
    public RelayCommandWithParameterAsync<int> AddOrRemoveBreakpointCommand { get; }
    public bool IsByteDumpVisible { get; set; }
    public bool IsByteDumpToggleVisible => _vice.IsDebugging;
    public bool IsDefineSymbolsVisible => DefineSymbols.Length > 1;
    public bool IsHeaderVisible => IsByteDumpToggleVisible || IsDefineSymbolsVisible;
    public ImmutableArray<ByteDumpLineViewModel> ByteDumpLines { get; private set; }
    /// <summary>
    /// Keeps call stack lines
    /// </summary>
    public ImmutableArray<CallStackViewModel.SourceCallStackItem> CallStackItems { get; private set; }
    /// <summary>
    /// A list of <see cref="ByteDumpLineViewModel"/> for this file.
    /// </summary>
    public ImmutableArray<ByteDumpLineViewModel> MatchingByteDumpLines { get; private set; }
    /// <summary>
    /// Signals to clients that syntax coloring data has been updated.
    /// </summary>
    /// <remarks>
    /// Client could alternatively watch for changes on <see cref="Lines"/> and <see cref="IgnoredContent"/>,
    /// but that could result in duplicate refreshes.
    /// </remarks>
    public event EventHandler? SyntaxColoringUpdated; 
    private ParsedSourceFile? _sourceFile;
    /// <summary>
    /// Parsed Source file with all defined sets.
    /// </summary>
    private IImmutableParsedFileSet<ParsedSourceFile>? _sourceFileWithSets;
    private readonly ISourceCodeParser<ParsedSourceFile> _parser;
    private readonly bool _initialized;
    /// <summary>
    /// Prevent triggering collateral parsing on setting <see cref="SelectedDefineSymbols"/> from code.
    /// Reparsing should be done only on user input.
    /// </summary>
    private bool _settingsSelectedDefineSymbols;
    private void RaiseBreakpointsChanged(EventArgs e) => BreakpointsChanged?.Invoke(this, e);

    /// <summary>
    /// Crateas an <see cref="AssemblerFileViewModel"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileService"></param>
    /// <param name="dispatcher"></param>
    /// <param name="statusInfo"></param>
    /// <param name="breakpoints"></param>
    /// <param name="vice"></param>
    /// <param name="callStack"></param>
    /// <param name="globals"></param>
    /// <param name="errorsOutput"></param>
    /// <param name="parserManager"></param>
    /// <param name="projectExplorer"></param>
    /// <param name="file"></param>
    /// <param name="selectedDefineSymbols">Preselected define symbols</param>
    /// <remarks>
    /// Instance of <see cref="AssemblerFileViewModel"/> are created through
    /// <see cref="Activator.CreateInstance{T}"/> and manually injected arguments can not be
    /// null thus using <see cref="NullableArgument{T}"/> type.
    /// </remarks>
    public AssemblerFileViewModel(ILogger<AssemblerFileViewModel> logger, IFileService fileService,
        IDispatcher dispatcher, StatusInfoViewModel statusInfo, BreakpointsViewModel breakpoints,
        IVice vice, CallStackViewModel callStack,
        Globals globals, ErrorsOutputViewModel errorsOutput,
        IParserManager parserManager, ProjectExplorerViewModel projectExplorer, 
        ProjectFile file,
        NullableArgument<FrozenSet<string>> selectedDefineSymbols) : base(
        logger, fileService, dispatcher, statusInfo, globals, file)
    {
        Breakpoints = breakpoints;
        _errorsOutput = errorsOutput;
        _vice = vice;
        _callStack = callStack;
        _parserManager = parserManager;
        _projectExplorer = projectExplorer;
        ByteDumpLines = ImmutableArray<ByteDumpLineViewModel>.Empty;
        MatchingByteDumpLines = ImmutableArray<ByteDumpLineViewModel>.Empty;
        Errors = FrozenDictionary<int, SyntaxErrorLine>.Empty;
        AddOrRemoveBreakpointCommand = new RelayCommandWithParameterAsync<int>(AddOrRemoveBreakpoint, CanSetOrRemoveBreakpointAtLine);
        Breakpoints.Breakpoints.CollectionChanged += BreakpointsOnCollectionChanged;
        _errorsOutput.PropertyChanged += ErrorsOutputOnPropertyChanged;
        _errorsOutput.CompilerErrorsAdded += ErrorsOutputOnCompilerErrorsAdded;
        _vice.PropertyChanged += ViceOnPropertyChanged;
        UpdateCallStackItems();
        _callStack.PropertyChanged += CallStackOnPropertyChanged;
        var project = (IProjectViewModel<ParsedSourceFile>)globals.Project;
        _parser = project.SourceCodeParser;
        _parser.FilesChanged += ParserOnFilesChanged;
        _sourceFileWithSets = _parser.AllFiles.GetValueOrDefault(File.AbsolutePath);
        if (selectedDefineSymbols.Value is not null)
        {
            SelectedDefineSymbols = selectedDefineSymbols.Value;
        }
        UpdateDefineSymbolsAndSelection();
        UpdateSyntaxInfo();
        _initialized = true;
    }
    private void ErrorsOutputOnCompilerErrorsAdded(object? sender, EventArgs e)
    {
        Errors = FrozenDictionary<int, SyntaxErrorLine>.Empty;
        UpdateCompilerErrors();
    }

    private void ErrorsOutputOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ErrorsOutputViewModel.IsEmpty):
                // when build starts, all errors are removed 
                if (_errorsOutput.IsEmpty)
                {
                    Errors = FrozenDictionary<int, SyntaxErrorLine>.Empty;
                }

                break;
        }
    }
    

    public async Task<(bool ShouldShow, ImmutableArray<EditorCompletionItem> Items)> ShouldShowCompletionAsync(
        TextChangeTrigger trigger, CancellationToken ct = default)
    {
        // when trigger is char typed, wait for parsing to be updated
        // no need when trigger is CompletionRequested since it doesn't cause parsing as no text is changed
        if (trigger == TextChangeTrigger.CharacterTyped)
        {
            _updateSyntaxCompletion = new TaskCompletionSource();
            await _updateSyntaxCompletion.Task;
        }

        ReadOnlySpan<char> lineText = Content.AsSpan().ExtractLine(CaretLine-1);
        var completionOption = _sourceFile?.GetCompletionOption(trigger, CaretLine - 1, CaretColumn-2, lineText);
        if (completionOption is not null)
        {
            var builder = ImmutableArray.CreateBuilder<EditorCompletionItem>();
            switch (completionOption.Value.Type)
            {
                case CompletionOptionType.FileReference:
                {
                    var suggestions = Globals.GetMatchingFiles(completionOption.Value.Root, "asm",
                        excludedFile: File.AbsolutePath);
                    foreach (var s in suggestions)
                    {
                        foreach (var p in s.Value)
                        {
                            builder.Add(new FileReferenceCompletionItem(p, s.Key, completionOption.Value.Root,
                                completionOption.Value.ReplacementLength)
                            {
                                PostfixDoubleQuote = !completionOption.Value.EndsWithDoubleQuote,
                            });
                        }
                    }
                }

                    break;
                case CompletionOptionType.PreprocessorDirective when _sourceFile is not null:
                {
                    var suggestions = _sourceFile.GetPreprocessorDirectiveSuggestions(completionOption.Value.Root);
                    foreach (var s in suggestions)
                    {
                        builder.Add(new StandardCompletionItem(s, "PPD", completionOption.Value.Root,
                            completionOption.Value.ReplacementLength, -1));
                    }
                }
                    break;
                default:
                    throw new IndexOutOfRangeException(nameof(CompletionOption.Type));
            }

            var items = builder.ToImmutable();
            return (items.Length > 0, items);
        }

        return (false, ImmutableArray<EditorCompletionItem>.Empty);
    }

    private void RaiseSyntaxColoringUpdated(EventArgs e) => SyntaxColoringUpdated?.Invoke(this, e);
    private CancellationTokenSource? _syntaxInfoUpdatesCts;

    private void ParserOnFilesChanged(object? sender, FilesChangedEventArgs e)
    {
        if (e.Modified.TryGetValue(File.AbsolutePath, out var parsedSourceFile))
        {
            _sourceFileWithSets = _parser.AllFiles.GetValueOrDefault(File.AbsolutePath);
            UpdateDefineSymbolsAndSelection();
            UpdateSyntaxInfo();
        }
    }

    private void UpdateDefineSymbolsAndSelection()
    {
        if (_sourceFileWithSets is not null)
        {
            // updates defines symbols settings
            var selectedDefineSymbols = SelectedDefineSymbols;
            FrozenSet<string>? newSelectedDefineSymbols = null;
            if (selectedDefineSymbols is not null)
            {
                newSelectedDefineSymbols =
                    _sourceFileWithSets.AllDefineSets.SingleOrDefault(ds => ds.SetEquals(selectedDefineSymbols));
            }

            _settingsSelectedDefineSymbols = true;
            try
            {
                DefineSymbols = _sourceFileWithSets.AllDefineSets;
                SelectedDefineSymbols = newSelectedDefineSymbols ?? DefineSymbols.First();
            }
            finally
            {
                _settingsSelectedDefineSymbols = false;
            }
        }
    }

    private TaskCompletionSource? _updateSyntaxCompletion;
    private void UpdateSyntaxInfo()
    {
        _ = UpdateSyntaxInfoAsync();
    }
    private async Task UpdateSyntaxInfoAsync()
    {
        if (_syntaxInfoUpdatesCts is not null)
        {
            await _syntaxInfoUpdatesCts.CancelAsync();
            _syntaxInfoUpdatesCts.Dispose();
            _syntaxInfoUpdatesCts = null;
        }

        _syntaxInfoUpdatesCts = new();
        var ct = _syntaxInfoUpdatesCts.Token;
        if (_sourceFileWithSets is not null && SelectedDefineSymbols is not null)
        {
            var singleFileSet = SelectedDefineSymbols;
            _sourceFile = _sourceFileWithSets.GetFile(singleFileSet);
            if (_sourceFile is not null)
            {
                try
                {
                    Debug.WriteLine("Refreshing syntax info");
                    (Lines, var ignoredContent, Errors, _, _) = await _sourceFile.GetSyntaxInfoAsync(ct);
                    var allErrors = Errors
                        .SelectMany(e => e.Value.Items)
                        .Select(se => new FileCompilerError(File, se));
                    _errorsOutput.AddParserErrorsForFile(File.AbsolutePath, allErrors);
                    ct.ThrowIfCancellationRequested();
                    IgnoredContent = ConvertIgnoredMultiLineStructureToSingleLine(ignoredContent);
                    RaiseSyntaxColoringUpdated(EventArgs.Empty);
                }
                catch (OperationCanceledException)
                {
                    _updateSyntaxCompletion?.SetCanceled(ct);
                }
                catch (Exception ex)
                {
                    _updateSyntaxCompletion?.SetException(ex);
                    Logger.LogError(ex, "Failed updating syntax info");
                }
            }
            else
            {
                Logger.LogWarning("No parsed file {File} for given definition symbols {Symbols}", File.Name, "?");
            }
        }
        else
        {
            SelectedDefineSymbols = null;
            DefineSymbols = ImmutableArray<FrozenSet<string>>.Empty;
            Logger.LogWarning("Opening {File} without parsed info", File.Name);
        }

        _updateSyntaxCompletion?.TrySetResult();
    }

    /// <summary>
    /// Converts multiline ranges to an array of grouped single line ranges per line.
    /// </summary>
    /// <param name="ranges"></param>
    /// <returns></returns>
    internal static FrozenDictionary<int, ImmutableArray<SingleLineTextRange>>
        ConvertIgnoredMultiLineStructureToSingleLine(ImmutableArray<MultiLineTextRange> ranges)
    {
        var builder = new Dictionary<int, ImmutableArray<SingleLineTextRange>>();
        foreach (var r in ranges)
        {
            for (int line = r.Start.Row; line <= r.End.Row; line++)
            {
                int? start = line == r.Start.Row ? r.Start.Column : null;
                int? end = line == r.End.Row ? r.End.Column : null;
                var lineRange = builder.TryGetValue(line, out var range) ? range : ImmutableArray<SingleLineTextRange>.Empty;
                lineRange = lineRange.AddRange(new SingleLineTextRange(start, end));
                builder[line] = lineRange;
            }
        }

        return builder.ToFrozenDictionary();
    }

    public bool CanSetOrRemoveBreakpointAtLine(int lineNumber)
    {
        var breakpoint = GetBreakPointAtLine(lineNumber);
        // addition is possible only on non-empty lines
        if (breakpoint is null)
        {
            return Lines.TryGetValue(lineNumber, out var line) 
                   && !line.Items.IsEmpty
                   && IsLineAvailableForBreakpoint(line.Items);
        }
        // always can remove
        return true;
    }

    internal static bool IsLineAvailableForBreakpoint(ImmutableArray<SyntaxItem> items)
    {
        foreach (var i in items)
        {
            if (i.TokenType == TokenType.PreprocessorDirective)
            {
                return false;
            }
        }

        return true;
    }

    private void CallStackOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof (CallStackViewModel.CallStack):
                UpdateCallStackItems();
                break;
        }
    }

    private void ViceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IVice.IsDebugging):
                OnPropertyChanged(nameof(IsByteDumpToggleVisible));
                break;
            case nameof(IVice.IsPaused):
                UpdateByteDumpExecutionLine();
                break;
        }
    }

    private void UpdateCallStackItems()
    {
        CallStackItems = [.._callStack.CallStack.OfType<CallStackViewModel.SourceCallStackItem>().Where(i => i.File == File)];
    }
    private void BreakpointsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        string filePath = File.GetRelativeFilePath();
        bool IsMatch(BreakpointViewModel b)
        {
            return b.Bind is BreakpointLineBind lineBind &&
                   filePath.Equals(lineBind.FilePath, OsDependent.FileStringComparison);
        }

        bool hasChanges =
            (e.NewItems?.OfType<BreakpointViewModel>().Any(IsMatch) ?? false)
            || (e.OldItems?.OfType<BreakpointViewModel>().Any(IsMatch) ?? false);
        if (hasChanges)
        {
            RaiseBreakpointsChanged(EventArgs.Empty);
        }
    }

    public async Task AddOrRemoveBreakpoint(int lineNumber)
    {
        string filePath = File.GetRelativeFilePath();
        var breakpoint = Breakpoints.GetLineBreakpointForLine(filePath, lineNumber);
        if (breakpoint is not null)
        {
            Logger.LogDebug("Removing breakpoint in {File} at {Line}", filePath, lineNumber);
            await Breakpoints.RemoveBreakpointAsync(breakpoint);
        }
        else
        {
            Logger.LogDebug("Adding breakpoint in {File} at {Line}", filePath, lineNumber);
            await Breakpoints.AddLineBreakpointAsync(File.GetRelativeFilePath(), lineNumber, null);
        }
    }

    public BreakpointViewModel? GetBreakPointAtLine(int lineNumber)
    {
        return Breakpoints.GetLineBreakpointForLine(File.GetRelativeFilePath(), lineNumber);
    }

    private void UpdateCompilerErrors()
    {
        var errors = _errorsOutput.Lines
            .Where(f => f.File?.IsSame(File) == true)
            .Select(e => e.Error);
        Errors = errors
            .GroupBy(e => e.Line)
            .ToFrozenDictionary(g => g.Key, g => new SyntaxErrorLine([..g]));
        RaiseSyntaxColoringUpdated(EventArgs.Empty);
    }

    private SingleLineTextRange? FindTokenAtLocation(int line, int column)
    {
        return _sourceFile?.GetTokenRangeAt(line, column);
    }

    public SyntaxError? GetSyntaxErrorAt(int line, int column)
    {
        if (Errors.TryGetValue(line, out var errors))
        {
            foreach (var e in errors.Items)
            {
                if (e.Range.Start <= column && e.Range.End > column)
                {
                    return e;
                }
            }
        }

        return null;
    }
    protected override void OnPropertyChanged(string name = default!)
    {
        // OnPropertyChanged has to happen before _parserManager is invoked
        // because it updates LastChangedTime has HasChanges
        base.OnPropertyChanged(name);
        if (!_initialized)
        {
            return;
        }
        switch (name)
        {
            case nameof(Content):
                if (IsContentLoaded)
                {
                    _ = _parserManager.ReparseChangesAsync(CancellationToken.None);
                }

                break;
            case nameof(IsByteDumpVisible):
                if (IsByteDumpVisible)
                {
                    LoadByteDump();
                }
                break;
            case nameof(ExecutionAddress):
                UpdateByteDumpExecutionLine();
                break;
            case nameof(CaretLine):
                UpdateByteDumpHighlight();
                break;
            case nameof(IsByteDumpToggleVisible):
                if (!IsByteDumpToggleVisible)
                {
                    IsByteDumpVisible = false;
                }
                break;
            case nameof(SelectedDefineSymbols):
                if (!_settingsSelectedDefineSymbols)
                {
                    // trigger reparsing only on user input
                    UpdateSyntaxInfo();
                }
                break;
        }

    }

    internal void UpdateByteDumpExecutionLine()
    {
        if (_vice.IsPaused)
        {
            foreach (var l in MatchingByteDumpLines)
            {
                l.IsExecutive = l.Address <= ExecutionAddress && l.Address + l.Bytes.Length > ExecutionAddress;
            }
        }
        else
        {
            foreach (var bl in MatchingByteDumpLines)
            {
                bl.IsExecutive = false;
            }
        }
    }

    internal void UpdateByteDumpHighlight()
    {
        foreach (var l in MatchingByteDumpLines)
        {
            l.IsHighlighted = l.FileLocation.Start.Row <= CaretLine && l.FileLocation.End.Row >= CaretLine;
        }
    }

    internal void LoadByteDump()
    {
        if (ByteDumpLines.IsEmpty)
        {
            var project = (KickAssProjectViewModel)Globals.Project;
            Guard.IsNotNull(project.ByteDumpLines);
            Guard.IsNotNull(project.AppInfo);
            if (project.AppInfo.SourceFiles.TryGetValue(
                    new SourceFilePath(File.GetRelativeFilePath(), IsRelative: true), out var sourceFile))
            {
                ByteDumpLines =
                [
                    ..project.ByteDumpLines.ValueOrThrow()
                        .Select(l => new ByteDumpLineViewModel(l, l.SourceFile == sourceFile))
                ];
            }

            MatchingByteDumpLines = [..ByteDumpLines.Where(l => l.BelongsToFile)];
        }

        UpdateByteDumpExecutionLine();
        UpdateByteDumpHighlight();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Breakpoints.Breakpoints.CollectionChanged -= BreakpointsOnCollectionChanged;
            _errorsOutput.PropertyChanged -= ErrorsOutputOnPropertyChanged;
            _errorsOutput.CompilerErrorsAdded -= ErrorsOutputOnCompilerErrorsAdded;
            _vice.PropertyChanged -= ViceOnPropertyChanged;
            _callStack.PropertyChanged -= CallStackOnPropertyChanged;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Opens referenced file clicked on by user.
    /// </summary>
    /// <param name="item"></param>
    public void ReferencedFileClicked(FileReferenceSyntaxItem item)
    {
        if (item.ReferencedFile.FullFilePath is null)
        {
            Logger.LogError("Clicked file {RelativePath} is missing FullFilePath", item.ReferencedFile.RelativeFilePath);
            return;
        }
        var file = _projectExplorer.GetProjectFileFromFullPath(item.ReferencedFile.FullFilePath!);
        if (file is null)
        {
            Logger.LogError("Failed to find file {RelativePath} among project files",
                item.ReferencedFile.RelativeFilePath);
            return;
        }

        var message = new OpenFileMessage(file, DefineSymbols: item.ReferencedFile.InDefines);
        Dispatcher.Dispatch(message);
    }
}