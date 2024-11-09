using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Configuration;
using C64AssemblerStudio.Engine.Models.SystemDialogs;
using C64AssemblerStudio.Engine.Services.Abstract;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels;

public interface ILibrariesEditorViewModel
{
    ObservableCollection<Library> Libraries { get; }
    Library? Selected { get; set; }
    string? Name { get; set; }
    string? Path { get; set; }
    RelayCommand AddCommand { get; }
    RelayCommand UpdateCommand { get; }
    RelayCommand DeleteCommand { get; }
    RelayCommandAsync SelectDirectoryCommand { get; }
}
/// <summary>
/// Represents libraries part of settings.
/// </summary>
public class LibrariesEditorViewModel: ViewModel, ILibrariesEditorViewModel
{
    private readonly ILogger<LibrariesEditorViewModel> _logger;
    private readonly ISystemDialogs _systemDialogs;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly CommandsManager _commandsManager;
    public RelayCommand AddCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommandAsync SelectDirectoryCommand { get; }
    public Library? Selected { get; set; }
    public ObservableCollection<Library> Libraries { get; } = new();
    public string? Name { get; set; }
    private bool IsNameValid { get; set; }
    private bool IsNameUnique { get; set; }
    private bool IsUpdateNameUnique { get; set; }
    public string? Path { get; set; }
    public bool IsPathValid { get; private set; }
    public bool IsSelectedValid => Selected is not null;

    public LibrariesEditorViewModel(ILogger<LibrariesEditorViewModel> logger, ISystemDialogs systemDialogs)
    {
        _logger = logger;
        _systemDialogs = systemDialogs;
        var uiFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        _commandsManager = new CommandsManager(this, uiFactory);

        AddCommand = _commandsManager.CreateRelayCommand(Add, () => IsNameValid && IsPathValid && IsNameUnique);
        UpdateCommand = _commandsManager.CreateRelayCommand(Update,
            () => IsSelectedValid && IsNameValid && IsPathValid && IsUpdateNameUnique);
        DeleteCommand = _commandsManager.CreateRelayCommand(Delete, () => IsSelectedValid);
        SelectDirectoryCommand = _commandsManager.CreateRelayCommandAsync(SelectDirectory);
    }

    protected override void OnPropertyChanged(string name = default!)
    {
        switch (name)
        {
            case nameof(Name):
                IsNameValid = !string.IsNullOrWhiteSpace(name);
                IsNameUnique = !Libraries.Any(l => l.Name.Equals(Name, StringComparison.Ordinal));
                IsUpdateNameUnique = !Libraries.Where(l => l != Selected).Any(l => l.Name.Equals(Name, StringComparison.Ordinal));
                break;
            case nameof(Path):
                IsPathValid = !string.IsNullOrWhiteSpace(Path);
                if (IsPathValid && Path!.Length > 0)
                {
                    Name = System.IO.Path.GetFileName(Path).Transform(To.SentenceCase);
                }
                break;
            case nameof(Selected):
                if (Selected is not null)
                {
                    Name = Selected.Name;
                    Path = Selected.Path;
                }
                break;
        }
        base.OnPropertyChanged(name);
    }
    
    private async Task SelectDirectory()
    {
        var newDirectory =
            await _systemDialogs.OpenDirectoryAsync(new OpenDirectory(Path, "Libarary directory selection"));
        var path = newDirectory.SingleOrDefault();
        if (path is not null)
        {
            Path = path;
        }
    }

    public void Init(IEnumerable<Library> libraries)
    {
        foreach (var l in libraries)
        {
            Libraries.Add(l);
        }
    }

    private void Add()
    {
        if (IsNameValid && IsPathValid)
        {
            Libraries.Add(new Library { Name = Name.ValueOrThrow(), Path = Path.ValueOrThrow() });
        }
    }
    private void Update()
    {
        if (Selected is not null && IsNameValid && IsPathValid)
        {
            Selected.Name = Name.ValueOrThrow();
            Selected.Path = Path.ValueOrThrow();
        }
    }

    private void Delete()
    {
        if (Selected is not null)
        {
            Libraries.Remove(Selected);
        }
    }
    /// <summary>
    /// Verifies that names are unique.
    /// </summary>
    public bool VerifyLibraries()
    {
        var set = new HashSet<string>();
        foreach (var l in Libraries)
        {
            if (string.IsNullOrWhiteSpace(l.Name) || set.Contains(l.Name))
            {
                return false;
            }

            set.Add(l.Name);
        }

        return true;
    }
}