﻿using System.Collections;
using System.Collections.Frozen;
using Avalonia.Data.Converters;
using Avalonia.Media;
using C64AssemblerStudio.Desktop.Views;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Converters;

public static class StudioConverters
{
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
}