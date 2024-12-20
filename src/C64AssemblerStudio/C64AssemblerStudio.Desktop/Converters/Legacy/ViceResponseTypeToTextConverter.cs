﻿using Righthand.ViceMonitor.Bridge.Responses;

namespace C64AssemblerStudio.Desktop.Converters;

public class ViceResponseTypeToTextConverter : ParameterlessValueConverter<ViceResponse?, string>
{
    public override string? Convert(ViceResponse? value, Type targetType, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }
        return value.GetType().Name.Replace("Response", "");
    }

    public override ViceResponse? ConvertBack(string? value, Type targetType, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
