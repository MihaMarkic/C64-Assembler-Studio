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
        && context.BuildType is BuildType.Archive or BuildType.Flatpak;

    public override void Run(BuildContext context)
    {
        context.CreateDirectory(context.PublishDirectory);
        var settings = new DotNetPublishSettings
        {
            Configuration = "Release",
            SelfContained = true,
            PublishSingleFile = true,
            Runtime = context.TargetRuntime,
            OutputDirectory = context.PublishDirectory,
            Verbosity = DotNetVerbosity.Detailed,
        };
        context.Information($"Publishing selfcontained to {context.PublishDirectory}");
        context.DeleteDirectory(context.PublishDirectory, new DeleteDirectorySettings{ Force = true, Recursive = true });
        context.Information("Publish directories deleted");
        context.DotNetPublish(context.DesktopProject.FullPath, settings);
    }
}