using Cake.Common.Diagnostics;
using Cake.Frosting;

namespace Build;

[TaskName("Info")]
public class InfoTask: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information($"Build type is {context.BuildType}");
        context.Information($"Architecture is {context.Architecture}");
        context.Information($"Root directory is {context.RootDirectory.MakeAbsolute(context.Environment)}");
        context.Information($"Solution directory is {context.SolutionDirectory}");
        context.Information($"Desktop project directory is {context.DesktopProjectDirectory}");
        context.Information($"Desktop project is {context.DesktopProject}");
        base.Run(context);
    }
}