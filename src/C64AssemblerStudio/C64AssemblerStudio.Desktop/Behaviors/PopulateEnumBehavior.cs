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
    protected override void Attached()
    {
        if (Type is not null)
        {
            var items = _mapper.GetMapEnum(Type);
            AssociatedObject!.ItemsSource = items
                .OrderBy(i => i.Key)
                .Select(i => new ComboBoxKeyValueItem (i.Key, i.Value)).ToImmutableArray();
        }
        base.Attached();
    }
}

public record ComboBoxKeyValueItem(object Key, string Text);
