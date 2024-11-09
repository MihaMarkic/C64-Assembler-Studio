using C64AssemblerStudio.Engine.Common;

namespace C64AssemblerStudio.Engine.Services.Implementation;

public static class ProjectFileClassificator
{
    public static FileType Classify(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        var fileType = Path.GetExtension(filePath) switch
        {
            ".cas" => FileType.Project,
            ".asm" => FileType.Assembler,
            _ => FileType.Other,
        };
        return fileType;
    }
}