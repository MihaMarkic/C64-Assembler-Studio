using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Converters;
//public class BreakpointAddressRangesToStringConverter : ParameterlessValueConverter<IReadOnlyCollection<BreakpointAddressRange>, string>
//{
//    public override string? Convert(IReadOnlyCollection<BreakpointAddressRange>? value, Type targetType, CultureInfo culture)
//    {
//        if (value is not null)
//        {
//            if (value.Count > 0)
//            {
//                return string.Join("; ", value.Select(r => $"${r.Start:x4}-${r.End:x4}"));
//            }
//            return string.Empty;
//        }
//        return null;
//    }

//    public override IReadOnlyCollection<BreakpointAddressRange>? ConvertBack(string? value, Type targetType, CultureInfo culture)
//    {
//        throw new NotImplementedException();
//    }
//}
