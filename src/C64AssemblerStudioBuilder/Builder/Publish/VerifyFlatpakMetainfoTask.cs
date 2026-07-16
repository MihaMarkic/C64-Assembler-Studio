using Cake.Common.IO;
using Cake.Core.IO;

namespace Build.Publish;

[TaskName("VerifyFlatpakMetainfo")]
public class VerifyFlatpakMetainfoTask : FrostingTask<BuildContext>
{
	public override void Run(BuildContext context)
	{
		var arguments = new ProcessArgumentBuilder()
			.Append("validate")
			.AppendQuoted(context.MakeAbsolute(context.FlatpakManifest).FullPath);
		var exitCodeWithArgument =
			context.StartProcess("appstreamcli", 
				new ProcessSettings
				{
					Arguments = arguments 
				});
		if (exitCodeWithArgument != 0)
		{
			throw new Exception($"appstreamcli failed with exit code {exitCodeWithArgument}");
		}
	}
}