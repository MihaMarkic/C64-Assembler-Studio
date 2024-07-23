using Avalonia;
using AvaloniaEdit;

namespace AvaloniaEdit;

public static class TextEditorExtensions
{
    public static Task WaitForLayoutUpdatedAsync(this TextEditor editor)
    {
        var tcs = new TaskCompletionSource();
        EventHandler<AvaloniaPropertyChangedEventArgs> propertyChangedHandler = default!;
        propertyChangedHandler = (s, e) =>
        {
            if (e.Property == Visual.BoundsProperty)
            {
                editor.PropertyChanged -= propertyChangedHandler;
                tcs.SetResult();
            }
        };
        editor.PropertyChanged += propertyChangedHandler;
        return tcs.Task;
    }
}