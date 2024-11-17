using System.Collections.Frozen;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels.Projects;

public interface IProjectViewModel: IDisposable, IAsyncDisposable
{
    RelayCommandAsync CloseCommand { get; }
    string? Path { get; set; }
    string? Caption { get; set; }
    string SymbolsDefine { get; set; }
    string? Directory { get; }
    Project? Configuration { get; }
    string? FullPrgPath { get; }
    public string? BreakpointsSettingsPath { get; }
    Task LoadDebugDataAsync(CancellationToken ct = default);
}

public interface IProjectViewModel<out TParsedFileType>: IProjectViewModel
    where TParsedFileType : ParsedSourceFile
{
    /// <summary>
    /// Provides access to source code parser.
    /// </summary>
    ISourceCodeParser<TParsedFileType> SourceCodeParser { get; }
}