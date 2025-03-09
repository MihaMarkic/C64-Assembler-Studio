using AutoFixture;
using C64AssemblerStudio.Core.Services.Abstract;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Implementation;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Services.Abstract;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.Services.Implementation;

public class ProjectFilesWatcherTest : BaseTest<ProjectFileWatcher>
{
    [TestFixture]
    public class FindMatchingDirectory : ProjectFilesWatcherTest
    {
        [Test]
        public void WhenRootDirectory_ReturnsRootDirectory()
        {
            var rootDirectory = new ProjectRoot(StringComparison.Ordinal)
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var logger = Fixture.Freeze<ILogger>();
            var osDependant = Fixture.Freeze<IOSDependent>();

            var actual = ProjectFileWatcher.FindMatchingDirectory(rootDirectory, "", logger, osDependant);

            Assert.That(actual, Is.SameAs(rootDirectory));
        }

        [Test]
        public void WhenDirectoryMatchesLibraries_AndThereIsNoRelativeLibrariesDirectory_ThrowsException()
        {
            var rootDirectory = new ProjectRoot(StringComparison.Ordinal)
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var library = new ProjectLibraries(StringComparison.Ordinal)
            {
                Name = "Libraries",
                Parent = null,
            };
            rootDirectory.Items.Add(library);
            var logger = Fixture.Freeze<ILogger>();
            var osDependant = Fixture.Freeze<IOSDependent>();

            Assert.Throws<Exception>(() =>
                ProjectFileWatcher.FindMatchingDirectory(rootDirectory, "Libraries", logger, osDependant));
        }

        [Test]
        public void
            WhenDirectoryMatchesLibraries_AndThereIsRelativeLibrariesDirectory_ReturnsRelativeLibrariesDirectory()
        {
            var rootDirectory = new ProjectRoot(StringComparison.Ordinal)
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var libraries = new ProjectLibraries(StringComparison.Ordinal)
            {
                Name = "Libraries",
                Parent = null,
            };
            rootDirectory.Items.Add(libraries);
            var relativeLibraries = new ProjectDirectory(StringComparison.Ordinal)
            {
                Name = "Libraries",
                Parent = rootDirectory,
            };
            rootDirectory.Items.Add(relativeLibraries);
            var logger = Fixture.Freeze<ILogger>();
            var osDependant = Fixture.Freeze<IOSDependent>();

            var actual = ProjectFileWatcher.FindMatchingDirectory(rootDirectory, "Libraries", logger, osDependant);

            Assert.That(actual, Is.SameAs(relativeLibraries));
        }
    }
}