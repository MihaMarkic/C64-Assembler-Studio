using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using Microsoft.Extensions.DependencyInjection;

namespace System;

public static class AttributeExtension
{
    static readonly EnumDisplayTextMapper mapper;
    static AttributeExtension()
    {
        mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public static string? GetDisplayText<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        var map = mapper.GetMapEnum(typeof(TEnum));
        return map[value];
    }
}
