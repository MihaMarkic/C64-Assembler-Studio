using C64AssemblerStudio.Engine.Services.Implementation;
using NUnit.Framework;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.Services.Implementation;

public class ProjectFilesWatcherTest: BaseTest<ProjectFileWatcher>
{
    [TestFixture]
    public class FindMatchingDirectory : ProjectFilesWatcherTest
    {
        [Test]
        public void WhenRootDirectory_ReturnsNull()
        {
            var actual = Target.FindMatchingDirectory(Path.Combine("D:", "Root"), "");
            
            Assert.That(actual, Is.Null);
        }
    }
}