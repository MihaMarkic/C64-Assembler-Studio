using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Docks;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class FilesViewModel : ViewModel
{
    private readonly ILogger<FilesViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly ISubscription _openFileMessage;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IVice _vice;
    private readonly Globals _globals;
    private readonly ProjectExplorerViewModel _projectExplorer;
    private readonly StatusInfoViewModel _statusInfo;
    private readonly FilesDocumentDockViewModel _filesDocumentDockViewModel;
    public BusyIndicator BusyIndicator { get; } = new();
    public FileViewModel? Selected { get; set; }
    public RelayCommandWithParameter<ProjectFileViewModel> CloseFileCommand { get; }
    public RelayCommandAsync SaveAllCommand { get; }
    public int? FocusedFileCaretLine => Selected?.CaretLine; 
    public int? FocusedFileCaretColumn => Selected?.CaretColumn;

    private DbgData? _debugData;

    /// <summary>
    /// Current selected. Used for tracking previous when <see cref="Selected"/> changes.
    /// </summary>
    private FileViewModel? _selected;

    public FilesViewModel(ILogger<FilesViewModel> logger, IDispatcher dispatcher, IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory,
        IVice vice, Globals globals, ProjectExplorerViewModel projectExplorer, StatusInfoViewModel statusInfo,
        FilesDocumentDockViewModel filesDocumentDockViewModel)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
        _serviceScopeFactory = serviceScopeFactory;
        _vice = vice;
        _globals = globals;
        _projectExplorer = projectExplorer;
        _statusInfo = statusInfo;
        _filesDocumentDockViewModel = filesDocumentDockViewModel;
        _openFileMessage = dispatcher.Subscribe<OpenFileMessage>(OpenFile);
        
        CloseFileCommand = new RelayCommandWithParameter<ProjectFileViewModel>(CloseFile);
        SaveAllCommand = new RelayCommandAsync(SaveAllAsync);
        _vice.PropertyChanged += ViceOnPropertyChanged;
        _vice.RegistersUpdated += ViceOnRegistersUpdated;
    }

    public bool HasChanges => _filesDocumentDockViewModel.Files.Any(f => f.HasChanges);

    private void ViceOnRegistersUpdated(object? sender, RegistersEventArgs e)
    {
        var pc = _vice.Registers.Current.PC;
        if (pc.HasValue)
        {
            SetExecutionPaused(pc.Value);
        }
        else
        {
            _logger.LogDebug("Failed updating execution line based on null PC");
        }
    }

    protected override void OnPropertyChanged(string? name = default!)
    {
        switch (name)
        {
            case nameof(Selected):
                if (_selected is not null)
                {
                    _selected.PropertyChanged -= SelectedOnPropertyChanged;
                }
                if (Selected is not null)
                {
                    Selected.PropertyChanged += SelectedOnPropertyChanged;
                }

                _selected = Selected;
                break;
            case nameof(FocusedFileCaretLine):
                _statusInfo.EditorCaretLine = FocusedFileCaretLine;
                break;
            case nameof(FocusedFileCaretColumn):
                _statusInfo.EditorCaretColumn = FocusedFileCaretColumn;
                break;
        }
        base.OnPropertyChanged(name);
    }

    private void SelectedOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(FileViewModel.CaretLine):
                OnPropertyChanged(nameof(FocusedFileCaretLine));
                break;
            case nameof(FileViewModel.CaretColumn):
                OnPropertyChanged(nameof(FocusedFileCaretColumn));
                break;
        }
    }

    public void RemoveProjectFiles() => _filesDocumentDockViewModel.RemoveAllDocuments();

    private void ViceOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IVice.IsPaused):
                if (!_vice.IsPaused)
                {
                    UnsetExecutionPaused();
                }
                break;
            case nameof(IVice.IsDebugging):
                _debugData = _vice.IsDebugging
                    ? ((KickAssProjectViewModel)_globals.Project.ValueOrThrow()).DbgData.ValueOrThrow()
                    : null;
                // if (!_vice.IsDebugging)
                // {
                //     foreach (var f in Files.OfType<AssemblerFileViewModel>())
                //     {
                //         f.IsByteDumpVisible = false;
                //     }
                // }
                break;
        }
    }
    
    internal void SetExecutionPaused(ushort address)
    {
        var result = _projectExplorer.GetExecutionLocation(address);

        if (result.HasValue)
        {
            var (file, fileLocation) = result.Value;
            var message = new OpenFileMessage(file, fileLocation.Col1, fileLocation.Line1 - 1);
            OpenFileCore(message, fileLocation, address);
        }
    }

    internal void UnsetExecutionPaused()
    {
        foreach (var projectFile in _filesDocumentDockViewModel.Files)
        {
            projectFile.ExecutionLineRange = null;
            projectFile.ExecutionAddress = null;
        }
    }

    public async Task SaveAllAsync()
    {
        using (BusyIndicator.Increase())
        {
            try
            {
                var allFiles = _filesDocumentDockViewModel.Files.Where(f => f.HasChanges)
                    .Select(f => f.SaveContentAsync());
                await Task.WhenAll(allFiles);
            }
            catch (Exception ex)
            {
                _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, "Save all content", ex.Message));
                _logger.LogError(ex, "Failed saving all files");
            }
        }
    }
    internal async Task<bool> CloseAllFilesAsync(CancellationToken ct = default)
    {
        var files = _filesDocumentDockViewModel.Files.Select(f => f.File).ToImmutableArray();
        var resultCode = await ShowDialogForClosingFiles(files, ct);
        switch (resultCode)
        {
            case SaveFilesDialogResultCode.Save:
                try
                {
                    await SaveAllAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save all files");
                    throw;
                }
            case SaveFilesDialogResultCode.DoNotSave:
                return true;
            default:
                return false;
        }
    }
    internal async Task<SaveFilesDialogResultCode> ShowDialogForClosingFiles(ImmutableArray<ProjectFile> files, CancellationToken ct = default)
    {
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var detailViewModel = scope.ServiceProvider.CreateScopedContent<SaveFileDialogViewModel>();
            detailViewModel.UnsavedFiles = files;
            var dialog = new ShowModalDialogMessage<SaveFileDialogViewModel, SaveFilesDialogResult>(
                "Save files", DialogButton.Save | DialogButton.DoNotSave | DialogButton.Cancel, detailViewModel)
            {
                MinSize = new Size(300, 200),
                DesiredSize = new Size(500, 300),
            };
            _dispatcher.DispatchShowModalDialog(dialog);
            var result = await dialog.Result;
            return result.Code;
        }
    }
    internal void CloseFile(ProjectFileViewModel file)
    {
        // var canRemove = await file.HandlesCloseFileAsync();
        // if (canRemove)
        // {
        //     Files.Remove(file);
        // }
        _filesDocumentDockViewModel.CloseFile(file);
    }

    internal FrozenDictionary<string, InMemoryFileContent> CollectAllOpenContent()
    {
        var files =  _filesDocumentDockViewModel
            .Files.OfType<AssemblerFileViewModel>()
            .Where(f => f.HasChanges)
            .ToFrozenDictionary(f => f.File.AbsolutePath,
                f => new InMemoryFileContent(f.File.AbsolutePath, f.Content, f.LastChangeTime));
        return files;
    }

    ProjectFileViewModel? FindOpenFile(ProjectFile file)
    {
        return  _filesDocumentDockViewModel.Files.FirstOrDefault(vm => vm.File.IsSame(file));
    }

    internal void OpenFile(OpenFileMessage message)
    {
        var pc = _vice.Registers.Current.PC;
        var result = pc.HasValue ?  _projectExplorer.GetExecutionLocation(pc.Value): null;
        FileLocation? executionLocation = result?.Location;
        OpenFileCore(message, executionLocation, address: null);
    }
    internal ProjectFileViewModel? OpenFileCore(OpenFileMessage message, FileLocation? executionLocation,
        ushort? address)
    {
        var viewModel = FindOpenFile(message.File);
        
        if (viewModel is not null)
        {
            Selected = viewModel;
            if (viewModel is AssemblerFileViewModel assemblerFileViewModel)
            {
                assemblerFileViewModel.SelectedDefineSymbols = message.DefineSymbols;
                _filesDocumentDockViewModel.SetFocused(assemblerFileViewModel);
            }
            if (message is { MoveCaret: true, Line: not null, Column: not null })
            {
                viewModel.MoveCaret(message.Line.Value, message.Column.Value);
            }
        }
        else
        {
            viewModel = message.File.FileType switch
            {
                FileType.Assembler => _serviceProvider.CreateScopedAssemblerSourceFileViewModel(message.File,
                    new(message.DefineSymbols)),
                _ => (ProjectFileViewModel?)null
            };
            if (viewModel is AssemblerFileViewModel assemblerFileViewModel)
            {
                try
                {
                    _ = assemblerFileViewModel.LoadContentAsync();
                    _filesDocumentDockViewModel.AddDocument(assemblerFileViewModel);
                    Selected = viewModel;
                    if (message is { MoveCaret: true, Line: not null, Column: not null })
                    {
                        viewModel.MoveCaret(message.Line.Value, message.Column.Value);
                    }
                }
                catch (Exception ex)
                {
                    _dispatcher.Dispatch(new ErrorMessage(ErrorMessageLevel.Error, $"Loading {message.File.Name}",
                        ex.Message));
                }
            }
            else
            {
                _logger.LogError("Couldn't open {FileType}", message.File.FileType);
            }
        }
        
        if (viewModel is not null && executionLocation is not null)
        {
            viewModel.ExecutionLineRange = (executionLocation.Line1 - 1, executionLocation.Line2 - 1);
            viewModel.ExecutionAddress = address;
        }

        return viewModel;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vice.PropertyChanged -= ViceOnPropertyChanged;
            _vice.RegistersUpdated -= ViceOnRegistersUpdated;
            _openFileMessage.Dispose();
        }

        base.Dispose(disposing);
    }
}