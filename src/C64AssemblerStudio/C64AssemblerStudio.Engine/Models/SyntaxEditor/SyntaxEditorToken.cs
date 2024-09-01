namespace C64AssemblerStudio.Engine.Models.SyntaxEditor;

/// <summary>
/// Represents are syntax coloring entity.
/// </summary>
/// <param name="TokenType">Key representing the token type. Usually an <see cref="Enum"/> is used.</param>
/// <param name="Line">1 based line start.</param>
/// <param name="Column">Column number within line.</param>
/// <param name="Length">Token length.</param>
public record SyntaxEditorToken(object TokenType, int Line, int Column, int Length);