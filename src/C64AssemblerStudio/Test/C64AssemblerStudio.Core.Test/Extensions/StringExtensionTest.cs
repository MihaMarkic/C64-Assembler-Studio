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
        [TestCase("""
                  First
                  Zero line
                  """, 2)]
        public void GivenSampleWithInvalidLineNumber_ThrowsArgumentException(string input, int lineNumber)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => input.AsSpan().ExtractLine(lineNumber));
        }
    }

    [TestFixture]
    public class ExtractLinePosition: StringExtensionTest
    {
        [TestCase("", 0, ExpectedResult = 0)]
        [TestCase("""
                  Zero line
                  """, 0, ExpectedResult = 0)]
        [TestCase("""
                  First
                  Zero line
                  """, 1, ExpectedResult = 7)]
        public int GivenSample_ReturnsCorrectLenghtValue(string input, int lineNumber)
        {
            return input.AsSpan().ExtractLinePosition(lineNumber).Start;
        }
        [TestCase("", 0, ExpectedResult = 0)]
        [TestCase("""
                  Zero line
                  """, 0, ExpectedResult = 9)]
        [TestCase("""
                  Zero line
                  
                  """, 0, ExpectedResult = 9)]
        [TestCase("""
                  First
                  Second
                  """, 1, ExpectedResult = 6)]
        public int GivenSample_ReturnsCorrectStartLineValue(string input, int lineNumber)
        {
            return input.AsSpan().ExtractLinePosition(lineNumber).Length;
        }
    }
}