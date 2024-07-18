using System;

namespace C64AssemblerStudio.Core.Common;

public class RelayCommand<T> : ICommandEx
{
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<T?> _execute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
        if (canExecute is not null)
        {
            _canExecute = canExecute;
        }
    }

    public virtual bool CanExecute(object? parameter)
    {
        if (_canExecute is null)
        {
            return true;
        }
        return _canExecute((T?)parameter);
    }

    public virtual void Execute(object? parameter)
    {
        _execute((T?)parameter);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}