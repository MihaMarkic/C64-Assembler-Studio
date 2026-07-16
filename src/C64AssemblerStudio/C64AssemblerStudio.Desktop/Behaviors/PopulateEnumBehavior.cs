using Avalonia.Controls;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop.Behaviors;

public class PopulateEnumBehavior : ClassicBehavior<ComboBox>
{
    private static readonly EnumDisplayTextMapper Mapper;
    static PopulateEnumBehavior()
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
                .Select(i => new ComboBoxKeyValueItem<object> (i.Key, i.Value)).ToImmutableArray();
        }
        base.Attached();
    }
    
    

    public static ImmutableList<ComboBoxKeyValueItem<ViceStartType>> ViceStartTypes
    {
        get
        {
            var items = Mapper.GetMapEnum<ViceStartType>();
            return items
                .OrderBy(i => i.Key)
                .Select(i => new ComboBoxKeyValueItem<ViceStartType>(i.Key, i.Value))
                .ToImmutableList();
        }
    }
}

public record ComboBoxKeyValueItem<T>(T Key, string Text);