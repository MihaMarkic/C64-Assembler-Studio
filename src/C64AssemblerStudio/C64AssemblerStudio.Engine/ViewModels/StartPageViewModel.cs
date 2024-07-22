using C64AssemblerStudio.Core.Common;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StartPageViewModel: ScopedViewModel
{
    private readonly Globals _globals;
    public event EventHandler? LoadLastProjectRequest;
    public RelayCommand LoadLastProjectCommand { get; }

    public StartPageViewModel(Globals globals)
    {
        _globals = globals;
        LoadLastProjectCommand = new(LoadLastProject);
    }

    void LoadLastProject()
    {
        LoadLastProjectRequest?.Invoke(this, EventArgs.Empty);
    }
}