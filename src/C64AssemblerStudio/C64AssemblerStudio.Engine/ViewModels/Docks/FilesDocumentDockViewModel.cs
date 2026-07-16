using System.ComponentModel;
using System.Diagnostics;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Files;
using CommunityToolkit.Diagnostics;
using Dock.Model.Core.Events;
using Dock.Model.Mvvm.Controls;

namespace C64AssemblerStudio.Engine.ViewModels.Docks;

public class FilesDocumentDockViewModel: DocumentDock
{
    private readonly IGuiServices _guiServices;
    private readonly Dictionary<ProjectFileViewModel, FileDocumentViewModel> _map = new();
    public FilesDocumentDockViewModel(IGuiServices guiServices)
    {
        _guiServices = guiServices;
        IsCollapsable = false;
        CanCreateDocument = false;
        Id = "_FilesDocumentDockViewModel";
    }
    public void AddDocument(ProjectFileViewModel viewModel)
    {
        if (Factory is null)
        {
            throw new Exception($"{nameof(Factory)} is null");
        }
        var document = new FileDocumentViewModel(_guiServices, viewModel);
        Factory.AddDockable(this, document);
        Factory.SetActiveDockable(document);
        Factory.SetFocusedDockable(this, document);
        _map.Add(viewModel, document);
    }

    public void SetFocused(ProjectFileViewModel viewModel)
    {
        if (_map.TryGetValue(viewModel, out var document))
        {
	        Guard.IsNotNull(Factory);
            Factory.SetActiveDockable(document);
            Factory.SetFocusedDockable(this, document);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Factory):
                // TODO perhaps handle Factory being set to null, just in case
                if (Factory is not null)
                {
                    Factory.DockableClosed += FactoryOnDockableClosed; 
                }
                break;
        }
        base.OnPropertyChanged(e);
    }

    void FactoryOnDockableClosed(object? sender, DockableClosedEventArgs e)
    {
        if (e.Dockable is FileDocumentViewModel fileDocumentViewModel)
        {
            _map.Remove(fileDocumentViewModel.Context);
        }
    }

    internal void RemoveAllDocuments()
    {
        foreach (var doc in _map.Values.ToImmutableArray())
        {
            Factory?.RemoveDockable(doc, false);
        }
        _map.Clear();
    }

    internal IEnumerable<ProjectFileViewModel> Files
    {
        get
        {
            foreach (var k in _map.Keys)
            {
                yield return k;
            }
        }
    }
    
    public void CloseFile(ProjectFileViewModel viewModel)
    {
        var doc = _map[viewModel];
        if (Close.CanExecute(doc))
        {
            Close.Execute(doc);
        }
    }
}