using System;

namespace C64AssemblerStudio.Core.Common;

public class RelayCommand : RelayCommandCore, ICommandEx
{
    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute) : this(execute, null)
    {
    }

    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        if (canExecute != null)
        {
            _canExecute = new Func<bool>(canExecute);
        }
    }

    public virtual bool CanExecute(object? parameter)
    {
        if (_canExecute == null)
        {
            return true;
        }
        return _canExecute();
    }

    public virtual void Execute(object? parameter)
    {
        try
        {
            _execute();
        }
        catch (Exception ex)
        {
            LogException(ex);
        }
    }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
