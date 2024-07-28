namespace C64AssemblerStudio.Engine.Grammars;

public record SyntaxError(int Line, int Column, string Message);