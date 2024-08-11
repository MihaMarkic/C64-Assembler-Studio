using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Frosting;

namespace Build;

[TaskName("Test")]
public class TestTask: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetTestSettings
        {
            Configuration = "Release"
        };
        
        context.DotNetTest(context.Solution.FullPath, settings);
    }
}