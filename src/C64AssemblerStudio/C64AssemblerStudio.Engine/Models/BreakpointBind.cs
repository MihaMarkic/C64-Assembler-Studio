using C64AssemblerStudio.Engine.Models.Projects;
using Righthand.RetroDbgDataProvider.KickAssembler.Models;

namespace C64AssemblerStudio.Engine.Models;
public abstract record BreakpointBind;
/// <summary>
/// 
/// </summary>
/// <param name="FilePath">Relative path to file</param>
/// <param name="File"></param>
/// <param name="LineNumber"></param>
public record BreakpointLineBind(string FilePath, int LineNumber, ProjectFile? File) : BreakpointBind
{
    public static BreakpointLineBind Empty { get; } = new BreakpointLineBind("", 0, null);
    // public PdbPath? FileName => File.Path;
    /// <summary>
    /// Shows info.
    /// </summary>
    /// <returns></returns>
    /// <remarks>LineNumber is Editor adjusted (+1).</remarks>
    public override string ToString() => $"Line {LineNumber+1} File {FilePath}";
}
public record BreakpointNoBind(string StartAddress, string? EndAddress) : BreakpointBind
{
    public static BreakpointNoBind Empty { get; } = new BreakpointNoBind("", null);
    public override string ToString() => "Unbound";
}
