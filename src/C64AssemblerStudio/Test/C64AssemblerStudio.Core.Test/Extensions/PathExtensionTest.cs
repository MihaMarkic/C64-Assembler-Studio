using C64AssemblerStudio.Core.Extensions;
using NUnit.Framework;

namespace C64AssemblerStudio.Core.Test.Extensions;

public class PathExtensionTest
{
    [TestFixture]
    public class PathAsSegmentRanges : PathExtensionTest
    {
        [Test]
        public void GivenSampleMixedSeparatorPath_ReturnsCorrectRanges()
        {
            const string Path = @"C:\Win/Temp";

            var actual = Path.AsSpan().PathAsSegmentRanges();

            ReadOnlySpan<Range> expected = [new (0, 2), new (3, 6), new (7, 11)];

            Assert.That(actual.SequenceEqual(expected), Is.True);
        }
    }
    [TestFixture]
    public class PathSegmentsStartWith : PathExtensionTest
    {
        [TestCase(@"C:\Win/Temp", @"C:\Win", ExpectedResult = true)]
        [TestCase(@"C:\Win/Temp", @"C:\Win\", ExpectedResult = true)]
        [TestCase(@"C:\Win/Temp", @"C:\Win\Tubo", ExpectedResult = false)]
        [TestCase(@"C:\Win/Temp", @"C:\Win\Temp/Tubo", ExpectedResult = false)]
        [TestCase(@"C:\Win/Temp", @"C:/Win\", ExpectedResult = true)]
        [TestCase(@"Tubo.asm", @"T", ExpectedResult = true)]
        [TestCase(@"Win/Tubo.asm", @"Win/T", ExpectedResult = true)]
        public bool WhenBothSegmentsAreEqual_ReturnsTrue(string sourceText, string startsWithText)
        {
            var source = sourceText.AsSpan().PathAsSegmented();
            var startsWith = startsWithText.AsSpan().PathAsSegmented();

            var actual = source.PathSegmentsStartWith(startsWith, StringComparison.InvariantCultureIgnoreCase);

            return actual;
        }
    }
}
