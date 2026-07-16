using Avalonia.Controls;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop.Behaviors;

public class PopulateEnumValuesBehavior : ClassicBehavior<ComboBox>
{
    private static readonly EnumDisplayTextMapper Mapper;
    static PopulateEnumValuesBehavior()
    {
        Mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public Type? Type { get; set; }
    protected override void Attached()
    {
        if (Type is not null)
        {
            var items = Mapper.GetMapEnum(Type);
            AssociatedObject!.ItemsSource = items
                .OrderBy(i => i.Key)
                .Select(i => i.Key).ToImmutableArray();
        }

        base.Attached();
    }
}