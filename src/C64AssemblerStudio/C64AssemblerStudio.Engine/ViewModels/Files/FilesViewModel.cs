using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Common;
using C64AssemblerStudio.Engine.Messages;
using Microsoft.Extensions.Logging;
using Righthand.MessageBus;

namespace C64AssemblerStudio.Engine.ViewModels.Files;

public class FilesViewModel: ViewModel
{
    private readonly ILogger<FilesViewModel> _logger;
    private readonly ISubscription _openFileMessage;
    private readonly IServiceProvider _serviceProvider;
    public ObservableCollection<FileViewModel> Files { get; }
    public FileViewModel? Selected { get; set; }
    public RelayCommandWithParameter<FileViewModel> CloseFileCommand { get; }

    public FilesViewModel(ILogger<FilesViewModel> logger, IDispatcher dispatcher, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _openFileMessage = dispatcher.Subscribe<OpenFileMessage>(OpenFile);
        Files = new();
        CloseFileCommand = new RelayCommandWithParameter<FileViewModel>(CloseFile);
    }

    internal void CloseFile(FileViewModel file)
    {
        Files.Remove(file);
    }
    internal void OpenFile(OpenFileMessage message)
    {
        var viewModel = message.File.FileType switch
        {
            FileType.Assembler => _serviceProvider.CreateScopedSourceFileViewModel<AssemblerFileViewModel>(message.File),
            _ => null,
        };
        if (viewModel is not null)
        {
            _ = viewModel.LoadContentAsync();
            Files.Add(viewModel);
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