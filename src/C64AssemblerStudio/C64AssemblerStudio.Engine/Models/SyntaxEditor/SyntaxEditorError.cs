namespace C64AssemblerStudio.Engine.Models.SyntaxEditor;

/// <summary>
/// Represents are syntax coloring entity.
/// </summary>
/// <param name="Kind">Key representing the error kind. Usually an <see cref="Enum"/> is used.</param>
/// <param name="Line">1 based line start.</param>
/// <param name="Column">Starting column in line.</param>
/// <param name="Length">Error length.</param>
/// <param name="Message">Associated specific message.</param>
public record SyntaxEditorError(SyntaxEditorErrorKind Kind, int Line, int Column, int Length, string? Message = null);

public enum SyntaxEditorErrorKind
{
    Generic,
    InvalidMemspace,
    InvalidRegister,
    InvalidLabel,
    InvalidBank,
}