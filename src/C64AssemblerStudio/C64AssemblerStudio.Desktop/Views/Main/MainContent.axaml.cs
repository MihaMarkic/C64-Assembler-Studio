using Avalonia.Controls;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Dock.Model.Avalonia.Controls;

namespace C64AssemblerStudio.Desktop.Views.Main;

public partial class MainContent : UserControl<MainViewModel>
{
    public MainContent()
    {
        InitializeComponent();
    }
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.FocusToolView = null;
        }
        base.OnDataContextChanged(e);
        if (ViewModel is not null)
        {
            ViewModel.FocusToolView = FocusToolView;
        }
    }

    /// <summary>
    /// Focuses tool view based on <paramref name="toolView"/> type name where ViewModel is trimmed
    /// and Tool.Id property.
    /// </summary>
    /// <param name="toolView"></param>
    private void FocusToolView(IToolView toolView)
    {
        const string postfix = "ViewModel";
        string name = toolView.GetType().Name;
        if (name.Length <= postfix.Length)
        {
            return;
        }

        var id = name.AsSpan()[..^postfix.Length];
        ImmutableArray<ToolDock> allPanes = [BottomPane, PropertiesPane, LeftPane];
        foreach (var toolDock in allPanes)
        {
            if (toolDock.VisibleDockables is not null)
            {
                foreach (var t in toolDock.VisibleDockables.OfType<Tool>())
                {
                    if (id.Equals(t.Id, StringComparison.Ordinal))
                    {
                        toolDock.ActiveDockable = t;
                        return;
                    }
                }
            }
        }
    }
}