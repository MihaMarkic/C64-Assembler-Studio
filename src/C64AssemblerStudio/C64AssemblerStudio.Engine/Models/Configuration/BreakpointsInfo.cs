using System.Text.Json.Serialization;

namespace C64AssemblerStudio.Engine.Models.Configuration;

public record BreakpointsSettings(ImmutableArray<BreakpointInfo> Breakpoints)
{
    public static readonly BreakpointsSettings Empty = new(ImmutableArray<BreakpointInfo>.Empty);
}

public record BreakpointInfo(
    bool StopWhenHit,
    bool IsEnabled,
    BreakpointMode Mode,
    string? Condition,
    BreakpointInfoBind Bind);

[JsonDerivedType(typeof(BreakpointInfoLineBind), typeDiscriminator: "line")]
[JsonDerivedType(typeof(BreakpointInfoNoBind), typeDiscriminator: "unbound")]
public abstract record BreakpointInfoBind;

public record BreakpointInfoLineBind(string FilePath, int LineNumber) : BreakpointInfoBind;

public record BreakpointInfoNoBind(string StartAddress, string? EndAddress) : BreakpointInfoBind;

public static class BreakpointInfoBindingExtensions
{
    public static BreakpointInfoBind ConvertFromModel(this BreakpointBind bind)
    {
        return bind switch
        {
            BreakpointLineBind lineBind => lineBind.ConvertFromModel(),
            BreakpointNoBind noBind => noBind.ConvertFromModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(bind)),
        };
    }

    public static BreakpointInfoBind ConvertFromModel(this BreakpointLineBind bind)
        => new BreakpointInfoLineBind(bind.FilePath, bind.LineNumber);

    public static BreakpointInfoNoBind ConvertFromModel(this BreakpointNoBind bind)
        => new BreakpointInfoNoBind(bind.StartAddress, bind.EndAddress);
}