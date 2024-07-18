using Avalonia;
using Avalonia.Xaml.Interactivity;

namespace C64AssemblerStudio.Desktop.Behaviors;

public abstract class ClassicBehavior<T> : Behavior<T>
          where T : AvaloniaObject
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            Attached();
        }
    }
    protected virtual void Attached()
    { }

    protected virtual void Detached()
    { }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject is not null)
        {
            Detached();
        }
    }
}
