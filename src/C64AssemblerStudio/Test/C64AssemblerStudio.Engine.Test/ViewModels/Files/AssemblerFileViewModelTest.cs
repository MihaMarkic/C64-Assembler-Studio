using C64AssemblerStudio.Engine.ViewModels.Files;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.ViewModels.Files;

public class AssemblerFileViewModelTest: BaseTest<AssemblerFileViewModel>
{
    [TestFixture]
    public class ParseText : AssemblerFileViewModelTest
    {
        [Test]
        public void WhenEmptyContent_ReturnsEmpty()
        {
            var actual = AssemblerFileViewModel.ParseText(Substitute.For<ILogger>(), "");

            Assert.That(actual, Is.Empty);
        }
    }
}