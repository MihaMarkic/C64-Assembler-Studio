using System;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.SignTool;
using Cake.Core.IO;
using Cake.Frosting;

namespace Build.Publish;

[TaskName("ScoopPublish")]
[IsDependentOn(typeof(SignWinX64Task))]
public class ScoopPublishTask : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.BuildType == BuildType.Scoop;
}


[TaskName("ScoopPublishWinX64")]
public class ScoopPublishWinX64Task : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) =>
        context.BuildType == BuildType.Scoop && context.Architecture == TargetArchitecture.WinX64;

    public override void Run(BuildContext context)
    {
        Publish.Scoop(context);
        var fileName = context.File("*_startmenu_shortcut.ps1");
        GlobPattern sourceGlob = context.MakeAbsolute(context.SolutionScriptsDirectory.CombineWithFilePath(fileName)).FullPath;

        context.CopyFiles(sourceGlob, context.PublishDirectory);
        context.Information($"Added script files");
    }
}

[TaskName("SignWin")]
[IsDependentOn(typeof(ScoopPublishWinX64Task))]
public class SignWinX64Task : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) =>
        context.BuildType == BuildType.Scoop && context.Architecture == TargetArchitecture.WinX64;

    public override void Run(BuildContext context)
    {
        var settings = new SignToolSignSettings
        {
            TimeStampUri = new Uri("http://timestamp.digicert.com"),
            CertThumbprint = "df28fe69c52288eeff676fec3f6167e40c0564f8",
            DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
        };
        
        var exeFile = context.PublishDirectory.CombineWithFilePath("C64AssemblerStudio.exe");
        context.Information($"File to sign is {exeFile}");
        try
        {
            context.Sign(exeFile, settings);
        }
        catch (Exception)
        {
            context.Error($"Signtool should be in %PATH%. To get its location one can use '$(get-command signtool).path' powershell command.");
            throw;
        }
    }
}

static class Publish
{
    internal static void Scoop(BuildContext context)
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