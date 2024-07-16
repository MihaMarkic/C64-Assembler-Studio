using System.ComponentModel.DataAnnotations;

namespace C64AssemblerStudio.Engine.Common;

public enum FileType
{
    [Display(Description = "Project file")]
    Project,
    [Display(Description = "Assembler file")]
    Assembler,
    Other
}