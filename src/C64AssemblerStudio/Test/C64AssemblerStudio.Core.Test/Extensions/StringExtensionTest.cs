using NUnit.Framework;
using C64AssemblerStudio.Core.Extensions;

namespace C64AssemblerStudio.Core.Test.Extensions;

public class StringExtensionTest
{
    [TestFixture]
    public class ExtractLine : StringExtensionTest
    {
        [TestCase("", 0, ExpectedResult = "")]
        [TestCase("""
                       Zero line
                       """, 0,
            ExpectedResult = "Zero line")]
        [TestCase("""
                  Zero line
                  First line
                  """, 1,
            ExpectedResult = "First line")]
        [TestCase("""
                  Zero line
                  First line
                  """, 0,
            ExpectedResult = "Zero line")]
        public string GivenSample_ExtractsLineCorrectly(string input, int lineNumber)
        {
            var actual = input.AsSpan().ExtractLine(lineNumber);

            return actual.ToString();
        }

        [TestCase("", 1)]
        [TestCase("", -1)]
        [TestCase("""
                  Zero line
                  """, 1)]
        public void GivenSampleWithInvalidLineNumber_ThrowsArgumentException(string input, int lineNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => input.AsSpan().ExtractLine(lineNumber));
        }
    }
}