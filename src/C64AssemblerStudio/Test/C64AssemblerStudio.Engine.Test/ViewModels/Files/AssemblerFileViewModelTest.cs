using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using C64AssemblerStudio.Engine.ViewModels;
using C64AssemblerStudio.Engine.ViewModels.Files;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;
using Righthand.RetroDbgDataProvider.Models.Program;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.ViewModels.Files;

public class AssemblerFileViewModelTest: BaseTest<AssemblerFileViewModel>
{
    [TestFixture]
    public class ConvertIgnoredMultiLineStructureToSingleLine : AssemblerFileViewModelTest
    {
        [Test]
        public void GivenSingleSingleLineRange_ReturnsSingleLine()
        {
            ImmutableArray<MultiLineTextRange> source = [new(new(2, 0), new(2, 8))];

            var actual = AssemblerFileViewModel.ConvertIgnoredMultiLineStructureToSingleLine(source);

            Assert.That(actual.Keys, Is.EquivalentTo(new[] { 2 }));
            ImmutableArray<SingleLineTextRange> expectedRange = [new (0, 8)];
            Assert.That(actual.Values.Single(), Is.EquivalentTo(expectedRange));
        }
    }
    [TestFixture]
    public class PopulateSuggestionsForArrayProperty : AssemblerFileViewModelTest
    {
        public static IEnumerable<(string Root, string ValueType, FrozenSet<string> Excluded, FrozenSet<string> Expected)> GetEnumerator()
        {
            yield return new ("", ".file", ["mbfiles"], ["name","type"]);
            yield return new ("t", ".file", ["mbfiles"], ["type"]);
            yield return new ("na", ".file", ["mbfiles"], ["name"]);
            yield return new ("x", ".file", ["mbfiles"], []);
        }
        
        [TestCaseSource(nameof(GetEnumerator))]
        public void GivenSamples_ReturnsNames((string Root, string ValueType, FrozenSet<string> Excluded, FrozenSet<string> Expected) td)
        {
            var actual = AssemblerFileViewModel.PopulateSuggestionsForArrayProperty(td.Root, td.ValueType, td.Excluded)
                .ToFrozenSet();

            Assert.That(actual, Is.EquivalentTo(td.Expected));
        }
    }

    // [TestFixture]
    // public class ParseText : AssemblerFileViewModelTest
    // {
    // [Test]
        // public void WhenEmptyContent_ReturnsEmpty()
        // {
        //     var actual = AssemblerFileViewModel.ParseText(Substitute.For<ILogger>(), "");
        //
        //     Assert.That(actual.Lines, Is.Empty);
        //     Assert.That(actual.Tokens, Is.Empty);
        // }
    // }
    //
    // [TestFixture]
    // public class IterateTokens : AssemblerFileViewModelTest
    // {
        // [Test]
        // public void WhenInstructionExtensionIsUsed_DetectsProperly()
        // {
        //     ImmutableArray<IToken> tokens =
        //     [
        //         new CommonToken(KickAssemblerLexer.STA) { Line = 1 }, 
        //         new CommonToken(KickAssemblerLexer.DOT) { Line = 1 },
        //         new CommonToken(KickAssemblerLexer.ONLYA) { StartIndex = 4, StopIndex = 5, Line = 1},
        //         new CommonToken(KickAssemblerLexer.HEX_NUMBER) { Line = 1 }
        //     ];
        //     var actual = AssemblerFileViewModel.IterateTokens(1, tokens, default);
        //     
        //     Assert.That(actual.Single()!.Count, Is.EqualTo(3));
        // }
    // }
}