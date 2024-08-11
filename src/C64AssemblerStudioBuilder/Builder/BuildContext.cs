using System;
using Cake.Common;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build;
public class BuildContext : FrostingContext
{
    public TargetArchitecture Architecture { get; }
    public BuildType BuildType { get; }
    public DirectoryPath RootDirectory { get; }
    public DirectoryPath SolutionDirectory { get; }
    public FilePath Solution { get; }
    public DirectoryPath DesktopProjectDirectory { get; }
    public FilePath DesktopProject { get; }
    public DirectoryPath PublishRootDirectory { get; }
    public DirectoryPath PublishDirectory { get; }
    public DirectoryPath SolutionScriptsDirectory { get; }
    public BuildContext(ICakeContext context)
        : base(context)
    {
        context.Environment.WorkingDirectory = context.MakeAbsolute(context.Directory("../../.."));
        RootDirectory = context.Directory(".");
        SolutionDirectory = RootDirectory + context.Directory("src/C64AssemblerStudio");
        Solution = SolutionDirectory.CombineWithFilePath(context.File("C64AssemblerStudio.sln"));
        SolutionScriptsDirectory = SolutionDirectory + context.Directory("Scripts");
        DesktopProjectDirectory = SolutionDirectory + context.Directory("C64AssemblerStudio.Desktop");
        DesktopProject = DesktopProjectDirectory .CombineWithFilePath(context.File("C64AssemblerStudio.Desktop.csproj"));
        PublishRootDirectory = SolutionDirectory + context.Directory("publish");
        PublishDirectory = PublishRootDirectory + this.Directory(Architecture switch
        {
            TargetArchitecture.WinX64 => "win_x64",
            TargetArchitecture.LinuxX64 => "linux_x64",
            TargetArchitecture.OSXArm64 => "osx_arm64",
            _ => throw new Exception($"Unknown architecture {Architecture}")
        });
        Architecture = context.Argument("architecture", TargetArchitecture.WinX64);
        BuildType = context.Argument("buildType", BuildType.Scoop);
    }

    public string ArchitectureUnderscoreText => Architecture switch
    {
        TargetArchitecture.WinX64 => "win_x64",
        TargetArchitecture.LinuxX64 => "linux_x64",
        TargetArchitecture.OSXArm64 => "osx_arm64",
        _ => throw new Exception($"Unknown architecture {Architecture}")
    };
    public string TargetRuntime => Architecture switch
    {
        TargetArchitecture.WinX64 => "win-x64",
        TargetArchitecture.LinuxX64 => "linux-x64",
        TargetArchitecture.OSXArm64 => "osx-arm64",
        _ => throw new Exception($"Unknown architecture {Architecture}")
    };
}

public enum TargetArchitecture
{
    WinX64,
    LinuxX64,
    // ReSharper disable once InconsistentNaming
    OSXArm64
}

public enum BuildType
{
    Scoop,
}