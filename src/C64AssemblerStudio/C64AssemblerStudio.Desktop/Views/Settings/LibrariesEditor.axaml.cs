using System.Collections.ObjectModel;
using Avalonia.Controls;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.ViewModels;

namespace C64AssemblerStudio.Desktop.Views.Settings;

public partial class LibrariesEditor : UserControl
{
    public LibrariesEditor()
    {
        InitializeComponent();
    }
}

public class DesignLibrariesEditor : ILibrariesEditorViewModel
{
    public DesignLibrariesEditor()
    {
        ImmutableArray<Library> libraries = [
            new() { Name = "First", Path = @"D:\Directory\Subdirectory\Library" },
            new() { Name = "Second", Path = @"D:\Directory\Library" }
        ];
        Libraries = new(libraries);
    }
    public ObservableCollection<Library> Libraries { get; }
    public Library? Selected { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public RelayCommand AddCommand => default!;
    public RelayCommand UpdateCommand => default!;
    public RelayCommand DeleteCommand => default!;
    public RelayCommandAsync SelectDirectoryCommand => default!;
}