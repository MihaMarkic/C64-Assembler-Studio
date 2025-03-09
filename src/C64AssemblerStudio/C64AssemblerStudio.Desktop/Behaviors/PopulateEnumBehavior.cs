using Avalonia.Controls;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop.Behaviors;

public class PopulateEnumBehavior : ClassicBehavior<ComboBox>
{
    static readonly EnumDisplayTextMapper _mapper;
    static PopulateEnumBehavior()
    {
        _mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public Type? Type { get; set; }
    public EnumMapping EnumMapping { get; set; } = EnumMapping.None;
    protected override void Attached()
    {
        if (Type is not null)
        {
            switch (EnumMapping)
            {
                    case EnumMapping.ComboBoxKeyValueItem:
                        var items = _mapper.GetMapEnum(Type);
                        AssociatedObject!.ItemsSource = items
                            .OrderBy(i => i.Key)
                            .Select(i => new ComboBoxKeyValueItem (i.Key, i.Value)).ToImmutableList();
                        break;
                    case EnumMapping.None:
                        AssociatedObject!.ItemsSource = Enum.GetValues(Type).Cast<object>().ToImmutableList();
                        break;
            }
        }
        base.Attached();
    }
}

public record ComboBoxKeyValueItem(object Key, string Text);

public enum EnumMapping
{
    None,
    ComboBoxKeyValueItem
}
