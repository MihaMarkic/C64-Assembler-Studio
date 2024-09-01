using AutoFixture;
using C64AssemblerStudio.Engine.Models.SyntaxEditor;
using C64AssemblerStudio.Engine.Services.Implementation;
using NUnit.Framework;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.Services.Implementation;

public class BreakpointConditionGrammarServiceTest: BaseTest<BreakpointConditionGrammarService>
{
    [TestFixture]
    public class VerifyText : BreakpointConditionGrammarServiceTest
    {
        [SetUp]
        public void Setup()
        {
            var serviceProvider = Fixture.Freeze<IServiceProvider>();
            var listener = Fixture.Create<BreakpointConditionsListener>();
            serviceProvider.GetRequiredServiceMock(Fixture, listener);
        }
        [Test]
        public void WhenNullText_ReturnsNoTokensAndNoErrors()
        {
            var actual = Target.VerifyText(null);

            Assert.That(actual.Tokens.Count, Is.Zero);
            Assert.That(actual.Errors.Length, Is.Zero);
        }
        [Test]
        public void WhenSingleRegisterToken_ParsesCorrectly()
        {
            var actual = Target.VerifyText("A");
            SyntaxEditorToken[] expected = [new (BreakpointDetailConditionTokenType.Register, 1, 0, 1)];
            Assert.That(actual.Tokens, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenSecondTokenIsAnError_ReportsAnError()
        {
            var actual = Target.VerifyText("A Error");

            Assert.That(actual.Errors.Length, Is.EqualTo(1));
        }
        [Test]
        public void WhenSimpleEquation_ParsesCorrectly()
        {
            var actual = Target.VerifyText("A==$5");
            
            SyntaxEditorToken[] expected = [
                new (BreakpointDetailConditionTokenType.Register, 1, 0, 1),
                new (BreakpointDetailConditionTokenType.Operator, 1, 1, 2),
                new (BreakpointDetailConditionTokenType.Number, 1, 3, 2)];
            Assert.That(actual.HasError, Is.False);
            Assert.That(actual.Tokens, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenUsingBank_ParsesCorrectly()
        {
            var actual = Target.VerifyText("@bank:A==$5");

            SyntaxEditorToken[] expected =
            [
                new(BreakpointDetailConditionTokenType.Bank, 1, 1, 4),
                new(BreakpointDetailConditionTokenType.Register, 1, 6, 1),
                new(BreakpointDetailConditionTokenType.Operator, 1, 7, 2),
                new(BreakpointDetailConditionTokenType.Number, 1, 9, 2)
            ];
            Assert.That(actual.Tokens, Is.EquivalentTo(expected));
        }

        [Test]
        public void WhenUsingInvalidMemspace_ReturnsError()
        {
            var actual = Target.VerifyText("X:A==$5");
            
            Assert.That(actual.HasError, Is.True);
        }
        [Test]
        public void WhenUsingInvalidMemspace_ErrorsContainCorrectSyntaxElement()
        {
            var actual = Target.VerifyText("X:A==$5");
            
            SyntaxEditorError[] expected =
            [
                new(SyntaxEditorErrorKind.InvalidMemspace, 1, 0, 1),
            ];
            Assert.That(actual.Errors, Is.EquivalentTo(expected));
        }
        [Test]
        public void WhenUsingInvalidRegister_ErrorsContainCorrectSyntaxElement()
        {
            var actual = Target.VerifyText("Z==$5");
            
            SyntaxEditorError[] expected =
            [
                new(SyntaxEditorErrorKind.InvalidRegister, 1, 0, 1),
            ];
            Assert.That(actual.Errors, Is.EquivalentTo(expected));
        }
    }
}