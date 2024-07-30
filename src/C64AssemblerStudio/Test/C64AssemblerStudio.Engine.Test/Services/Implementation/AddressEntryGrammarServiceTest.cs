using System.Collections.Frozen;
using C64AssemblerStudio.Engine.Services.Implementation;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.Models.Program;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.Services.Implementation;

public class AddressEntryGrammarServiceTest: BaseTest<AddressEntryGrammarService>
{
    protected static readonly FrozenDictionary<string, Label> EmptyLabelsMap = FrozenDictionary<string, Label>.Empty;
    [TestFixture]
    public class CalculateAddress : AddressEntryGrammarServiceTest
    {
        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = null)]
        [TestCase("     ", ExpectedResult = null)]
        public ushort? WhenEmptyInput_ReturnsNull(string? text)
        {
            return Target.CalculateAddress(EmptyLabelsMap, text);
        }
        [Test]
        public void GivenDecimalNumber_ReturnsTheParsedValue()
        {
            var actual = Target.CalculateAddress(EmptyLabelsMap, "45");
            Assert.That(actual, Is.EqualTo(45));
        }
        [Test]
        public void GivenBinaryNumber_ReturnsTheParsedValue()
        {
            var actual = Target.CalculateAddress(EmptyLabelsMap, "%10");
            Assert.That(actual, Is.EqualTo(2));
        }
        [Test]
        public void GivenHexNumber_ReturnsTheParsedValue()
        {
            var actual = Target.CalculateAddress(EmptyLabelsMap, "$10");
            Assert.That(actual, Is.EqualTo(16));
        }
        [Test]
        public void GivenAdditionBetweenTwoDecNumbers_ReturnsCorrectResult()
        {
            var actual = Target.CalculateAddress(EmptyLabelsMap, "10 + 8");
            Assert.That(actual, Is.EqualTo(18));
        }

        [Test]
        public void GivenSingleKnownLabel_ReturnsItsAddress()
        {
            var labelsMap = new Dictionary<string, Label>
                { { "label", new Label(TextRange.Empty, "label", 1122) } };

            var actual = Target.CalculateAddress(labelsMap, "label");
            
            Assert.That(actual, Is.EqualTo(1122));
        }
        [Test]
        public void GivenUnknownLabel_ThrowsException()
        {
            Assert.Throws<Exception>(() => Target.CalculateAddress(EmptyLabelsMap, "label"));
        }
    }
}