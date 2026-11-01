namespace C64AssemblerStudio.Engine.Models.Profiler;

public record ProfiledApplication(
	ProfiledMethod Entry,
	ImmutableArray<ProfiledMethod> InterruptMethods);

public record ProfiledMethod(
	ushort EntryAddress,
	ImmutableArray<ProfiledInstruction> Instructions,
	ImmutableArray<ProfiledMethodCall> MethodCalls);

public record ProfiledInstruction(ushort InstructionAddress, int Calls, long TotalTime);

public record ProfiledMethodCall(ushort InstructionAddress, int Calls, long TotalTime, ProfiledMethod Method)
	: ProfiledInstruction(InstructionAddress, Calls, TotalTime);