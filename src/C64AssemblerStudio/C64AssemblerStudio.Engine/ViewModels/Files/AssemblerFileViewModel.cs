using System.Collections.Frozen;
using System.Collections.Specialized;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public record SyntaxError(string Text, int Line, int Start, int End);
public class AssemblerFileViewModel : ProjectFileViewModel
{
    public enum TokenType
    {
        String,
        Instruction,
        Operator,
        Number,
        Unknown,
        Comment,
        Directive,
        Color,
        InstructionExtension,
        Bracket,
        Separator
    }
    public record LineItem(int Start, int End, TokenType TokenType);
    public record Line(ImmutableArray<LineItem> Items);
    private static readonly FrozenDictionary<int, TokenType> Map;
    private readonly CompilerErrorsOutputViewModel _compilerErrors;
    public event EventHandler? BreakpointsChanged;
    public BreakpointsViewModel Breakpoints { get; }
    private ImmutableArray<ImmutableArray<IToken>> _tokens = ImmutableArray<ImmutableArray<IToken>>.Empty;
    public ImmutableArray<Line?> Lines { get; private set; } = ImmutableArray<Line?>.Empty;
    public FrozenDictionary<int, ImmutableArray<SyntaxError>> Errors { get; private set; }
    public RelayCommandWithParameterAsync<int> AddOrRemoveBreakpointCommand { get; }

    static AssemblerFileViewModel()
    {
        Map = BuildMap();
    }

    private void OnBreakpointsChanged(EventArgs e) => BreakpointsChanged?.Invoke(this, e);
    public AssemblerFileViewModel(ILogger<AssemblerFileViewModel> logger, IFileService fileService,
        IDispatcher dispatcher, StatusInfoViewModel statusInfo, BreakpointsViewModel breakpoints,
        Globals globals, CompilerErrorsOutputViewModel compilerErrors, ProjectFile file) : base(
        logger, fileService, dispatcher, statusInfo, globals, file)
    {
        Breakpoints = breakpoints;
        _compilerErrors = compilerErrors;
        Errors = FrozenDictionary<int, ImmutableArray<SyntaxError>>.Empty;
        AddOrRemoveBreakpointCommand = new RelayCommandWithParameterAsync<int>(AddOrRemoveBreakpoint);
        Breakpoints.Breakpoints.CollectionChanged += BreakpointsOnCollectionChanged;
        UpdateErrors();
        _compilerErrors.Lines.CollectionChanged += CompilerErrors_LinesOnCollectionChanged;
    }

    private void BreakpointsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        string filePath = File.GetRelativeFilePath();
        bool IsMatch(BreakpointViewModel b)
        {
            return b.Bind is BreakpointLineBind lineBind &&
                   filePath.Equals(lineBind.FilePath, OSDependent.FileStringComparison);
        }

        bool hasChanges = e.NewItems is IList<BreakpointViewModel> newItems && newItems.Any(IsMatch)
                || e.OldItems is IList<BreakpointViewModel> oldItems && oldItems.Any(IsMatch);
        if (hasChanges)
        {
            OnBreakpointsChanged(EventArgs.Empty);
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

    private void CompilerErrors_LinesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateErrors();
    }

    public bool HasBreakPointAtLine(int lineNumber)
    {
        return Breakpoints.GetLineBreakpointForLine(File.GetRelativeFilePath(), lineNumber) is not null;
    }

    void UpdateErrors()
    {
        var errors = _compilerErrors.Lines
            .Where(f => f.File?.IsSame(File) == true)
            .Select(e =>
            {
                var token = FindTokenAtLocation(e.Error.Line, e.Error.Column);
                int start;
                int end;
                if (token is not null)
                {
                    start = token.Column;
                    end = token.Column + token.Text.Length;
                }
                else
                {
                    start = e.Error.Column;
                    end = e.Error.Column + 1;
                }
                return new SyntaxError(e.Error.Text ?? "?", e.Error.Line, start, end);
            });
        Errors = errors
            .GroupBy(e => e.Line)
            .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray());
    }

    IToken? FindTokenAtLocation(int line, int column)
    {
        if (_tokens.Length >= line)
        {
            var tokens = _tokens[line - 1];
            foreach (var t in tokens)
            {
                if (t.Column <= column && t.Column + t.Text.Length >= column)
                {
                    return t;
                }
            }
        }

        return null;
    }

    public SyntaxError? GetSyntaxErrorAt(int line, int column)
    {
        if (Errors.TryGetValue(line, out var errors))
        {
            foreach (var e in errors)
            {
                if (e.Start <= column && e.End > column)
                {
                    return e;
                }
            }
        }

        return null;
    }
    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(Content):
                _ = ParseTextAsync();
                break;
        }

        base.OnPropertyChanged(name);
    }

    private CancellationTokenSource? _ctsParser;

    internal async Task ParseTextAsync(CancellationToken ct = default)
    {
        await _ctsParser.CancelNullableAsync(ct: ct);
        _ctsParser = new();
        try
        {
            (Lines, var tokens) = await Task.Run(() => ParseText(Logger, Content, _ctsParser.Token), _ctsParser.Token);
            var tokensBuilder = new List<IToken>?[Lines.Length];
            foreach (var t in tokens)
            {
                tokensBuilder[t.Line-1] ??= new();
                tokensBuilder[t.Line-1]!.Add(t);
            }
            _tokens = [..tokensBuilder.Select(a => a?.ToImmutableArray() ?? ImmutableArray<IToken>.Empty)];
            UpdateErrors();
        }
        catch (OperationCanceledException)
        {
        }
    }

    internal static (ImmutableArray<Line?> Lines, ImmutableArray<IToken> Tokens) ParseText(ILogger logger, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (ImmutableArray<Line?>.Empty, ImmutableArray<IToken>.Empty);
        }

        var input = new AntlrInputStream(content);
        var lexer = new KickAssemblerLexer(input);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KickAssemblerParser(tokenStream)
        {
            BuildParseTree = true
        };
        var tree = parser.program();
        var listener = new KickAssemblerParserBaseListener();
        ParseTreeWalker.Default.Walk(listener, tree);

        var tokens = tokenStream.GetTokens();
        int linesCount = content.Count(c => c == '\n') + 1;
        var builder = IterateTokens(linesCount, tokens, ct);

        var lines = builder.Select(l => l is not null ? new Line([..l]) : null);
        return ([..lines], [..tokens]);
    }

    internal static List<LineItem>?[] IterateTokens(int linesCount, IList<IToken> tokens, CancellationToken ct)
    {
        List<LineItem>?[] builder = new List<LineItem>?[linesCount];

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            ct.ThrowIfCancellationRequested();
            bool ignore = false;
            if (Map.TryGetValue(token.Type, out var tokenType))
            {
                var line = builder[token.Line - 1];
                if (line is null)
                {
                    line = new();
                    builder[token.Line - 1] = line;
                }

                int startIndex = token.StartIndex;
                switch (tokenType)
                {
                    case TokenType.Directive:
                        if (i > 0)
                        {
                            var previous = tokens[i - 1];
                            if (previous.Type == KickAssemblerLexer.DOT && previous.Line == token.Line)
                            {
                                startIndex = token.StartIndex - 1;
                            }
                        }
                        break;
                    case TokenType.InstructionExtension:
                        if (i > 1 && tokens[i-1].Type == KickAssemblerLexer.DOT 
                            && Map.TryGetValue(tokens[i-2].Type, out var instructionTokenType) && instructionTokenType == TokenType.Instruction)
                        {
                            startIndex = token.StartIndex - 1;
                        }
                        else
                        {
                            ignore = true;
                        }
                        break;
                }

                if (!ignore)
                {
                    line.Add(new LineItem(startIndex, token.StopIndex, tokenType));
                }
            }
        }
        return builder;
    }

    static FrozenDictionary<int, TokenType> BuildMap()
    {
        var map = new Dictionary<int, TokenType>()
        {
            // manual
            { KickAssemblerLexer.DEC_NUMBER, TokenType.Number },
            { KickAssemblerLexer.HEX_NUMBER, TokenType.Number },
            { KickAssemblerLexer.BIN_NUMBER, TokenType.Number },
            { KickAssemblerLexer.STRING, TokenType.String },
            { KickAssemblerLexer.SINGLE_LINE_COMMENT, TokenType.Comment },
            { KickAssemblerLexer.MULTI_LINE_COMMENT, TokenType.Comment },
            { KickAssemblerLexer.ONLYA, TokenType.InstructionExtension },
            { KickAssemblerLexer.ABS, TokenType.InstructionExtension },
          // Instructions
          { KickAssemblerLexer.ADC, TokenType.Instruction },
          { KickAssemblerLexer.AND, TokenType.Instruction },
          { KickAssemblerLexer.ASL, TokenType.Instruction },
          { KickAssemblerLexer.BCC, TokenType.Instruction },
          { KickAssemblerLexer.BCS, TokenType.Instruction },
          { KickAssemblerLexer.BEQ, TokenType.Instruction },
          { KickAssemblerLexer.BIT, TokenType.Instruction },
          { KickAssemblerLexer.BMI, TokenType.Instruction },
          { KickAssemblerLexer.BNE, TokenType.Instruction },
          { KickAssemblerLexer.BPL, TokenType.Instruction },
          { KickAssemblerLexer.BRA, TokenType.Instruction },
          { KickAssemblerLexer.BRK, TokenType.Instruction },
          { KickAssemblerLexer.BVC, TokenType.Instruction },
          { KickAssemblerLexer.BVS, TokenType.Instruction },
          { KickAssemblerLexer.CLC, TokenType.Instruction },
          { KickAssemblerLexer.CLD, TokenType.Instruction },
          { KickAssemblerLexer.CLI, TokenType.Instruction },
          { KickAssemblerLexer.CLV, TokenType.Instruction },
          { KickAssemblerLexer.CMP, TokenType.Instruction },
          { KickAssemblerLexer.CPX, TokenType.Instruction },
          { KickAssemblerLexer.CPY, TokenType.Instruction },
          { KickAssemblerLexer.DEC, TokenType.Instruction },
          { KickAssemblerLexer.DEX, TokenType.Instruction },
          { KickAssemblerLexer.DEY, TokenType.Instruction },
          { KickAssemblerLexer.EOR, TokenType.Instruction },
          { KickAssemblerLexer.INC, TokenType.Instruction },
          { KickAssemblerLexer.INX, TokenType.Instruction },
          { KickAssemblerLexer.INY, TokenType.Instruction },
          { KickAssemblerLexer.JMP, TokenType.Instruction },
          { KickAssemblerLexer.JSR, TokenType.Instruction },
          { KickAssemblerLexer.LDA, TokenType.Instruction },
          { KickAssemblerLexer.LDY, TokenType.Instruction },
          { KickAssemblerLexer.LDX, TokenType.Instruction },
          { KickAssemblerLexer.LSR, TokenType.Instruction },
          { KickAssemblerLexer.NOP, TokenType.Instruction },
          { KickAssemblerLexer.ORA, TokenType.Instruction },
          { KickAssemblerLexer.PHA, TokenType.Instruction },
          { KickAssemblerLexer.PHX, TokenType.Instruction },
          { KickAssemblerLexer.PHY, TokenType.Instruction },
          { KickAssemblerLexer.PHP, TokenType.Instruction },
          { KickAssemblerLexer.PLA, TokenType.Instruction },
          { KickAssemblerLexer.PLP, TokenType.Instruction },
          { KickAssemblerLexer.PLY, TokenType.Instruction },
          { KickAssemblerLexer.ROL, TokenType.Instruction },
          { KickAssemblerLexer.ROR, TokenType.Instruction },
          { KickAssemblerLexer.RTI, TokenType.Instruction },
          { KickAssemblerLexer.RTS, TokenType.Instruction },
          { KickAssemblerLexer.SBC, TokenType.Instruction },
          { KickAssemblerLexer.SEC, TokenType.Instruction },
          { KickAssemblerLexer.SED, TokenType.Instruction },
          { KickAssemblerLexer.SEI, TokenType.Instruction },
          { KickAssemblerLexer.STA, TokenType.Instruction },
          { KickAssemblerLexer.STX, TokenType.Instruction },
          { KickAssemblerLexer.STY, TokenType.Instruction },
          { KickAssemblerLexer.STZ, TokenType.Instruction },
          { KickAssemblerLexer.TAX, TokenType.Instruction },
          { KickAssemblerLexer.TAY, TokenType.Instruction },
          { KickAssemblerLexer.TSX, TokenType.Instruction },
          { KickAssemblerLexer.TXA, TokenType.Instruction },
          { KickAssemblerLexer.TXS, TokenType.Instruction },
          { KickAssemblerLexer.TYA, TokenType.Instruction },
          // Brackets
          { KickAssemblerLexer.OPEN_BRACE               , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_BRACE              , TokenType.Bracket },
          { KickAssemblerLexer.OPEN_BRACKET             , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_BRACKET            , TokenType.Bracket },
          { KickAssemblerLexer.OPEN_PARENS              , TokenType.Bracket },
          { KickAssemblerLexer.CLOSE_PARENS             , TokenType.Bracket },
          // Operators
          { KickAssemblerLexer.PLUS                     , TokenType.Operator },
          { KickAssemblerLexer.MINUS                    , TokenType.Operator },
          { KickAssemblerLexer.STAR                     , TokenType.Operator },
          { KickAssemblerLexer.DIV                      , TokenType.Operator },
          { KickAssemblerLexer.PERCENT                  , TokenType.Operator },
          { KickAssemblerLexer.AMP                      , TokenType.Operator },
          { KickAssemblerLexer.BITWISE_OR               , TokenType.Operator },
          { KickAssemblerLexer.CARET                    , TokenType.Operator },
          { KickAssemblerLexer.BANG                     , TokenType.Operator },
          { KickAssemblerLexer.TILDE                    , TokenType.Operator },
          { KickAssemblerLexer.AT                       , TokenType.Operator },
          { KickAssemblerLexer.ASSIGNMENT               , TokenType.Operator },
          { KickAssemblerLexer.LT                       , TokenType.Operator },
          { KickAssemblerLexer.GT                       , TokenType.Operator },
          { KickAssemblerLexer.INTERR                   , TokenType.Operator },
          { KickAssemblerLexer.DOUBLE_COLON             , TokenType.Operator },
          { KickAssemblerLexer.OP_COALESCING            , TokenType.Operator },
          { KickAssemblerLexer.OP_INC                   , TokenType.Operator },
          { KickAssemblerLexer.OP_DEC                   , TokenType.Operator },
          { KickAssemblerLexer.OP_AND                   , TokenType.Operator },
          { KickAssemblerLexer.OP_OR                    , TokenType.Operator },
          { KickAssemblerLexer.OP_PTR                   , TokenType.Operator },
          { KickAssemblerLexer.OP_EQ                    , TokenType.Operator },
          { KickAssemblerLexer.OP_NE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_LE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_GE                    , TokenType.Operator },
          { KickAssemblerLexer.OP_ADD_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_SUB_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_MULT_ASSIGNMENT       , TokenType.Operator },
          { KickAssemblerLexer.OP_DIV_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_MOD_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_AND_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_OR_ASSIGNMENT         , TokenType.Operator },
          { KickAssemblerLexer.OP_XOR_ASSIGNMENT        , TokenType.Operator },
          { KickAssemblerLexer.OP_LEFT_SHIFT            , TokenType.Operator },
          { KickAssemblerLexer.OP_RIGHT_SHIFT           , TokenType.Operator },
          { KickAssemblerLexer.OP_LEFT_SHIFT_ASSIGNMENT , TokenType.Operator },
          { KickAssemblerLexer.OP_COALESCING_ASSIGNMENT , TokenType.Operator },
          { KickAssemblerLexer.OP_RANGE                 , TokenType.Operator },
          // Directives
          { KickAssemblerLexer.BINARY_TEXT, TokenType.Directive },
          { KickAssemblerLexer.C64_TEXT, TokenType.Directive },
          { KickAssemblerLexer.TEXT_TEXT, TokenType.Directive },
          { KickAssemblerLexer.ENCODING, TokenType.Directive },
          { KickAssemblerLexer.FILL, TokenType.Directive },
          { KickAssemblerLexer.FILLWORD, TokenType.Directive },
          { KickAssemblerLexer.LOHIFILL, TokenType.Directive },
          { KickAssemblerLexer.BYTE, TokenType.Directive },
          { KickAssemblerLexer.WORD, TokenType.Directive },
          { KickAssemblerLexer.DWORD, TokenType.Directive },
          { KickAssemblerLexer.CPU, TokenType.Directive },
          { KickAssemblerLexer.CPU6502NOILLEGALS, TokenType.Directive },
          { KickAssemblerLexer.CPU6502, TokenType.Directive },
          { KickAssemblerLexer.DTV, TokenType.Directive },
          { KickAssemblerLexer.CPU65C02, TokenType.Directive },
          { KickAssemblerLexer.ASSERT, TokenType.Directive },
          { KickAssemblerLexer.ASSERTERROR, TokenType.Directive },
          { KickAssemblerLexer.PRINT, TokenType.Directive },
          { KickAssemblerLexer.PRINTNOW, TokenType.Directive },
          { KickAssemblerLexer.VAR, TokenType.Directive },
          { KickAssemblerLexer.CONST, TokenType.Directive },
          { KickAssemblerLexer.IF, TokenType.Directive },
          { KickAssemblerLexer.ELSE, TokenType.Directive },
          { KickAssemblerLexer.ERRORIF, TokenType.Directive },
          { KickAssemblerLexer.EVAL, TokenType.Directive },
          { KickAssemblerLexer.FOR, TokenType.Directive },
          { KickAssemblerLexer.WHILE, TokenType.Directive },
          { KickAssemblerLexer.STRUCT, TokenType.Directive },
          { KickAssemblerLexer.DEFINE, TokenType.Directive },
          { KickAssemblerLexer.FUNCTION, TokenType.Directive },
          { KickAssemblerLexer.RETURN, TokenType.Directive },
          { KickAssemblerLexer.MACRO, TokenType.Directive },
          { KickAssemblerLexer.PSEUDOCOMMAND, TokenType.Directive },
          { KickAssemblerLexer.PSEUDOPC, TokenType.Directive },
          { KickAssemblerLexer.UNDEF, TokenType.Directive },
          { KickAssemblerLexer.ENDIF, TokenType.Directive },
          { KickAssemblerLexer.ELIF, TokenType.Directive },
          { KickAssemblerLexer.IMPORT, TokenType.Directive },
          { KickAssemblerLexer.IMPORTONCE, TokenType.Directive },
          { KickAssemblerLexer.IMPORTIF, TokenType.Directive },
          { KickAssemblerLexer.NAMESPACE, TokenType.Directive },
          { KickAssemblerLexer.SEGMENT, TokenType.Directive },
          { KickAssemblerLexer.SEGMENTDEF, TokenType.Directive },
          { KickAssemblerLexer.SEGMENTOUT, TokenType.Directive },
          { KickAssemblerLexer.MODIFY, TokenType.Directive },
          { KickAssemblerLexer.FILEMODIFY, TokenType.Directive },
          { KickAssemblerLexer.PLUGIN, TokenType.Directive },
          { KickAssemblerLexer.LABEL, TokenType.Directive },
          { KickAssemblerLexer.FILE, TokenType.Directive },
          { KickAssemblerLexer.DISK, TokenType.Directive },
          { KickAssemblerLexer.PC, TokenType.Directive },
          { KickAssemblerLexer.BREAK, TokenType.Directive },
          { KickAssemblerLexer.WATCH, TokenType.Directive },
          { KickAssemblerLexer.ZP, TokenType.Directive },
          // Colors
          { KickAssemblerLexer.BLACK, TokenType.Color },
          { KickAssemblerLexer.WHITE, TokenType.Color },
          { KickAssemblerLexer.RED, TokenType.Color },
          { KickAssemblerLexer.CYAN, TokenType.Color },
          { KickAssemblerLexer.PURPLE, TokenType.Color },
          { KickAssemblerLexer.GREEN, TokenType.Color },
          { KickAssemblerLexer.BLUE, TokenType.Color },
          { KickAssemblerLexer.YELLOW, TokenType.Color },
          { KickAssemblerLexer.ORANGE, TokenType.Color },
          { KickAssemblerLexer.BROWN, TokenType.Color },
          { KickAssemblerLexer.LIGHT_RED, TokenType.Color },
          { KickAssemblerLexer.DARK_GRAY, TokenType.Color },
          { KickAssemblerLexer.DARK_GREY, TokenType.Color },
          { KickAssemblerLexer.GRAY, TokenType.Color },
          { KickAssemblerLexer.GREY, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GREEN, TokenType.Color },
          { KickAssemblerLexer.LIGHT_BLUE, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GRAY, TokenType.Color },
          { KickAssemblerLexer.LIGHT_GREY, TokenType.Color },
        };
        return map.ToFrozenDictionary();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Breakpoints.Breakpoints.CollectionChanged -= BreakpointsOnCollectionChanged;
            _compilerErrors.Lines.CollectionChanged -= CompilerErrors_LinesOnCollectionChanged;
        }
        base.Dispose(disposing);
    }
}