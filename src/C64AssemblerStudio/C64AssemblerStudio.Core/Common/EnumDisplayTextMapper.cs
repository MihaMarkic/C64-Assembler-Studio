using System;
using System.ComponentModel.DataAnnotations;

namespace C64AssemblerStudio.Core.Common;

public class EnumDisplayTextMapper : EnumMapper<string>
{
    protected override string Map(Type enumType, Enum value)
    {
        var descriptionAttribute = AttributeRetriever.GetEnumAttribute<DisplayAttribute>(enumType, value);
        return descriptionAttribute?.Description ?? value.ToString();
    }
}
