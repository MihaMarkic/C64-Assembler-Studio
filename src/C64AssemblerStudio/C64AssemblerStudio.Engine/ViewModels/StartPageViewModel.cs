using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.ViewModels.Files;

namespace C64AssemblerStudio.Engine.ViewModels;

public class StartPageViewModel: ScopedViewModel
{
    public event EventHandler? LoadLastProjectRequest;
    public bool HasRecentProjects { get; set; }
    public string? FullPath { get; set; }
    public RelayCommand LoadLastProjectCommand { get; }

    // ReSharper disable once MemberCanBeProtected.Global
    public StartPageViewModel()
    { 
        LoadLastProjectCommand = new(LoadLastProject, () => HasRecentProjects);
    }

    void LoadLastProject()
    {
        LoadLastProjectRequest?.Invoke(this, EventArgs.Empty);
    }
}

public class DesignStartPageViewModel : StartPageViewModel
{
    public DesignStartPageViewModel()
    {
        HasRecentProjects = true;
        FullPath = @"D:\TestPath\Project\Dude.cas";
    }
}