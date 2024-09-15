using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Frosting;

namespace Build.Publish;

[TaskName("FrameworkDependentPublish")]
public class FrameworkDependentPublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
        => context is { Architecture: TargetArchitecture.Dependent, BuildType: BuildType.Archive };

    public override void Run(BuildContext context)
    {
        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            SelfContained = false,
            OutputDirectory = context.PublishDirectory,
        };
        context.Information(settings.ToString());

        context.DeleteDirectory(context.PublishDirectory,
            new DeleteDirectorySettings { Force = true, Recursive = true });
        context.DotNetPublish(context.DesktopProject.FullPath, settings);
    }
}