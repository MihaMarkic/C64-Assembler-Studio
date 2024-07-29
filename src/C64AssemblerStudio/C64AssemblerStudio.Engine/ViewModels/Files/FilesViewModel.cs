using System.Collections.Frozen;
using System.ComponentModel;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using CommunityToolkit.Diagnostics;
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
    private readonly IVice _vice;
    private readonly Globals _globals;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public ObservableCollection<FileViewModel> Files { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public FileViewModel? Selected { get; set; }
    public RelayCommandWithParameter<FileViewModel> CloseFileCommand { get; }
    public RelayCommandAsync SaveAllCommand { get; }
    private DbgData? _debugData;

    public FilesViewModel(ILogger<FilesViewModel> logger, IDispatcher dispatcher, IServiceProvider serviceProvider,
        IVice vice, Globals globals, ProjectExplorerViewModel projectExplorer)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
        _vice = vice;
        _globals = globals;
        _projectExplorer = projectExplorer;
        _openFileMessage = dispatcher.Subscribe<OpenFileMessage>(OpenFile);
        
        Files = new();
        CloseFileCommand = new RelayCommandWithParameter<FileViewModel>(CloseFile);
        SaveAllCommand = new RelayCommandAsync(SaveAllAsync);
        _vice.PropertyChanged += ViceOnPropertyChanged;
        _vice.RegistersUpdated += ViceOnRegistersUpdated;
    }

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

    internal (ProjectFile File, FileLocation Location)? GetExecutionLocation(ushort address)
    {
        // don't care until there is no debug data
        if (_debugData is null)
        {
            return null;
        }
        var blockItems = _debugData.Segments.SelectMany(s => s.Blocks).SelectMany(b => b.Items);
        var fileLocation = blockItems.SingleOrDefault(bi => bi.Start <= address && bi.End >= address)?.FileLocation;
        if (fileLocation is not null)
        {
            var source = _debugData.Sources.Where(s => s.Origin == SourceOrigin.User)
                .SingleOrDefault(s => s.Index == fileLocation.SourceIndex);
            if (source is not null)
            {
                string? relativePath = source.GetRelativePath(_globals.Project.Directory.ValueOrThrow());
                if (relativePath is not null)
                {
                    var file = _projectExplorer.FindProjectFile(relativePath);
                    if (file is not null)
                    {
                        return (file, fileLocation);
                    }
                    else
                    {
                        _logger.LogWarning("Couldn't get {File} from project explorer", source.FullPath);
                    }
                }
                else
                {
                    _logger.LogWarning("Couldn't get relative path for {File}", source.FullPath);
                }
            }
            else
            {
                _logger.LogDebug("No line at address {Address:X4} within {SourceIndex} when execution paused", address,
                    fileLocation.SourceIndex);
            }
        }
        else
        {
            _logger.LogDebug("No source at address {Address:X4} when execution paused", address);
        }

        return null;
    }
    
    internal void SetExecutionPaused(ushort address)
    {
        var result = GetExecutionLocation(address);

        if (result.HasValue)
        {
            var (file, fileLocation) = result.Value;
            var message = new OpenFileMessage(file, fileLocation.Col1, fileLocation.Line1 - 1);
            OpenFileCore(message, fileLocation);
        }
    }

    internal void UnsetExecutionPaused()
    {
        foreach (var projectFile in Files.OfType<ProjectFileViewModel>())
        {
            projectFile.ExecutionLineRange = null;
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

    internal void CloseFile(FileViewModel file)
    {
        Files.Remove(file);
    }

    ProjectFileViewModel? FindOpenFile(ProjectFile file)
    {
        return Files.OfType<ProjectFileViewModel>().FirstOrDefault(vm => vm.File.IsSame(file));
    }

    internal void OpenFile(OpenFileMessage message)
    {
        var pc = _vice.Registers.Current.PC;
        var result = pc.HasValue ? GetExecutionLocation(pc.Value): null;
        FileLocation? executionLocation = result?.Location;
        OpenFileCore(message, executionLocation);
    }
    internal ProjectFileViewModel? OpenFileCore(OpenFileMessage message, FileLocation? executionLocation)
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