using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Messages;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider;
using Righthand.RetroDbgDataProvider.Models.Parsing;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class ErrorsOutputViewModel : OutputViewModel<FileCompilerError>
{
    private readonly IDispatcher _dispatcher;
    private readonly ProjectExplorerViewModel _projectExplorer;
    public event EventHandler? CompilerErrorsAdded;
    public RelayCommandWithParameter<FileCompilerError> JumpToCommand { get; }
    public override string Header { get; } = "Error List";

    public ErrorsOutputViewModel(IDispatcher dispatcher,
        ProjectExplorerViewModel projectExplorer)
    {
        _dispatcher = dispatcher;
        _projectExplorer = projectExplorer;
        JumpToCommand = new RelayCommandWithParameter<FileCompilerError>(JumpTo);
    }

    private void RaiseCompilerErrorsAdded(EventArgs e) => CompilerErrorsAdded?.Invoke(this, e);

    public void AddCompilerErrors(IEnumerable<FileCompilerError> lines)
    {
        AddLines(lines);
        RaiseCompilerErrorsAdded(EventArgs.Empty);
    }

    void JumpTo(FileCompilerError line)
    {
        //var file = _projectExplorer.GetProjectFileFromFullPath(line.Path);
        if (line.File is not null)
        {
            var message = new OpenFileMessage(line.File, line.Error.Range.Start ?? 0, line.Error.Line, MoveCaret: true);
            _dispatcher.Dispatch(message);
        }
    }

    public void AddParserErrorsForFile(string path, IEnumerable<FileCompilerError> errors)
    {
           ClearErrorsForFile(path);
           AddLines(errors);
    }

    private void ClearErrorsForFile(string path)
    {
        for (int i = Lines.Count - 1; i >= 0; i--)
        {
            if (path.Equals(Lines[i].Path, OsDependent.FileStringComparison))
            {
                Lines.RemoveAt(i);
            }
        }
    }
}

public record FileCompilerError(ProjectFile? File, SyntaxError Error)
{
    public string? Path => File?.AbsolutePath;
}