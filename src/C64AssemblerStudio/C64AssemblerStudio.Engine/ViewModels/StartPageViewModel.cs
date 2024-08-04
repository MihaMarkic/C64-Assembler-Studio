using C64AssemblerStudio.Core.Common;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StartPageViewModel: ScopedViewModel
{
    private readonly Globals _globals;
    public event EventHandler? LoadLastProjectRequest;
    public bool HasRecentProjects { get; set; }
    public RelayCommand LoadLastProjectCommand { get; }

    public StartPageViewModel(Globals globals)
    {
        _globals = globals;
        LoadLastProjectCommand = new(LoadLastProject, () => HasRecentProjects);
    }

    void LoadLastProject()
    {
        LoadLastProjectRequest?.Invoke(this, EventArgs.Empty);
    }
}