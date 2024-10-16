using System.ComponentModel;
using C64AssemblerStudio.Core.Common;
using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;

namespace C64AssemblerStudio.Engine.ViewModels.Projects;

public interface IProjectViewModel: IDisposable
{
    RelayCommand CloseCommand { get; }
    string? Path { get; set; }
    string? Directory { get; }
    Project? Configuration { get; }
    string? FullPrgPath { get; }
    public string? BreakpointsSettingsPath { get; }
    event PropertyChangedEventHandler? PropertyChanged;
    Task LoadDebugDataAsync(CancellationToken ct = default);
    /// <summary>
    /// Provides access to source code parser.
    /// </summary>
    ISourceCodeParser<ParsedSourceFile> SourceCodeParser { get; }
}