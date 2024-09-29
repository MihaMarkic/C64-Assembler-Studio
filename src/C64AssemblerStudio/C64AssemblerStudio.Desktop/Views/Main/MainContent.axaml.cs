using System.Collections.Specialized;
using System.ComponentModel;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Files;
using C64AssemblerStudio.Engine.ViewModels.Tools;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Avalonia.Core;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Core.Events;

namespace C64AssemblerStudio.Desktop.Views.Main;

public partial class MainContent : UserControl<MainViewModel>
{
    public MainContent()
    {
        InitializeComponent();
        Dock.Factory!.DockableClosed += OnDockableClosed;
    }

    private void OnDockableClosed(object? sender, DockableClosedEventArgs e)
    {
        if (e.Dockable is Document { Context: FileViewModel fileViewModel })
        {
            ViewModel!.Files.Files.Remove(fileViewModel);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.FocusToolView = null;
            ViewModel.Files.Files.CollectionChanged -= FilesOnCollectionChanged;
            ViewModel.Files.PropertyChanged -= FilesOnPropertyChanged;
        }
        base.OnDataContextChanged(e);
        if (ViewModel is not null)
        {
            ViewModel.FocusToolView = FocusToolView;
            ViewModel.Files.Files.CollectionChanged += FilesOnCollectionChanged;
            ViewModel.Files.PropertyChanged += FilesOnPropertyChanged;
        }
    }

    private void FilesOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FilesViewModel.Selected):
                var selected = ViewModel!.Files.Selected;
                if (selected is not null)
                {
                    var document = GetFileDocument(selected);
                    if (document is not null)
                    {
                        Dock.Factory!.SetActiveDockable(document);
                    }
                }

                break;
        }
    }

    private void FilesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dock.Factory is null)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                if (e.NewItems?.Count > 0)
                {
                    foreach (var f in e.NewItems.OfType<FileViewModel>())
                    {
                        var doc = new Document
                        {
                            Title = f.Caption ?? "?",
                            Content = DocumentsPane.DocumentTemplate.ValueOrThrow().Content,
                            // DataContext is passed through Context property
                            Context = f,
                        };
                        Dock.Factory.AddDockable(DocumentsPane, doc);
                        Dock.Factory.SetActiveDockable(doc);
                    }
                }
                break;
            }
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems?.Count > 0)
                {
                    foreach (var f in e.OldItems.OfType<FileViewModel>())
                    {
                        var document = GetFileDocument(f);
                        if (document is not null)
                        {
                            Dock.Factory.RemoveDockable(document, false);
                        }
                    }
                }
                break;
        }
    }

    Document? GetFileDocument(FileViewModel fileViewModel)
    {
        var document = (Document?)Dock.Factory.ValueOrThrow()
            .Find(d => d is Document doc && ReferenceEquals(doc.Context, fileViewModel))
            .SingleOrDefault();
        return document;
    }

    /// <summary>
    /// Focuses tool view based on <paramref name="toolView"/> type name where ViewModel is trimmed
    /// and Tool.Id property.
    /// </summary>
    /// <param name="toolView"></param>
    private void FocusToolView(IToolView toolView)
    {
        Dock.Factory?.SetActiveDockable(BuildOutput);
        return;
        const string postfix = "ViewModel";
        string name = toolView.GetType().Name;
        if (name.Length <= postfix.Length)
        {
            return;
        }

        var id = name.AsSpan()[..^postfix.Length];
        foreach (var (owner, tool) in IterateAllDockables())
        {
            if (id.Equals(tool.Id, StringComparison.Ordinal))
            {
                return;
            }
        }
    }

    IEnumerable<(DockBase Owner, IDockable Tool)> IterateAllDockables()
    {
        ImmutableArray<DockBase> allPanes = [BottomPane, PropertiesPane, LeftPane];
        foreach (var toolDock in allPanes)
        {
            if (toolDock.VisibleDockables is not null)
            {
                foreach (var d in toolDock.VisibleDockables)
                {
                    yield return (toolDock, d);
                }
            }
        }

        ImmutableArray<IList<IDockable>?> rootAdditional =
            [Root.TopPinnedDockables, Root.LeftPinnedDockables, Root.RightPinnedDockables, Root.BottomPinnedDockables];
        foreach (var ds in rootAdditional)
        {
            if (ds is not null)
            {
                foreach (var d in ds)
                {
                    yield return (Root, d);
                }
            }
        }
    }
}