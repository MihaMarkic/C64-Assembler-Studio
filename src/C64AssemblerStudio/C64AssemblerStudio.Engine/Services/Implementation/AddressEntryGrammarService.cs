using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using C64AssemblerStudio.Engine.Grammars;
using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class AddressEntryGrammarService : IAddressEntryGrammarService
{
    public (bool HasError, ImmutableArray<SyntaxError> ErrorText) VerifyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return(true, ImmutableArray<SyntaxError>.Empty.Add(new SyntaxError(0, 0, "Value can not be empty")));
        }
        var input = new AntlrInputStream(text);
        var lexer = new AddressEntryLexer(input);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new AddressEntryParser(tokenStream)
        {
            BuildParseTree = true
        };
        var errorListener = new ErrorListener();
        parser.AddErrorListener(errorListener);
        var tree = parser.arguments();
        var listener = new AddressEntryParserBaseListener();
        ParseTreeWalker.Default.Walk(listener, tree);

        return (errorListener.HasErrors, [..errorListener.Errors]);
    }
}


internal class ErrorListener : IAntlrErrorListener<IToken>
{
    public bool HasErrors => Errors.Count > 0;
    public List<SyntaxError> Errors { get; } = new();
    public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        Errors.Add(new SyntaxError(line, charPositionInLine, msg));
    }
}