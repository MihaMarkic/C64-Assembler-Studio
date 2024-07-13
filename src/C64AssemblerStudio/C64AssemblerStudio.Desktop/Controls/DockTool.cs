using Avalonia;
using Avalonia.Controls;

namespace C64AssemblerStudio.Desktop.Controls;
public class DockTool: ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<DockTool, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}
