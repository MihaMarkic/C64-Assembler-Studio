using System;
using System.Threading.Tasks;

namespace C64AssemblerStudio.Core.Common;

public class RelayCommandAsync<T> : RelayCommandCore, ICommandEx
{
    private readonly Func<T?, bool>? _canExecute;
    private readonly Func<T?, Task> _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommandAsync(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public virtual bool CanExecute(object? parameter)
    {
        if (_canExecute is null)
        {
            return true;
        }
        return _canExecute((T?)parameter);
    }

    public virtual async void Execute(object? parameter)
    {
        try
        {
            await _execute((T)(parameter ?? throw new ArgumentNullException(nameof(parameter))));
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}