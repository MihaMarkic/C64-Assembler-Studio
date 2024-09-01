using Avalonia.Data.Converters;
using Avalonia.Media;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Converters;

public static class StudioConverters
{
    public static readonly IValueConverter IsBuildStatusVisible =
        new FuncValueConverter<BuildStatus, bool>(x => x != BuildStatus.Idle);
    public static readonly IValueConverter BuildStatusToString =
        new FuncValueConverter<BuildStatus, string?>(bs => bs.GetDisplayText());
    public static readonly IValueConverter IsDebuggingStatusVisible =
        new FuncValueConverter<DebuggingStatus, bool>(x => x != DebuggingStatus.Idle);
    public static readonly IValueConverter DebuggingStatusToString =
        new FuncValueConverter<DebuggingStatus, string?>(bs => bs.GetDisplayText());
    public static readonly IValueConverter ValueToHexAddress =
        new FuncValueConverter<ushort?, string?>(s => s?.ToString("X4"));
    public static readonly IValueConverter AppendDirectorySeparator =
        new FuncValueConverter<string?, string?>(s => string.IsNullOrWhiteSpace(s) ? "": $"{s}{(s.EndsWith(Path.DirectorySeparatorChar) ? "": Path.DirectorySeparatorChar)}");
    public static readonly IValueConverter ErrorToBrush = new FuncValueConverter<bool, IBrush?>(e => e ? Brushes.Red : null);

    public static readonly IValueConverter ToEditorLine =
        new FuncValueConverter<int, int>(l => l + 1);
}