using Avalonia;
using Avalonia.Controls;
using C64AssemblerStudio.Engine.ViewModels.Tools;

namespace C64AssemblerStudio.Desktop.Behaviors;

public class ScrollIntoViewGridRowBehavior : ClassicBehavior<DataGrid>
{
    public static readonly StyledProperty<MemoryViewerRow?> ItemProperty =
        AvaloniaProperty.Register<ScrollIntoViewGridRowBehavior, MemoryViewerRow?>(nameof(Item));

    public ScrollIntoViewGridRowBehavior()
    {
        this.GetObservable(ItemProperty).Subscribe(ItemPropertyChanged);
    }
    public MemoryViewerRow? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }
    void ItemPropertyChanged(MemoryViewerRow? item)
    {
        if (AssociatedObject is not null && item is not null)
        {
            AssociatedObject.ScrollIntoView(item, null);
        }
    }
}
