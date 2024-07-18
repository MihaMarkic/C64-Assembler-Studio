namespace C64AssemblerStudio.Core.Common;

/// <summary>
/// <see cref="ICommandEx"/> implementation that asserts parameter is always not null.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RelayCommandWithParameter<T> : ICommandEx
{
    private readonly Func<T, bool>? _canExecute;
    private readonly Action<T> _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommandWithParameter(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
        if (canExecute is not null)
        {
            _canExecute = canExecute;
        }
    }

    public virtual bool CanExecute(object? parameter)
    {
        if (parameter is null)
        {
            return false;
        }
        if (_canExecute is null)
        {
            return true;
        }
        return _canExecute((T)parameter);
    }

    public virtual void Execute(object? parameter)
    {
        _execute((T)parameter.ValueOrThrow());
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}