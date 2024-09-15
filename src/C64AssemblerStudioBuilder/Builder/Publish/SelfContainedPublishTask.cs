using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Frosting;

namespace Build.Publish;

[TaskName("SelfContainedPublish")]
public class SelfContainedPublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
        => context.Architecture != TargetArchitecture.Dependent
        && context.BuildType == BuildType.Archive;

    public override void Run(BuildContext context)
    {
        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            SelfContained = true,
            Runtime = context.TargetRuntime,
            OutputDirectory = context.PublishDirectory,
        };
        context.Information(settings.ToString());
        
        context.DeleteDirectory(context.PublishDirectory, new DeleteDirectorySettings{ Force = true, Recursive = true });
        context.DotNetPublish(context.DesktopProject.FullPath, settings);
    }
}