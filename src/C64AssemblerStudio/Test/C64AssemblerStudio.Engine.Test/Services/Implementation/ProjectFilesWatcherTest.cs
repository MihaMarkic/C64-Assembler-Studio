using AutoFixture;
using C64AssemblerStudio.Engine.Models.Projects;
using C64AssemblerStudio.Engine.Services.Implementation;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
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
            var rootDirectoy = new ProjectRoot
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var logger = Fixture.Freeze<ILogger>();

            var actual = ProjectFileWatcher.FindMatchingDirectory(rootDirectoy, "", logger);

            Assert.That(actual, Is.SameAs(rootDirectoy));
        }

        [Test]
        public void WhenDirectoryMatchesLibraries_AndThereIsNoRelativeLibrariesDirectory_ThrowsException()
        {
            var rootDirectoy = new ProjectRoot
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var library = new ProjectLibraries
            {
                Name = "Libraries",
                Parent = null,
            };
            rootDirectoy.Items.Add(library);
            var logger = Fixture.Freeze<ILogger>();

            Assert.Throws<Exception>(() => ProjectFileWatcher.FindMatchingDirectory(rootDirectoy, "Libraries", logger));
        }

        [Test]
        public void
            WhenDirectoryMatchesLibraries_AndThereIsRelativeLibrariesDirectory_ReturnsRelativeLibrariesDirectory()
        {
            var rootDirectoy = new ProjectRoot
            {
                AbsoluteRootPath = @"D:\root",
                Name = "Root",
                Parent = null,
            };
            var libraries = new ProjectLibraries
            {
                Name = "Libraries",
                Parent = null,
            };
            rootDirectoy.Items.Add(libraries);
            var relativeLibraries = new ProjectDirectory
            {
                Name = "Libraries",
                Parent = rootDirectoy,
            };
            rootDirectoy.Items.Add(relativeLibraries);
            var logger = Fixture.Freeze<ILogger>();

            var actual = ProjectFileWatcher.FindMatchingDirectory(rootDirectoy, "Libraries", logger);

            Assert.That(actual, Is.SameAs(relativeLibraries));
        }
    }
}