using System.IO.Compression;
using System.Xml.Linq;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Frosting;

namespace Build.Publish;

[TaskName("Publish")]
[IsDependentOn(typeof(TestTask))]
[IsDependentOn(typeof(CompressPublishTask))]
public class PublishTask: FrostingTask
{
    
}

[TaskName("CompressPublish")]
[IsDependentOn(typeof(ScoopPublishTask))]
[IsDependentOn(typeof(SelfContainedPublishTask))]
[IsDependentOn(typeof(FrameworkDependentPublishTask))]
public class CompressPublishTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var csprojectXml = XDocument.Load(context.MakeAbsolute(context.DesktopProject).FullPath);
        var versionText = csprojectXml.Root!.Element("PropertyGroup")!.Element("Version")!.Value;
        var source = context.MakeAbsolute(context.PublishDirectory).FullPath;
        string version = versionText.Split('-')[0].Replace('.', '_');
        string archiveType = context.BuildType switch
        {
            BuildType.Archive => context.Architecture == TargetArchitecture.Dependent ? "" : "_selfcontained",
            BuildType.Scoop => "_scoop",
        };
        string fileName = $"C64AssemblerStudio{archiveType}_{version}_{context.ArchitectureUnderscoreText}.zip";
        context.Information($"Compressed file is {fileName}");
        var target = context.MakeAbsolute(context.PublishRootDirectory.CombineWithFilePath(context.File(fileName))).FullPath;

        if (context.FileExists(target))
        {
            context.DeleteFile(target);
        }
        ZipFile.CreateFromDirectory(source, target);
    }
}