using Cake.Common.IO;
using Cake.Core.IO;

namespace Build.Publish;

[TaskName("VerifyFlatpakDesktop")]
public class VerifyFlatpakDesktopTask : FrostingTask<BuildContext>
{
	public override void Run(BuildContext context)
	{
		var arguments = new ProcessArgumentBuilder()
			.AppendQuoted(context.MakeAbsolute(context.FlatpakDesktop).FullPath);
		var exitCodeWithArgument =
			context.StartProcess("desktop-file-validate", 
				new ProcessSettings
				{
					Arguments = arguments 
				});
		if (exitCodeWithArgument != 0)
		{
			throw new Exception($"desktop-file-validate failed with exit code {exitCodeWithArgument}");
		}
		context.Information(".desktop file is valid");
	}
}