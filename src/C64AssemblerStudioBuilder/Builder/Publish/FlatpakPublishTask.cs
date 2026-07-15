using Cake.Common.Diagnostics;
using Cake.Flatpak;

namespace Build.Publish;

[TaskName("FlatpakPublish")]
[IsDependentOn(typeof(VerifyFlatpakManifestTask))]
[IsDependentOn(typeof(SelfContainedPublishTask))]
public class FlatpakPublishTask: FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) =>
        context is { BuildType: BuildType.Flatpak, Architecture: TargetArchitecture.LinuxX64 };

    public override void Run(BuildContext context)
    {
        var buildDir = context.PublishRootDirectory.Combine("flatpak");
        context.Information($"Flatpak building to {buildDir} with manifest {context.FlatpakConfiguration}");
        //context.CleanDirectory(buildDir);
        context.FlatpakBuilder(buildDir, context.FlatpakConfiguration, new FlatpakBuilderSettings { ForceClean = true, User = true });
    }
}