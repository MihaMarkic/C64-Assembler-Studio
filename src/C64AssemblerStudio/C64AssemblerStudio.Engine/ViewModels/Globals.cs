using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.Models.Configuration;

namespace C64AssemblerStudio.Engine.ViewModels;

public class Globals(EmptyProjectViewModel emptyProject): NotifiableObject
{
    public const string AppName = "C64 Assembler Studio";
    /// <summary>
    /// Holds active project, when no project is defined it contains <see cref="EmptyProjectViewModel"/>.
    /// </summary>
    public ProjectViewModel Project { get; set; } = emptyProject;
    public Settings Settings { get; set; } = new Settings();
}
