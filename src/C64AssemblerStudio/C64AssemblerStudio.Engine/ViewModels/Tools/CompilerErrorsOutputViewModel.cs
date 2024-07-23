using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class CompilerErrorsOutputViewModel : OutputViewModel<FileCompilerError>
{
    private readonly IDispatcher _dispatcher;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public RelayCommandWithParameter<FileCompilerError> JumpToCommand { get; }
    public override string Header { get; } = "Error List";

    public CompilerErrorsOutputViewModel(IDispatcher dispatcher,
        ProjectExplorerViewModel projectExplorer)
    {
        _dispatcher = dispatcher;
        _projectExplorer = projectExplorer;
        JumpToCommand = new RelayCommandWithParameter<FileCompilerError>(JumpTo);
    }

    void JumpTo(FileCompilerError line)
    {
        //var file = _projectExplorer.GetProjectFileFromFullPath(line.Path);
        if (line.File is not null)
        {
            var message = new OpenFileMessage(line.File, line.Error.Column, line.Error.Line, MoveCaret: true);
            _dispatcher.Dispatch(message);
        }
    }
}

public record FileCompilerError(ProjectFile? File, CompilerError Error);