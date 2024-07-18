using C64AssemblerStudio.Engine.Models.Projects;

namespace C64AssemblerStudio.Engine.Messages;

public record OpenFileMessage(ProjectFile File, int? Column = default, int? Line = default, bool MoveCaret = false);
//public record OpenSourceLineNumberFileMessage(PdbFile File, int Line, int? Column = default, bool MoveCaret = false)
//    : OpenFileMessage(File, Column, MoveCaret);
//public record OpenSourceLineFileMessage(PdbFile File, PdbLine Line, PdbAssemblyLine? AssemblyLine, bool IsExecution,
//    int? Column = default, bool MoveCaret = false)
//    : OpenFileMessage(File, Column, MoveCaret);
//public record OpenAddressMessage(ushort Address);
