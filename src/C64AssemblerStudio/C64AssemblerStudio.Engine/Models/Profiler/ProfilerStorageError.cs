namespace C64AssemblerStudio.Engine.Models.Profiler;

public record ProfilerStorageError(string Message, Exception? Exception);

public class ProfilerStorageErrorEventArgs : EventArgs
{
	public ProfilerStorageError Error { get; }

	public ProfilerStorageErrorEventArgs(ProfilerStorageError error)
	{
		Error = error;
	}
}