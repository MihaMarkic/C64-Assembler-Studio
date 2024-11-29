using System.Collections.Frozen;
using C64AssemblerStudio.Engine.Models.Projects;

namespace C64AssemblerStudio.Engine.Messages;

/// <summary>
/// Opens file with additional hints.
/// </summary>
/// <param name="File"></param>
/// <param name="Column"></param>
/// <param name="Line"></param>
/// <param name="MoveCaret">When true, client should position caret</param>
/// <param name="DefineSymbols">File variation to open, where there are more define symbols sets</param>
public record OpenFileMessage(ProjectFile File, int? Column = default, int? Line = default, bool MoveCaret = false,
    FrozenSet<string>? DefineSymbols = null);
