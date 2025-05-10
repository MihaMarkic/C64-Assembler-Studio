using Cake.Flatpak;
using Cake.Frosting;

namespace Build.Test;

[TaskName("TestFlatpak")]
public class TestFlatpakTask: FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var buildDir = context.PublishRootDirectory.Combine("flatpak");
        var manifest =
            context.SolutionDirectory.CombineWithFilePath("Configurations/com.rthand.C64AssemblerStudio.json");
        context.FlatpakBuilderRun(buildDir, manifest, "C64AssemblerStudio");
    }
}