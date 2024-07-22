using Righthand.RetroDbgDataProvider.Models;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class CompilerErrorsOutputViewModel: OutputViewModel<CompilerError>
{
    public override string Header { get; } = "Error List";
}