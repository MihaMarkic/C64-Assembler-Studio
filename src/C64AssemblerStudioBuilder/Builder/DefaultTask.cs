using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Build;

[TaskName("Default")]
[IsDependentOn(typeof(InfoTask))]
public class DefaultTask: FrostingTask
{
    public override void Run(ICakeContext context)
    {
        context.Log.Information("C64 Assembler Studio builder");
        context.Log.Information(@"Sample command: .\build.ps1 -Target ");
    }
}