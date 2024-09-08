using System.Collections.Frozen;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models.Program;
using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Engine.Services.Implementation;

/// <summary>
/// Collects syntax elements from parsed tree and semantic errors.
/// </summary>
/// <remarks>
/// Semantic errors are obtained when debugging data is available - during runtime usually and
/// contains such data as invalid memspace and label names.
/// </remarks>
public class BreakpointConditionsListener : BreakpointConditionsParserBaseListener
    {
        private readonly ILogger<BreakpointConditionsListener> _logger;
        private readonly List<SyntaxEditorError> _errors = new ();
        private readonly FrozenDictionary<string, Label>? _labels;
        private readonly FrozenDictionary<string, BankItem>? _bankItemsByName;
        public bool HasErrors => _errors.Count > 0;
        public ImmutableArray<SyntaxEditorError> Errors => [.._errors];

        public BreakpointConditionsListener(ILogger<BreakpointConditionsListener> logger, Globals globals, IVice vice)
        {
            _logger = logger;
            if (globals.Project is KickAssProjectViewModel project)
            {
                _labels = project.Labels;
            }
            _bankItemsByName = vice.BankItemsByName;
        }

        public override void EnterConditionParens(BreakpointConditionsParser.ConditionParensContext context)
        {
            AddToken(BreakpointDetailConditionTokenType.Parens, context.OPEN_PARENS());
            base.EnterConditionParens(context);
            AddToken(BreakpointDetailConditionTokenType.Parens, context.CLOSE_PARENS());
        }

        internal List<SyntaxEditorToken> Tokens { get; } = new List<SyntaxEditorToken>();

        private void AddSyntaxError(SyntaxEditorErrorKind kind, ITerminalNode node, string? message = null)
        {
            AddSyntaxError(kind, node.Symbol, message);
        }
        private void AddSyntaxError(SyntaxEditorErrorKind kind, IToken token, string? message = null)
        {
            var symbol = token;
            _errors.Add(new SyntaxEditorError(kind, symbol.Line, symbol.Column,
                symbol.Length(), message));
        }

        public override void EnterRegister(BreakpointConditionsParser.RegisterContext context)
        {
            var token = context.UNQUOTED_STRING();
            if (token is not null)
            {
                AddToken(BreakpointDetailConditionTokenType.Register, token);
                string text = token.GetText();
                if (!C64Globals.Registers.Contains(text))
                {
                    AddSyntaxError(SyntaxEditorErrorKind.InvalidRegister, token);
                }
            }
            base.EnterRegister(context);
        }

        public override void EnterHexNumber(BreakpointConditionsParser.HexNumberContext context)
        {
            var token = context.HEX_NUMBER();
            AddToken(BreakpointDetailConditionTokenType.Number, token);
            base.EnterHexNumber(context);
        }

        public override void EnterLabel(BreakpointConditionsParser.LabelContext context)
        {
            var node = context.UNQUOTED_STRING();
            if (node is not null)
            {
                AddToken(BreakpointDetailConditionTokenType.Label, node);
                // _labels is null when there is no debug info
                // and as such can't verify label name
                if (_labels is not null && !_labels.ContainsKey(node.GetText()))
                {
                    AddSyntaxError(SyntaxEditorErrorKind.InvalidLabel, node);
                }
            }
            base.EnterLabel(context);
        }

        public override void EnterOperator(BreakpointConditionsParser.OperatorContext context)
        {
            var operation = (ITerminalNode)context.GetChild(0);
            AddToken(BreakpointDetailConditionTokenType.Operator, operation);
            base.EnterOperator(context);
        }

        public override void EnterBank(BreakpointConditionsParser.BankContext context)
        {
            if (context.bank is not null)
            {
                AddToken(BreakpointDetailConditionTokenType.Bank, context.bank);
                if (_bankItemsByName is not null && !_bankItemsByName.ContainsKey(context.bank.Text))
                {
                    AddSyntaxError(SyntaxEditorErrorKind.InvalidBank, context.bank);
                }
            }

            base.EnterBank(context);
        }

        public override void EnterMemspace(BreakpointConditionsParser.MemspaceContext context)
        {
            var memspaceVariable = context.memspace;
            var variableNode = memspaceVariable.UNQUOTED_STRING() ?? memspaceVariable.DEC_NUMBER();
            AddToken(BreakpointDetailConditionTokenType.Memspace, variableNode);
            string text = variableNode.GetText();
            // only c is valid textual memspace
            if (!C64Globals.MemspacePrefixes.Contains(text))
            {
                AddSyntaxError(SyntaxEditorErrorKind.InvalidMemspace, variableNode);
            }
            
            base.EnterMemspace(context);
        }

        void AddToken(BreakpointDetailConditionTokenType type, ITerminalNode? token)
        {
            if (token is not null)
            {
                AddToken(type, token.Symbol);
            }
        }
        void AddToken(BreakpointDetailConditionTokenType type, IToken? token)
        {
            if (token is not null)
            {
                var syntaxToken =
                    new SyntaxEditorToken(type, token.Line, token.Column, token.Length());
                Tokens.Add(syntaxToken);
            }
        }
    }