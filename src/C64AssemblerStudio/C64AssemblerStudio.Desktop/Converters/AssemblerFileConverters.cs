using Avalonia.Data.Converters;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Desktop.Converters;

public static class AssemblerFileConverters
{
    /// <summary>
    /// When byte dump is visible (true) it returns 1, 2 otherwise.
    /// </summary>
    public static readonly IValueConverter EditorColumnSpan =
        new FuncValueConverter<bool, int>(x => x ? 1 : 3);

    public static readonly IValueConverter FileLocationToString =
        new FuncValueConverter<TextRange, string?>(tr => (tr?.Start.Row+1)?.ToString());

    public static readonly IValueConverter LabelsToString =
        new FuncValueConverter<IList<string>, string?>(l => l is not null ? string.Join(",", l) : null);

    public static readonly IValueConverter DataToString =
        new FuncValueConverter<IList<byte>, string?>(d =>
            d is not null ? string.Join(",", d.Select(b => b.ToString("X2"))) : null);
}