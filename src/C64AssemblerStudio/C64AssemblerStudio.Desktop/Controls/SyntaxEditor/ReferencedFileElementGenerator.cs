using System.Collections.Frozen;
using System.Diagnostics;
using AvaloniaEdit.Rendering;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Desktop.Controls.SyntaxEditor
{
    public class ReferencedFileElementGenerator: VisualLineElementGenerator
    {
        private ImmutableArray<FileReferenceSyntaxItem> _referencedFiles;
        private FrozenDictionary<int, FileReferenceSyntaxItem> _referencedFilesMap;
        private int? _maxOffset;
        public readonly Action<FileReferenceSyntaxItem> OnClicked;
        public ReferencedFileElementGenerator(ImmutableArray<FileReferenceSyntaxItem> referencedFiles,
            Action<FileReferenceSyntaxItem> onClicked)
        {
            OnClicked = onClicked;
            _referencedFiles = ImmutableArray<FileReferenceSyntaxItem>.Empty;
            _referencedFilesMap = FrozenDictionary<int, FileReferenceSyntaxItem>.Empty;
            UpdateReferencedFiles(referencedFiles);
        }

        private void UpdateReferencedFiles(ImmutableArray<FileReferenceSyntaxItem> referencedFiles)
        {
            _referencedFiles = referencedFiles;
            _referencedFilesMap = referencedFiles.ToFrozenDictionary(rf => rf.Start, rf => rf);
            _maxOffset = referencedFiles.IsEmpty ? null : referencedFiles.Last().Start;
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            // Debug.WriteLine($"{nameof(GetFirstInterestedOffset)} on {startOffset}");
            if (_maxOffset >= startOffset)
            {
                foreach (var rf in _referencedFiles)
                {
                    if (rf.Start >= startOffset)
                    {
                        var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
                        int maxLength = endOffset - rf.Start;
                        if (maxLength >= rf.Length)
                        {
                            var text = CurrentContext.GetText(rf.Start + 1,
                                rf.ReferencedFile.RelativeFilePath.Length);
                            var match = text.Text.AsSpan()[text.Offset..(text.Offset + text.Count)];
                            // Debug.WriteLine(
                            //     $"Matched {match} on offset {rf.Start} against {rf.ReferencedFile.RelativeFilePath}");
                            if (match.SequenceEqual(rf.ReferencedFile.RelativeFilePath))
                            {
                                return rf.Start;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            Debug.WriteLine($"Generating visual element at {offset}");
            var rf = _referencedFilesMap[offset];
            Debug.WriteLine($"Creating {rf.Length} from offset {offset} on line length {CurrentContext.VisualLine.VisualLength}");
            var line = CurrentContext.VisualLine;
            return new ReferencedFileVisualLineLinkText(CurrentContext.VisualLine, rf.Length, rf, OnClicked);
        }
    }
}
