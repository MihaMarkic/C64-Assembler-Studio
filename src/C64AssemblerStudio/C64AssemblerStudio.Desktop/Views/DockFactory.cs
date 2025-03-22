using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Breakpoints;
using C64AssemblerStudio.Engine.ViewModels.Docks;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Desktop.Views;

public class DockFactory: Factory, IDockFactory
{
    private readonly IServiceProvider _serviceProvider;
    public IRootDock? RootDock { get; private set; }
    private IDocumentDock? _documentDock;
    private readonly Globals _globals;
    private readonly ErrorsOutputViewModel _errorsOutputViewModel;
    private readonly ErrorMessagesViewModel _errorMessagesViewModel;
    private readonly BuildOutputViewModel _buildOutputViewModel;
    private readonly DebugOutputViewModel _debugOutputViewModel;
    private readonly RegistersViewModel _registersViewModel;
    private readonly BreakpointsViewModel _breakpointsViewModel;
    private readonly MemoryViewerViewModel _memoryViewerViewModel;
    private readonly CallStackViewModel _callStackViewModel;
    private readonly ProjectExplorerViewModel _projectExplorerViewModel;
    public override IDocumentDock CreateDocumentDock() => new CustomDocumentDock();

    public DockFactory(IServiceProvider serviceProvider, ErrorsOutputViewModel errorsOutputViewModel,
        ErrorMessagesViewModel errorMessagesViewModel, BuildOutputViewModel buildOutputViewModel,
        DebugOutputViewModel debugOutputViewModel, RegistersViewModel registersViewModel,
        BreakpointsViewModel breakpointsViewModel, MemoryViewerViewModel memoryViewerViewModel,
        CallStackViewModel callStackViewModel, ProjectExplorerViewModel projectExplorerViewModel, Globals globals)
    {
        _serviceProvider = serviceProvider;
        _errorsOutputViewModel = errorsOutputViewModel;
        _errorMessagesViewModel = errorMessagesViewModel;
        _buildOutputViewModel = buildOutputViewModel;
        _debugOutputViewModel = debugOutputViewModel;
        _registersViewModel = registersViewModel;
        _breakpointsViewModel = breakpointsViewModel;
        _memoryViewerViewModel = memoryViewerViewModel;
        _callStackViewModel = callStackViewModel;
        _projectExplorerViewModel = projectExplorerViewModel;
        _globals = globals;
    }
    public override IRootDock CreateLayout()
    {
        var rootDock = CreateRootDock();

        var projectExplorer = new Tool
            { Id = nameof(Navigation.ProjectExplorer), Title = "Project Explorer" };
        var errorMessages = new Tool
            { Id = nameof(Navigation.ErrorMessages), Title = "Error messages" };
        var errorsOutput = new Tool
            { Id = nameof(Navigation.ErrorsOutput), Title = "Errors Output" };
        var buildOutput = new Tool
            { Id = nameof(Navigation.BuildOutput), Title = "Build Output" };
        var debugOutput = new Tool 
            { Id = nameof(Navigation.DebugOutput), Title = "Debug Output" };
        var registers = new Tool
            { Id = nameof(Navigation.Registers), Title = "Registers" };
        var breakpoints = new Tool
            { Id = nameof(Navigation.Breakpoints), Title = "Breakpoints" };
        var memoryViewer = new Tool
            { Id = nameof(Navigation.MemoryViewer), Title = "Memory Viewer" };
        var callStack = new Tool
            { Id = nameof(Navigation.CallStack), Title = "Call Stack" };        
        var leftDock = new ProportionalDock
        {
            Proportion = 0.20,
            VisibleDockables = CreateList<IDockable>(
                new ToolDock
                {
                    ActiveDockable = projectExplorer,
                    VisibleDockables = CreateList<IDockable>(projectExplorer),
                    Alignment = Alignment.Left,
                })
        };

        var bottomDock = new ProportionalDock
        {
            Proportion = 0.25,
            VisibleDockables = CreateList<IDockable>(
                new ToolDock
                {
                    ActiveDockable = errorsOutput,
                    VisibleDockables = CreateList<IDockable>(errorMessages, buildOutput, debugOutput, errorsOutput,
                        registers, breakpoints, memoryViewer, callStack),
                    Alignment = Alignment.Bottom,
                })
        };

        var documentDock = new CustomDocumentDock
        {
            IsCollapsable = false,
            CanCreateDocument = true
        };
        rootDock.IsCollapsable = false;

        var upperLayout = new ProportionalDock
        {
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>
            (
                leftDock,
                new ProportionalDockSplitter(),
                documentDock,
                new ProportionalDockSplitter()
                // rightDock
            )
        };

        var mainLayout = new ProportionalDock
        {
            Proportion = 0.70,
            Title = "Main",
            Orientation = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(
                upperLayout,
                new ProportionalDockSplitter(),
                bottomDock
            )
        };
        
        var home = new HomeDockViewModel
        {
            Id = nameof(Navigation.Home),
            ActiveDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };

        var startPage = new StartPageDockViewModel
        {
            Id = nameof(Navigation.StartPage),
            Title = "Start Page",
        };
        
        rootDock.IsCollapsable = false;
        rootDock.ActiveDockable = startPage;
        rootDock.DefaultDockable = home;
        rootDock.VisibleDockables = CreateList<IDockable>(startPage, home);
        _documentDock = documentDock;
        RootDock = rootDock;
            
        return rootDock;
    }
    private StartPageViewModel CreateStartPage()
    {
        var result = _serviceProvider.CreateScopedContent<StartPageViewModel>();
        var mostRecent = _globals.Settings.RecentProjects.FirstOrDefault();
        result.HasRecentProjects = mostRecent is not null;
        result.FullPath = mostRecent;
        return result;
    }
    public override IDockWindow? CreateWindowFrom(IDockable dockable)
    {
        var window = base.CreateWindowFrom(dockable);

        if (window is not null)
        {
            // TODO update with consistent name
            window.Title = "C64 Assembler Studio";
        }
        return window;
    }

    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            [nameof(Navigation.ProjectExplorer)] = () => _projectExplorerViewModel,
            [nameof(Navigation.ErrorMessages)] = () => _errorMessagesViewModel,
            [nameof(Navigation.ErrorsOutput)] = () => _errorsOutputViewModel,
            [nameof(Navigation.BuildOutput)] = () => _buildOutputViewModel,
            [nameof(Navigation.DebugOutput)] = () => _debugOutputViewModel,
            [nameof(Navigation.Registers)] = () => _registersViewModel,
            [nameof(Navigation.Breakpoints)] = () => _breakpointsViewModel,
            [nameof(Navigation.MemoryViewer)] = () => _memoryViewerViewModel,
            [nameof(Navigation.CallStack)] = () => _callStackViewModel,
            [nameof(Navigation.StartPage)] = CreateStartPage,
        };
        
        DockableLocator = new Dictionary<string, Func<IDockable?>>()
        {
            ["Root"] = () => RootDock,
            ["Documents"] = () => _documentDock
        };

        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };
        base.InitLayout(layout);
    }
}