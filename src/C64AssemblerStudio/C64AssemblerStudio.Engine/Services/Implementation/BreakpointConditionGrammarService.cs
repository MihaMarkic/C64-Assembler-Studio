using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class BreakpointConditionGrammarService : IBreakpointConditionGrammarService
{
    private readonly ILogger<BreakpointConditionGrammarService> _logger;
    private readonly IServiceProvider _serviceProvider;
    public BreakpointConditionGrammarService(ILogger<BreakpointConditionGrammarService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<(bool HasError, ImmutableArray<SyntaxEditorError> Errors, ImmutableArray<SyntaxEditorToken> Tokens)>
        VerifyTextAsync(string? text, CancellationToken ct = default)
        {
            return await Task.Factory.StartNew(o => VerifyText((string?)o), text, ct);
        }
    public (bool HasError, ImmutableArray<SyntaxEditorError> Errors, ImmutableArray<SyntaxEditorToken> Tokens) VerifyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (false, [], []);
        }

        var input = new AntlrInputStream(text);
        var lexer = new BreakpointConditionsLexer(input);
        var lexerErrorListener = new LexerErrorListener();
        lexer.AddErrorListener(lexerErrorListener);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new BreakpointConditionsParser(tokenStream)
        {
            BuildParseTree = true
        };
        var grammarErrorListener = new ParserErrorListener();
        parser.AddErrorListener(grammarErrorListener);
        var tree = parser.root();
        var semanticListener = _serviceProvider.GetRequiredService<BreakpointConditionsListener>();
        ParseTreeWalker.Default.Walk(semanticListener, tree);

        return (
            grammarErrorListener.HasErrors || lexerErrorListener.HasErrors || semanticListener.HasErrors,
            [..lexerErrorListener.Errors.Union(grammarErrorListener.Errors).Union(semanticListener.Errors)],
            [..semanticListener.Tokens]);
    }

    internal class LexerErrorListener : IAntlrErrorListener<int>
    {
        public bool HasErrors => Errors.Count > 0;
        public List<SyntaxEditorError> Errors { get; } = new();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line,
            int charPositionInLine,
            string msg, RecognitionException e)
        {
            Errors.Add(new SyntaxEditorError(SyntaxEditorErrorKind.Generic, line, charPositionInLine, 1, msg));
        }
    }

    internal class ParserErrorListener : IAntlrErrorListener<IToken>
    {
        public bool HasErrors => Errors.Count > 0;
        public List<SyntaxEditorError> Errors { get; } = new();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine,
            string msg, RecognitionException e)
        {
            //offendingSymbol.Type
            Errors.Add(new SyntaxEditorError(SyntaxEditorErrorKind.Generic, line, charPositionInLine,
                offendingSymbol.StopIndex - offendingSymbol.StartIndex + 1, msg));
        }
    }
}

