using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using Microsoft.Extensions.DependencyInjection;

namespace System;

public static class AttributeExtension
{
    private static readonly EnumDisplayTextMapper Mapper;
    static AttributeExtension()
    {
        Mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public static string? GetDisplayText<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        var map = Mapper.GetMapEnum(typeof(TEnum));
        return map[value];
    }
}
