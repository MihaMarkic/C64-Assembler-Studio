using Righthand.RetroDbgDataProvider.KickAssembler.Models;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.Models.Projects;

/// <summary>
/// Represents a byte dump line augmented with file location
/// </summary>
/// <param name="AssemblyLine"></param>
/// <param name="SourceFile"></param>
/// <param name="FileLocation"></param>
public record ByteDumpLine(AssemblyLine AssemblyLine, SourceFile SourceFile, MultiLineTextRange FileLocation);