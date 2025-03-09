using System.Collections;
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Desktop.Behaviors;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Desktop.Converters;

public static class StudioConverters
{
    private static readonly EnumDisplayTextMapper _mapper;

    static StudioConverters()
    {
        _mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    
    public static readonly IValueConverter IsBuildStatusVisible =
        new FuncValueConverter<BuildStatus, bool>(x => x != BuildStatus.Idle);

    public static readonly IValueConverter BuildStatusToString =
        new FuncValueConverter<BuildStatus, string?>(bs => bs.BuildStatusToString());

    public static readonly IValueConverter IsDebuggingStatusVisible =
        new FuncValueConverter<DebuggingStatus, bool>(x => x != DebuggingStatus.Idle);

    public static readonly IValueConverter DebuggingStatusToString =
        new FuncValueConverter<DebuggingStatus, string?>(bs => bs.DebugStatusToString());

    public static readonly IValueConverter ValueToHexAddress =
        new FuncValueConverter<ushort?, string?>(s => s?.ToString("X4"));

    public static readonly IValueConverter AppendDirectorySeparator =
        new FuncValueConverter<string?, string?>(s =>
            string.IsNullOrWhiteSpace(s)
                ? ""
                : $"{s}{(s.EndsWith(Path.DirectorySeparatorChar) ? "" : Path.DirectorySeparatorChar)}");

    public static readonly IValueConverter ErrorToBrush =
        new FuncValueConverter<bool, IBrush?>(e => e ? Brushes.Red : null);

    public static readonly IValueConverter DefineSymbolsToString =
        new FuncValueConverter<FrozenSet<string>, string>(ds => ds is not null ? string.Join(",", ds) : string.Empty);

    public static readonly IValueConverter ListMinCountToBool =
        new FuncValueConverter<IList, int, bool>((l, min) => l is not null && l.Count > min);

    public static readonly IValueConverter ToEditorLine =
        new FuncValueConverter<int, int>(l => l + 1);

    public static readonly IValueConverter StatusInfoToCaretText =
        new FuncValueConverter<StatusInfoViewModel, string>(si => $"{si?.EditorCaretLine}:{si?.EditorCaretColumn}");

    public static readonly IMultiValueConverter LineAndColumnToString =
        new FuncMultiValueConverter<int, string>(p =>
        {
            var lineAndColumn = p.ToImmutableArray();
            return lineAndColumn.Length == 2 ? $"{lineAndColumn[0]}:{lineAndColumn[1]}" : ":";
        });

    public static readonly IValueConverter SyntaxErrorSourceToString =
        new FuncValueConverter<SyntaxErrorSource, string>(s => s switch
        {
            SyntaxErrorLexerSource => "Lexer",
            SyntaxErrorParserSource => "Parser",
            SyntaxErrorFileSource => "File",
            SyntaxErrorCompiledFileSource => "Compiler",
            _ => "?"
        });

    public static readonly IValueConverter Add =
        new FuncValueConverter<int, int, int>((v, p) => v + p);

    private static readonly IBrush ProjectFileReferenceCompletionOptionSourceBrush = new SolidColorBrush(Colors.DarkGreen);
    private static readonly IBrush LibraryFileReferenceCompletionOptionSourceBrush = new SolidColorBrush(Colors.DarkOrange);
    private static readonly IBrush PreprocessorDirectiveCompletionOptionSourceBrush = new SolidColorBrush(Colors.DarkGray);
    private static readonly IBrush DefaultCompletionOptionSourceBrush = new SolidColorBrush(Colors.Black);

    public static readonly IValueConverter CompletionSourceToBrushConverter =
        new FuncValueConverter<string, IBrush?>(s =>
        {
            if (s is not null)
            {
                return s switch
                {
                    "Project" => ProjectFileReferenceCompletionOptionSourceBrush,
                    "PPD" => PreprocessorDirectiveCompletionOptionSourceBrush,
                    "Library" => LibraryFileReferenceCompletionOptionSourceBrush,
                    _ => DefaultCompletionOptionSourceBrush,
                };
            }

            return null;
        });
}