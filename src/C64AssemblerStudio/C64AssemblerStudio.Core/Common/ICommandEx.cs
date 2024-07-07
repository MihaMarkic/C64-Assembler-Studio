using System.Windows.Input;

namespace C64AssemblerStudio.Core.Common;

public interface ICommandEx: ICommand
{
    void RaiseCanExecuteChanged();
}
