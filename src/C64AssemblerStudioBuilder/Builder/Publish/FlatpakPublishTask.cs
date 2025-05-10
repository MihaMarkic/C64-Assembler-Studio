using Cake.Common.Diagnostics;
using Cake.Flatpak;
using Cake.Frosting;

namespace Build.Publish;

[TaskName("FlatpakPublish")]
[IsDependentOn(typeof(SelfContainedPublishTask))]
public class FlatpakPublishTask: FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) =>
        context is { BuildType: BuildType.Flatpak, Architecture: TargetArchitecture.LinuxX64 };

    public override void Run(BuildContext context)
    {
        var buildDir = context.PublishRootDirectory.Combine("flatpak");
        var manifest =
            context.SolutionDirectory.CombineWithFilePath("Configurations/com.rthand.C64AssemblerStudio.json");
        context.Information($"Flatpak building to {buildDir} with manifest {manifest}");
        //context.CleanDirectory(buildDir);
        context.FlatpakBuilder(buildDir, manifest, new FlatpakBuilderSettings { ForceClean = true });
    }
}