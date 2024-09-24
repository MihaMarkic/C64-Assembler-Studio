using System.Collections.Frozen;
using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using C64AssemblerStudio.Engine.Grammars;
using C64AssemblerStudio.Engine.Services.Abstract;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public class AddressEntryGrammarService : IAddressEntryGrammarService
{
    public (bool HasError, ImmutableArray<SyntaxError> ErrorText) VerifyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (true, ImmutableArray<SyntaxError>.Empty.Add(new SyntaxError(0, 0, "Value can not be empty")));
        }

        var input = new AntlrInputStream(text);
        var lexer = new AddressEntryLexer(input);
        var lexerErrorListener = new LexerErrorsListener();
        lexer.AddErrorListener(lexerErrorListener);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new AddressEntryParser(tokenStream)
        {
            BuildParseTree = true
        };
        var errorListener = new ParserErrorListener();
        parser.AddErrorListener(errorListener);
        var tree = parser.address();
        var listener = new AddressEntryParserBaseListener();
        ParseTreeWalker.Default.Walk(listener, tree);

        return (errorListener.HasErrors || lexerErrorListener.HasErrors, [..errorListener.Errors]);
    }

    public ushort? CalculateAddress(IDictionary<string, Label> labels, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var input = new AntlrInputStream(text);
        var lexer = new AddressEntryLexer(input);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new AddressEntryParser(tokenStream)
        {
            BuildParseTree = true,
        };

        var evaluator = new EvaluatorVisitor(labels);
        return evaluator.Visit(parser.address());
    }
    
    private class LexerErrorsListener: IAntlrErrorListener<int>
    {
        public bool HasErrors { get; private set; }
        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            HasErrors = true;
        }
    }

    private class EvaluatorVisitor : AddressEntryParserBaseVisitor<ushort>
    {
        private readonly IDictionary<string, Label> _labelsMap;

        public EvaluatorVisitor(IDictionary<string, Label> labelsMap)
        {
            _labelsMap = labelsMap;
        }

        public override ushort VisitPlus(AddressEntryParser.PlusContext context)
        {
            return (ushort)(Visit(context.left) + Visit(context.right));
        }

        public override ushort VisitMinus(AddressEntryParser.MinusContext context)
        {
            return (ushort)(Visit(context.left) - Visit(context.right));
        }

        public override ushort VisitMultiplication(AddressEntryParser.MultiplicationContext context)
        {
            return (ushort)(Visit(context.left) * Visit(context.right));
        }

        public override ushort VisitDivision(AddressEntryParser.DivisionContext context)
        {
            ushort right = Visit(context.right);
            if (right == 0)
            {
                throw new Exception($"Can't divide against 0 (from {context.GetText()})");
            }

            return (ushort)(Visit(context.left) / Visit(context.right));
        }

        public override ushort VisitParens(AddressEntryParser.ParensContext context)
        {
            return Visit(context.arguments());
        }

        public override ushort VisitLabel(AddressEntryParser.LabelContext context)
        {
            string labelName = context.GetText();
            if (_labelsMap.TryGetValue(labelName, out var label))
            {
                return label.Address;
            }

            throw new Exception($"Couldn't find label {context.GetText()}");
        }

        public override ushort VisitDecNumber(AddressEntryParser.DecNumberContext context)
        {
            if (ushort.TryParse(context.GetText(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new Exception($"Couldn't parse {context.GetText()} as decimal");
        }

        public override ushort VisitBinNumber(AddressEntryParser.BinNumberContext context)
        {
            if (ushort.TryParse(context.GetText().AsSpan()[1..], NumberStyles.BinaryNumber,
                    CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new Exception($"Couldn't parse {context.GetText()} as binary");
        }

        public override ushort VisitHexNumber(AddressEntryParser.HexNumberContext context)
        {
            if (ushort.TryParse(context.GetText().AsSpan()[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var value))
            {
                return value;
            }

            throw new Exception($"Couldn't parse {context.GetText()} as hexadecimal");
        }
    }


    private class ParserErrorListener : IAntlrErrorListener<IToken>
    {
        public bool HasErrors => Errors.Count > 0;
        public List<SyntaxError> Errors { get; } = new();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine,
            string msg, RecognitionException e)
        {
            Errors.Add(new SyntaxError(line, charPositionInLine, msg));
        }
    }
}