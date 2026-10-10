using AutoFixture;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Abstract;
using C64AssemblerStudio.Engine.ViewModels.Projects;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.MessageBus;
using Righthand.RetroDbgDataProvider.Models;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.ViewModels.Projects;

[TestFixture]
public class ProjectViewModelTest: BaseTest<MockProjectViewModel>
{
	public class GetAddressFromSysCommand : ProjectViewModelTest
	{
		[TestCase("$32,$30,$36,$32,$00,$00,$00", ExpectedResult = (ushort)0x080e)]
		public ushort GiveSampleCase_ParsesAddressCorrectly(string data)
		{
			var bytes = data.ToBytes();
			
			var actual = MockProjectViewModel.GetAddressFromSysCommand(bytes);

			return actual;
		}
	}

	public class GetStartingAddressAsync : ProjectViewModelTest
	{
		[TestCase("$01,$08,$0c,$08,$b5,$07,$9e,$20,$32,$30,$36,$32,$00,$00,$00", ExpectedResult = (ushort)0x080e)]
		public async ValueTask<ushort?> GiveSampleCase_ParsesAddressCorrectly(string data)
		{
			var bytes = data.ToBytes();
			using var ms = new MemoryStream([..bytes]);

			var fileService = Fixture.Freeze<IFileService>();
			fileService.OpenRead("").ReturnsForAnyArgs(ms);
			fileService.FileExists("some_file.prg").Returns(true);
			
			var actual = await Target.GetStartingAddressAsync("some_file.prg", CancellationToken.None);

			return actual;
		}
	}
}

// ReSharper disable once ClassNeverInstantiated.Global
public class MockProjectViewModel : ProjectViewModel<Project, ParsedSourceFile>
{
	public MockProjectViewModel(ILogger<MockProjectViewModel> logger, ISettingsManager settingsManager, 
		ISystemDialogs systemDialogs, IDispatcher dispatcher, IFileService fileService, ISourceCodeParser<ParsedSourceFile> sourceCodeParser) 
		: base(logger, settingsManager, systemDialogs, dispatcher,  fileService, sourceCodeParser)
	{
	}

	public override Task LoadDebugDataAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}