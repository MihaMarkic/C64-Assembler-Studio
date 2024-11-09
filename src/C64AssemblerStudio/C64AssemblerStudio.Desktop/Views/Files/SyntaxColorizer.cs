using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.ViewModels.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Desktop.Views.Files;

public class SyntaxColorizer : DocumentColorizingTransformer
{
    public (int Start, int End)? ExecutionLine { get; set; }
    private readonly AssemblerFileViewModel _file;
    private ImmutableHashSet<int> _callStackLineNumbers;
    private static readonly TextDecorationCollection Squiggle;
    private static readonly ILogger<SyntaxColorizer> _logger;

    static SyntaxColorizer()
    {
        Squiggle = TextDecorations.Underline;
        Squiggle.Add(new TextDecoration { StrokeThickness = 4, Stroke = Brushes.Red });
        _logger = IoC.Host.Services.GetRequiredService<ILogger<SyntaxColorizer>>();
    }

    public SyntaxColorizer(AssemblerFileViewModel file)
    {
        _file = file;
        _callStackLineNumbers = ImmutableHashSet<int>.Empty;
        CreateCallStackLineNumberMap();
    }

    public void CreateCallStackLineNumberMap()
    {
        _callStackLineNumbers = _file.CallStackItems.Select(i => i.FileLocation!.Line1).ToImmutableHashSet();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!line.IsDeleted)
        {
            try
            {
                ColorizeIgnoredContent(line);
                ColorizeSyntax(line);
                //ColorizeErrors(line);

                bool isBackgroundAssigned = false;

                // execution line
                if (ExecutionLine.HasValue && ExecutionLine.Value.Start <= line.LineNumber - 1 &&
                    ExecutionLine.Value.End >= line.LineNumber - 1)
                {
                    ChangeLinePart(line.Offset, line.EndOffset, ApplyExecutionLineChanges);
                    isBackgroundAssigned = true;
                }
                // call stack 
                else if (_callStackLineNumbers.Contains(line.LineNumber))
                {
                    ChangeLinePart(line.Offset, line.EndOffset, ApplyCallStackChanges);
                    isBackgroundAssigned = true;
                }

                if (!isBackgroundAssigned)
                {
                    // switch (sourceLine)
                    // {
                    // case LineViewModel lineViewModel:
                    //     if (lineViewModel.HasBreakpoint)
                    //     {
                    //         ChangeLinePart(line.Offset, line.EndOffset, ApplyBreakpointLineChanges);
                    //     }
                    //     else
                    //     {
                    //         int lineIndex = sourceFileViewModel.GetLineIndex(line.LineNumber - 1);
                    //         if (CallStackLineNumbers.Contains(lineIndex + 1))
                    //         {
                    //             ChangeLinePart(line.Offset, line.EndOffset, ApplyCallStackChanges);
                    //         }
                    //     }
                    //
                    // break;
                    //     default:
                    //         ChangeLinePart(line.Offset, line.EndOffset, ApplyAssemblyChanges);
                    //         break;
                    // }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed colorizing syntax line {LineNumber}", line.LineNumber);
            }
        }
    }

    private void ColorizeErrors(DocumentLine line)
    {
        // if (_file.Errors.TryGetValue(line.LineNumber, out var errors))
        // {
        //     foreach (var e in errors)
        //     {
        //         int start = line.Offset + e.Range.Start!.Value;
        //         int end = line.Offset + e.Range.End!.Value;
        //         // in case of errors, user can alter file (delete a char where error pointed) and error is still pointing to original length
        //         if (end <= line.EndOffset)
        //         {
        //             ChangeLinePart(start, end, ApplySyntaxErrorChanges);
        //         }
        //     }
        // }
    }

    private void ColorizeSyntax(DocumentLine line)
    {
        if (_file.Lines.TryGetValue(line.LineNumber - 1, out var lineSyntax) &&
            !lineSyntax.Items.IsEmpty)
        {
            foreach (var syntax in lineSyntax.Items)
            {
                Action<VisualLineElement>? apply = syntax.TokenType switch
                {
                    TokenType.String => ApplyStringChanges,
                    TokenType.Instruction => ApplyInstructionChanges,
                    TokenType.InstructionExtension => ApplyInstructionExtensionChanges,
                    TokenType.Comment => ApplyCommentChanges,
                    TokenType.Number => ApplyNumberChanges,
                    TokenType.Directive => ApplyDirectiveChanges,
                    // SyntaxElementType.Comment => ApplyCommentChanges,
                    _ => null,
                };
                if (apply is not null)
                {
                    int startOffset = Math.Min(line.EndOffset, Math.Max(line.Offset, syntax.Start));
                    int endOffset = Math.Min(syntax.End + 1, line.EndOffset);
                    ChangeLinePart(startOffset, endOffset, apply);
                }
            }
        }
    }

    private void ColorizeIgnoredContent(DocumentLine line)
    {
        if (_file.IgnoredContent.TryGetValue(line.LineNumber, out var ignoredRange))
        {
            foreach (var ignored in ignoredRange)
            {
                int startOffset = line.Offset + (ignored.Start ?? 0);
                startOffset = Math.Min(line.EndOffset, Math.Max(line.Offset, startOffset));
                int endOffset =  ignored.End is null ? line.EndOffset: line.Offset + ignored.End.Value;
                endOffset = Math.Min(endOffset + 1, line.EndOffset);
                ChangeLinePart(startOffset, endOffset, ApplyIgnoredChanges);
            }
        }
    }

    void ApplyStringChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.String);

    void ApplyInstructionChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Instruction);

    void ApplyInstructionExtensionChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.InstructionExtension);

    void ApplyCommentChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Comment);

    void ApplyNumberChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Number);

    void ApplyDirectiveChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Directive);

    void ApplyAssemblyChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Assembly);

    void ApplyCallStackChanges(VisualLineElement element) =>
        element.TextRunProperties.SetBackgroundBrush(ElementColor.CallStackCall);

    void ApplyIgnoredChanges(VisualLineElement element) =>
        element.TextRunProperties.SetForegroundBrush(ElementColor.Ignored);
    
    void ApplySyntaxErrorChanges(VisualLineElement element)
    {
        element.TextRunProperties.SetTextDecorations(Squiggle);
    }

    void ApplyExecutionLineChanges(VisualLineElement element)
    {
        // This is where you do anything with the line

        element.TextRunProperties.SetForegroundBrush(Brushes.Black);
        element.TextRunProperties.SetBackgroundBrush(Brushes.Yellow);
    }

    void ApplyBreakpointLineChanges(VisualLineElement element)
    {
        // This is where you do anything with the line

        element.TextRunProperties.SetForegroundBrush(Brushes.White);
        element.TextRunProperties.SetBackgroundBrush(ElementColor.BreakpointBackground);
    }

    static class ElementColor
    {
        public static readonly IBrush String = Brushes.DarkRed;
        public static readonly IBrush Comment = Brushes.LightGray;
        public static readonly IBrush Assembly = Brushes.Gray;
        public static readonly IBrush Instruction = Brushes.Blue;
        public static readonly IBrush InstructionExtension = Brushes.DarkBlue;
        public static readonly IBrush Number = Brushes.DarkCyan;
        public static readonly IBrush Directive = Brushes.PaleVioletRed;
        public static readonly IBrush BreakpointBackground = new SolidColorBrush(new Color(0xFF, 0x96, 0x3A, 0x46));
        public static readonly IBrush CallStackCall = new SolidColorBrush(new Color(0x50, 0xB4, 0xE4, 0xB4));
        public static readonly IBrush Ignored = Brushes.LightGray;
    }
}