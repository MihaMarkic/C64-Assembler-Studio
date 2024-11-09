using System.Collections.Frozen;
using System.Text.Json.Serialization;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Extensions;

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
    /// <summary>
    /// Project #define symbols separated by semicolon.
    /// </summary>
    /// <remarks>They can be override in source code by #define and #undef.</remarks>
    public string SymbolsDefine { get; set; } = string.Empty;

    /// <summary>
    /// Returns <see cref="SymbolsDefine"/> as <see cref="FrozenSet{String}"/>.
    /// </summary>
    public FrozenSet<string> SymbolsDefineSet => SymbolsDefine.Split(';').Select(d => d.Trim())
        .Where(d => !string.IsNullOrWhiteSpace(d))
        .ToFrozenSet();
}