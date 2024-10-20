﻿using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Desktop.Views.Files;

public class SyntaxColorizer : DocumentColorizingTransformer
{
    public (int Start, int End)? ExecutionLine { get; set; }
    private readonly AssemblerFileViewModel _file;
    private ImmutableHashSet<int> _callStackLineNumbers;
    private static readonly TextDecorationCollection Squiggle;

    static SyntaxColorizer()
    {
        Squiggle = TextDecorations.Underline;
        Squiggle.Add(new TextDecoration { StrokeThickness = 4, Stroke = Brushes.Red});
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
        if (!line.IsDeleted && !_file.Lines.IsEmpty &&  line.LineNumber <= _file.Lines.Length)
        {
            var lineSyntax = _file.Lines[line.LineNumber-1];
            if (lineSyntax?.Items.IsEmpty == false)
            {
                foreach (var syntax in lineSyntax.Items)
                {
                    Action<VisualLineElement>? apply = syntax.TokenType switch
                    {
                        AssemblerFileViewModel.TokenType.String => ApplyStringChanges,
                        AssemblerFileViewModel.TokenType.Instruction => ApplyInstructionChanges,
                        AssemblerFileViewModel.TokenType.InstructionExtension => ApplyInstructionExtensionChanges,
                        AssemblerFileViewModel.TokenType.Comment => ApplyCommentChanges,
                        AssemblerFileViewModel.TokenType.Number => ApplyNumberChanges,
                        AssemblerFileViewModel.TokenType.Directive => ApplyDirectiveChanges,
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

            if (_file.Errors.TryGetValue(line.LineNumber, out var errors))
            {
                foreach (var e in errors)
                {
                    int start = line.Offset + e.Start;
                    int end = line.Offset + e.End;
                    // in case of errors, user can alter file (delete a char where error pointed) and error is still pointing to original length
                    if (end <= line.EndOffset)
                    {
                        ChangeLinePart(start, end, ApplySyntaxErrorChanges);
                    }
                }
            }

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
    }
}
