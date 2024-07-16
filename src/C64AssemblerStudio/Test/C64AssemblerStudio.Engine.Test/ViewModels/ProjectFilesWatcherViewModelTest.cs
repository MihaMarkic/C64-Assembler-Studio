using C64AssemblerStudio.Engine.ViewModels;
using NUnit.Framework;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.ViewModels;

public class ProjectFilesWatcherViewModelTest: BaseTest<ProjectFilesWatcherViewModel>
{
    [TestFixture]
    public class FindMatchingDirectory : ProjectFilesWatcherViewModelTest
    {
        [Test]
        public void WhenRootDirectory_ReturnsNull()
        {
            var actual = Target.FindMatchingDirectory(Path.Combine("D:", "Root"), "");
            
            Assert.That(actual, Is.Null);
        }
    }
}