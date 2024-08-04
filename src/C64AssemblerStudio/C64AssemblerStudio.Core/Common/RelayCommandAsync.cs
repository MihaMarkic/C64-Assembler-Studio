using System;
using System.Threading.Tasks;

namespace C64AssemblerStudio.Core.Common;

public class RelayCommandAsync : RelayCommandCore, ICommandEx
{
    private readonly Func<bool>? _canExecute;
    private readonly Func<Task> _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommandAsync(Func<Task> execute, Func<bool>? canExecute = null)
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
        return _canExecute();
    }

    public virtual async void Execute(object? parameter)
    {
        try
        {
            await _execute();
        }
        catch (Exception ex)
        {
            LogException(ex);
        }  
    } 
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
