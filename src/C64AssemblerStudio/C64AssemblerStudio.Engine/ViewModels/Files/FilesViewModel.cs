using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

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
    public ObservableCollection<FileViewModel> Files { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public FileViewModel? Selected { get; set; }
    public RelayCommandWithParameterAsync<FileViewModel> CloseFileCommand { get; }
    public RelayCommandAsync SaveAllCommand { get; }
    private DbgData? _debugData;

    public FilesViewModel(ILogger<FilesViewModel> logger, IDispatcher dispatcher, IServiceProvider serviceProvider,
        IServiceScopeFactory serviceScopeFactory,
        IVice vice, Globals globals, ProjectExplorerViewModel projectExplorer)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
        _serviceScopeFactory = serviceScopeFactory;
        _vice = vice;
        _globals = globals;
        _projectExplorer = projectExplorer;
        _openFileMessage = dispatcher.Subscribe<OpenFileMessage>(OpenFile);
        
        Files = new();
        CloseFileCommand = new RelayCommandWithParameterAsync<FileViewModel>(CloseFileAsync);
        SaveAllCommand = new RelayCommandAsync(SaveAllAsync);
        _vice.PropertyChanged += ViceOnPropertyChanged;
        _vice.RegistersUpdated += ViceOnRegistersUpdated;
    }

    public bool HasChanges => Files.Any(f => f.HasChanges);

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

    public void RemoveProjectFiles()
    {
        foreach (var f in Files.ToImmutableArray())
        {
            Files.Remove(f);
        }
    }

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
        foreach (var projectFile in Files.OfType<ProjectFileViewModel>())
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
                var allFiles = Files.Where(f => f.HasChanges)
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
        var files = Files.OfType<ProjectFileViewModel>().Select(f => f.File).ToImmutableArray();
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
                break;
            case SaveFilesDialogResultCode.DoNotSave:
                return true;
            default:
                return false;
        }
    }

    internal async Task CloseFileAsync(FileViewModel file)
    {
        if (file.HasChanges && file is ProjectFileViewModel projectFile)
        {
            var resultCode = await ShowDialogForClosingFiles([projectFile.File], CancellationToken.None);
            switch (resultCode)
            {
                case SaveFilesDialogResultCode.Save:
                    try
                    {
                        await file.SaveContentAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save {File}", projectFile.File.Name);
                    }
                    break;
                case SaveFilesDialogResultCode.DoNotSave:
                    break;
                default:
                    return;
            }
        }

        Files.Remove(file);
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

    ProjectFileViewModel? FindOpenFile(ProjectFile file)
    {
        return Files.OfType<ProjectFileViewModel>().FirstOrDefault(vm => vm.File.IsSame(file));
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
            if (message is { MoveCaret: true, Line: not null, Column: not null })
            {
                viewModel.MoveCaret(message.Line.Value, message.Column.Value);
            }
        }
        else
        {
            viewModel = message.File.FileType switch
            {
                FileType.Assembler =>
                    _serviceProvider.CreateScopedSourceFileViewModel<AssemblerFileViewModel>(message.File),
                _ => null,
            };
            if (viewModel is not null)
            {
                try
                {
                    _ = viewModel.LoadContentAsync();
                    Files.Add(viewModel);
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