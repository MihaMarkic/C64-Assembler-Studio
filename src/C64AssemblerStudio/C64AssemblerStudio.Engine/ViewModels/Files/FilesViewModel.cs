using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class FilesViewModel: ViewModel
{
    private readonly ILogger<FilesViewModel> _logger;
    private readonly IDispatcher _dispatcher;
    private readonly ISubscription _openFileMessage;
    private readonly IServiceProvider _serviceProvider;
    public ObservableCollection<FileViewModel> Files { get; }
    public BusyIndicator BusyIndicator { get; } = new();
    public FileViewModel? Selected { get; set; }
    public RelayCommandWithParameter<FileViewModel> CloseFileCommand { get; }
    public RelayCommandAsync SaveAllCommand { get; }

    public FilesViewModel(ILogger<FilesViewModel> logger, IDispatcher dispatcher, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _dispatcher = dispatcher;
        _serviceProvider = serviceProvider;
        _openFileMessage = dispatcher.Subscribe<OpenFileMessage>(OpenFile);
        Files = new();
        CloseFileCommand = new RelayCommandWithParameter<FileViewModel>(CloseFile);
        SaveAllCommand = new RelayCommandAsync(SaveAll);
    }

    async Task SaveAll()
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
    internal async void OpenFile(OpenFileMessage message)
    {
        var existingViewModel = FindOpenFile(message.File);
        if (existingViewModel is not null)
        {
            Selected = existingViewModel;
            if (message is { MoveCaret: true, Line: not null, Column: not null })
            {
                existingViewModel.MoveCaret(message.Line.Value, message.Column.Value);
            }
            return;
        }
        var viewModel = message.File.FileType switch
        {
            FileType.Assembler => _serviceProvider.CreateScopedSourceFileViewModel<AssemblerFileViewModel>(message.File),
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _openFileMessage.Dispose();
        }
        base.Dispose(disposing);
    }
}