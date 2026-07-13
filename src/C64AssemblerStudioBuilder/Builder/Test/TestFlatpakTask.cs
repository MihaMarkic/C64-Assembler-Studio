using Cake.Flatpak;
using Cake.Frosting;

namespace Build.Test;

[TaskName("TestFlatpak")]
public class TestFlatpakTask: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var buildDir = context.PublishRootDirectory.Combine("flatpak");
        context.FlatpakBuilderRun(buildDir, context.FlatpakConfiguration, "C64AssemblerStudio");
    }
}