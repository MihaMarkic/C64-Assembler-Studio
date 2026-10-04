using C64AssemblerStudio.Core;
using C64AssemblerStudio.Engine.ViewModels.Tools;

namespace C64AssemblerStudio.Engine.ViewModels;

public class ProfilerViewModel: NotifiableObject, IToolView
{
	public string Header => "Profiler";
}