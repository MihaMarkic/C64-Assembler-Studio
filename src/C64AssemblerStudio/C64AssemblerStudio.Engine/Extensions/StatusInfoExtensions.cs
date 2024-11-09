namespace C64AssemblerStudio.Engine.ViewModels;

public static class StatusInfoExtensions
{
   public static string BuildStatusToString(this BuildStatus source)
   {
      return source switch
      {
         BuildStatus.Building => "Building",
         BuildStatus.Failure => "Build Failure",
         BuildStatus.Idle => "Idle",
         BuildStatus.Success => "Build Success",
         _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
      };
   }

   public static string DebugStatusToString(this DebuggingStatus source)
   {
      return source switch
      {
         DebuggingStatus.Debugging => "Debugging",
         DebuggingStatus.Idle => "Idle",
         DebuggingStatus.Paused => "Paused",
         DebuggingStatus.WaitingForConnection => "Waiting For Connection",
         _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
      };
   }
}