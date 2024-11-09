using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Models.Program;

namespace C64AssemblerStudio.Engine.Models.Files;

public record SourceFileLocation(SourceFile SourceFile, MultiLineTextRange FileLocation);

public record SingleLineFileLocation() : SourceFileLocation(new SourceFile(
    new SourceFilePath("dir/somefile.asm", IsRelative: true), FrozenDictionary<string, Label>.Empty,
    ImmutableArray<BlockItem>.Empty), new DesignSingleLineMultiLineTextRange());