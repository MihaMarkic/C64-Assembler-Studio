using C64AssemblerStudio.Engine.Models;

namespace C64AssemblerStudio.Desktop.Converters;
public class BreakpointBindingToStringConverter : ParameterlessValueConverter<BreakpointBind, string>
{
    public override string? Convert(BreakpointBind? value, Type targetType, CultureInfo culture)
    {
        return value switch
        {
            BreakpointNoBind noBind => FormatNoBind(noBind),
            BreakpointLineBind lineBind => $"Line {lineBind.LineNumber + 1}",
            _ => "no bind"
        };
    }

    string FormatNoBind(BreakpointNoBind bind)
    {
        if (bind.EndAddress is not null)
        {
            return $"From {bind.StartAddress} to {bind.StartAddress}";
        }

        return $"{bind.StartAddress}";
    }

    public override BreakpointBind? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
