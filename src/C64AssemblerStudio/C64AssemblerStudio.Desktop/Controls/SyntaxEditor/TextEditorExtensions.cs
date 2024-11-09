using Avalonia;

namespace AvaloniaEdit;

public static class TextEditorExtensions
{
    /// <summary>
    /// Awaits layout update.
    /// </summary>
    /// <param name="editor"></param>
    /// <returns></returns>
    /// <remarks>Used for awaiting layout completition before jumping to any line.</remarks>
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