using System.Text.Json.Serialization;
using C64AssemblerStudio.Core;

namespace C64AssemblerStudio.Engine.Models.Projects;

[JsonDerivedType(typeof(KickAssProject), typeDiscriminator: "kick-ass")]
public abstract class Project: NotifiableObject
{
    /// <summary>
    /// Project's caption
    /// </summary>
    public string? Caption { get; set; }
    /// <summary>
    /// When enabled, the application will be started from first available address
    /// </summary>
    public DebugAutoStartMode AutoStartMode { get; set; } = DebugAutoStartMode.Vice;
    public string? StopAtLabel { get; set; }
}