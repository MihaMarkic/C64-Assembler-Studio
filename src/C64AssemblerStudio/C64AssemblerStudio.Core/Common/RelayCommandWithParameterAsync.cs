namespace C64AssemblerStudio.Core.Common;

/// <summary>
/// <see cref="ICommandEx"/> async implementation that asserts parameter is always not null.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RelayCommandWithParameterAsync<T> : RelayCommandCore, ICommandEx
{
    private readonly Func<T, bool>? _canExecute;
    private readonly Func<T, Task> _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommandWithParameterAsync(Func<T, Task> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
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

    public virtual async void Execute(object? parameter)
    {
        try
        {
            if (parameter is null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            await _execute((T)parameter);
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}