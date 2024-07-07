using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using System;
using System.Linq;
using System.Collections.Immutable;

namespace C64AssemblerStudio.Behaviors;

public class PopulateEnumBehavior : ClassicBehavior<ComboBox>
{
    static readonly EnumDisplayTextMapper mapper;
    static PopulateEnumBehavior()
    {
        mapper = IoC.Host.Services.GetRequiredService<EnumDisplayTextMapper>();
    }
    public Type? Type { get; set; }
    protected override void Attached()
    {
        if (Type is not null)
        {
            var items = mapper.GetMapEnum(Type);
            AssociatedObject!.ItemsSource = items
                .OrderBy(i => i.Key)
                .Select(i => new ComboBoxKeyValueItem (i.Key, i.Value)).ToImmutableArray();
        }
        base.Attached();
    }
}

public record ComboBoxKeyValueItem(object Key, string Text);
