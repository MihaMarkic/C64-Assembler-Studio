using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class CompilerErrorsOutputViewModel : OutputViewModel<CompilerError>
{
    private readonly IDispatcher _dispatcher;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public RelayCommandWithParameter<CompilerError> JumpToCommand { get; }
    public override string Header { get; } = "Error List";

    public CompilerErrorsOutputViewModel(IDispatcher dispatcher,
        ProjectExplorerViewModel projectExplorer)
    {
        _dispatcher = dispatcher;
        _projectExplorer = projectExplorer;
        JumpToCommand = new RelayCommandWithParameter<CompilerError>(JumpTo);
    }

    void JumpTo(CompilerError line)
    {
        var file = _projectExplorer.GetProjectFileFromFullPath(line.Path);
        if (file is not null)
        {
            var message = new OpenFileMessage(file, line.Column, line.Line, MoveCaret: true);
            _dispatcher.Dispatch(message);
        }
    }
}