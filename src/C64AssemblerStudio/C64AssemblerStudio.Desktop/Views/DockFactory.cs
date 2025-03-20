using C64AssemblerStudio.Engine.ViewModels.Docks;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop.Services.Implementation;

public class DockFactory: Factory
{
    private IRootDock? _rootDock;
    private IDocumentDock? _documentDock;
    private IServiceProvider _serviceProvider;
    
    public override IDocumentDock CreateDocumentDock() => new CustomDocumentDock();

    public DockFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public override IRootDock CreateLayout()
    {
        var rootDock = CreateRootDock();

        var leftDock = new ProportionalDock
        {
            VisibleDockables = CreateList<IDockable>(
                new ToolDock()
                )
        };
        var documentDock = new CustomDocumentDock
        {
            IsCollapsable = false,
            // ActiveDockable = document1,
            // VisibleDockables = CreateList<IDockable>(document1, document2, document3),
            CanCreateDocument = true
        };
        rootDock.IsCollapsable = false;
        // rootDock.ActiveDockable = dashboardView;
        // rootDock.DefaultDockable = homeView;
        // rootDock.VisibleDockables = CreateList<IDockable>(dashboardView, homeView);

        _documentDock = documentDock;
        _rootDock = rootDock;
            
        return rootDock;
    }
    public override IDockWindow? CreateWindowFrom(IDockable dockable)
    {
        var window = base.CreateWindowFrom(dockable);

        if (window is not null)
        {
            window.Title = "Dock Avalonia Demo";
        }
        return window;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["ErrorMessages"] = () => _serviceProvider.GetRequiredService<ErrorMessagesViewModel>(),
        };
        
        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => _rootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };
        base.InitLayout(layout);
    }
}