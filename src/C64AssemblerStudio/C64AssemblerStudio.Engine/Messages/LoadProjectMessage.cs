namespace C64AssemblerStudio.Engine.Messages;

/// <summary>
/// Messages that project loading is required.
/// </summary>
/// <param name="Path"></param>
public record LoadProjectMessage(string Path);