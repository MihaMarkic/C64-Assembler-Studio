using AutoFixture;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Core.Services.Implementation;
using C64AssemblerStudio.Engine.Services.Implementation;
using C64AssemblerStudio.Engine.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using System.Collections.Frozen;
using Righthand.RetroDbgDataProvider.Services.Implementation;
using TestsBase;

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
            yield return new("Sub/T", "T*", [".prg"], "/project/Root".ToPath(), "",
                [],
                defaultSubFound,
                ["Sub/TestOne.prg".ToPath()]);

        }

        [TestCaseSource(nameof(Source))]
        public void GivenTestCase_ReturnsCorrectFiles(TestItem td)
        {
            IOSDependent osDependent = OperatingSystem.IsWindows() ? new WindowsDependent(): new LinuxDependent();
            var directoryService = Fixture.Freeze<IDirectoryService>();
            directoryService.GetFilteredFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<FrozenSet<string>>())
                .Returns([..td.FoundFiles.Select(f => Path.Combine(td.StartDirectory, f))]);
            var logger = Fixture.Create<ILogger<ProjectServices>>();
            var globals = Fixture.Create<Globals>();
            var fileServiceLogger = Fixture.Create<ILogger<FileService>>();
            var fileService = new FileService(fileServiceLogger, osDependent);
            var target = new ProjectServices(logger, globals, fileService, osDependent, directoryService);
            Dictionary<ProjectFileKey, FrozenSet<string>> builder = new();

            target.AddMatchingFiles(builder, ProjectFileOrigin.Project, td.RootFileName, td.SearchPattern, td.Extensions, td.StartDirectory, td.RelativeDirectory, td.ExcludedFilesSet);
            
            Assert.That(builder.Values.Single(), Is.EquivalentTo(td.Expected));
        }

        [TestCase("", "", "Project", "", "", "main.asm")]
        [TestCase("mai", "", "Project", "", "", "main.asm")]
        [TestCase("", "", "Project", "Sub", "", "Sub/sub.asm")]
        public void GivenTestCase_ReturnsCorrectFileNames(string normalizedRootFileName, string searchPattern, string rootDirectory, string relativeDirectory, 
            string excludedFileNames,
            string expectedText)
        {
            IOSDependent osDependent = OperatingSystem.IsWindows() ? new WindowsDependent() : new LinuxDependent();
            var logger = Fixture.Create<ILogger<ProjectServices>>();
            var globals = Fixture.Create<Globals>();
            var directoryServiceLogger = Fixture.Create<ILogger<DirectoryService>>();
            var directoryService = new DirectoryService(directoryServiceLogger, osDependent);
            var fileServiceLogger = Fixture.Create<ILogger<FileService>>();
            var fileService = new FileService(fileServiceLogger, osDependent);
            var target = new ProjectServices(logger, globals, fileService, osDependent, directoryService);
            Dictionary<ProjectFileKey, FrozenSet<string>> builder = new();
            FrozenSet<string> expected = [.. expectedText.Split(',').Select(p => osDependent.NormalizePath(p))];
            var testDirectory = TestContext.CurrentContext.WorkDirectory;
            var fileSystemRoot = Path.Combine(testDirectory, "TestFileSystems", "Default");
            foreach (var segment in rootDirectory.Split('/'))
            {
                fileSystemRoot = Path.Combine(fileSystemRoot, segment);
            }
            FrozenSet<string> excludedFiles = [.. excludedFileNames.Split(',')];

            target.AddMatchingFiles(builder, ProjectFileOrigin.Project, normalizedRootFileName, searchPattern, [".asm"], fileSystemRoot, relativeDirectory,
                excludedFiles);
            var actual = builder.Values.Single();

            Assert.That(actual, Is.EquivalentTo(expected).Using((IComparer<string>)osDependent.FileStringComparer));
        }
    }
    [TestFixture]
    public class AddMatchingDirectories: ProjectServicesTest
    {
        [Test]
        public void WhenBaseDirectoryDoesNotExist_BuilderIsEmpty()
        {
            var directoryService = Fixture.Freeze<IDirectoryService>();
            directoryService.GetDirectories(default!, default!).ReturnsForAnyArgs(ci =>
            {
                throw new Exception("Directory does not exist");
            });
            directoryService.Exists(default!).ReturnsForAnyArgs(false);

            Dictionary<ProjectFileKey, FrozenSet<string>> builder = [];

            Target.AddMatchingDirectories(builder, ProjectFileOrigin.Project, "", "", "", "");

            Assert.That(builder, Is.Empty);
        }
        [TestCase("", "Project", "", "", "Sub")]
        [TestCase("s*", "Project", "", "", "Sub")]
        [TestCase("", "Libraries/One", "", "", "Sub")]
        [TestCase("", "Libraries/One", "Sub", "", "InnerSubOne")]
        [TestCase("", "Project", "", "Sub", "Sub/Nested")]
        public void GivenTestCase_ReturnsCorrectFileNames(string searchPattern, string root, 
            string fileRelativeDirectory, string searchRelativeDirectory, string expectedText)
        {
            var logger = Fixture.Create<ILogger<ProjectServices>>();
            var globals = Fixture.Create<Globals>();
            var directoryServiceLogger = Fixture.Create<ILogger<DirectoryService>>();
            IOSDependent osDependent = OperatingSystem.IsWindows() ? new WindowsDependent(): new LinuxDependent();
            var directoryService = new DirectoryService(directoryServiceLogger, osDependent);
            var fileServiceLogger = Fixture.Create<ILogger<FileService>>();
            var fileService = new FileService(fileServiceLogger, osDependent);
            var target = new ProjectServices(logger, globals, fileService, osDependent, directoryService);
            Dictionary<ProjectFileKey, FrozenSet<string>> builder = new();
            FrozenSet<string> expected = [.. expectedText.Split(',').Select(p => osDependent.NormalizePath(p))];
            var testDirectory = TestContext.CurrentContext.WorkDirectory;
            var fileSystemRoot = Path.Combine(testDirectory, "TestFileSystems", "Default");
            foreach (var segment in root.Split('/'))
            {
                fileSystemRoot = Path.Combine(fileSystemRoot, segment);
            }

            target.AddMatchingDirectories(builder, ProjectFileOrigin.Project, 
                searchPattern, fileSystemRoot, fileRelativeDirectory, searchRelativeDirectory);
            var actual = builder.Values.Single();

            Assert.That(actual, Is.EquivalentTo(expected).Using((IComparer<string>)osDependent.FileStringComparer));
        }
    }
}