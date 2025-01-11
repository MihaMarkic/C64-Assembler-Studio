using System.Collections.Frozen;
using Antlr4.Runtime.Misc;
using AutoFixture;
using C64AssemblerStudio.Engine.Services.Implementation;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using TestsBase;
using IFileService = C64AssemblerStudio.Core.Services.Abstract.IFileService;

namespace C64AssemblerStudio.Engine.Test.Services.Implementation;

public class ProjectServicesTest: BaseTest<ProjectServices>
{
    [TestFixture]
    public class AddMatchingFiles : ProjectServicesTest
    {
        public record struct TestItem(
            string RootFileName, string SearchPattern, FrozenSet<string> Extensions,
            string StartDirectory, string RelativeDirectory, FrozenSet<string> ExcludedFilesSet, FrozenSet<string> FoundFiles, FrozenSet<string> Expected);

        
        public static IEnumerable<TestItem> Source()
        {
            FrozenSet<string> defaultFound = ["TestOne.prg", "TestTwo.prg", "TestOne.sid", "Extra.prg", "Sub/TestOne.prg".ToPath()];
            yield return new("Test", "Test*", [".prg"], "/project/Root".ToPath(), "", 
                [],
                defaultFound,
                ["TestOne.prg", "TestTwo.prg"]);
            yield return new("Test", "Test*", [".prg"], "/project/Root".ToPath(), "", 
                ["TestOne.prg"],
                defaultFound,
                ["TestTwo.prg"]);
            yield return new("", "*", [".prg"], "/project/Root".ToPath(), "", 
                [],
                defaultFound,
                ["TestOne.prg", "TestTwo.prg", "Extra.prg", "Sub/TestOne.prg".ToPath()]);
            FrozenSet<string> defaultSubFound = ["Sub/TestOne.prg".ToPath()];
            yield return new("Sub/T".ToPath(), "T*", [".prg"], "/project/Root".ToPath(), "Sub", 
                [],
                defaultSubFound,
                ["Sub/TestOne.prg".ToPath()]);
            
        }

        [TestCaseSource(nameof(Source))]
        public void GivenTestCase_ReturnsCorrectFiles(TestItem td)
        {
            Dictionary<ProjectFileKey, FrozenSet<string>> builder = new();
            var fileService = Fixture.Freeze<IFileService>();
            fileService.GetFilteredFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FrozenSet<string>>())
                .Returns([..td.FoundFiles.Select(f => Path.Combine(td.StartDirectory, f))]);
            
            Target.AddMatchingFiles(builder, ProjectFileOrigin.Project, td.RootFileName, td.SearchPattern, td.Extensions, td.StartDirectory, td.RelativeDirectory, td.ExcludedFilesSet);
            
            Assert.That(builder.Values.Single(), Is.EquivalentTo(td.Expected));
        }
    }
}