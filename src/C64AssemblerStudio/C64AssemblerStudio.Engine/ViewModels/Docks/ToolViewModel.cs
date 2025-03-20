using Dock.Model.Mvvm.Controls;

namespace C64AssemblerStudio.Engine.ViewModels.Docks;

public class ToolViewModel<T>: Tool
    where T: ViewModel
{
    public ToolViewModel(T viewModel): base()
    {
        base.Context = viewModel;
    }

    public new T Context => (T)base.Context;
}