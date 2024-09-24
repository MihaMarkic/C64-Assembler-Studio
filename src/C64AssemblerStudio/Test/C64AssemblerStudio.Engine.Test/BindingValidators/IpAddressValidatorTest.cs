using C64AssemblerStudio.Engine.BindingValidators;
using NUnit.Framework;
using TestsBase;

namespace C64AssemblerStudio.Engine.Test.BindingValidators;

public class IpAddressValidatorTest: BaseTest<IpAddressValidator>
{
    [TestFixture]
    public class Update : IpAddressValidatorTest
    {
        [TestCase(null, ExpectedResult = false)]
        [TestCase("localhost", ExpectedResult = true)]
        [TestCase("localhost:", ExpectedResult = true)]
        [TestCase("localhost:alfa", ExpectedResult = true)]
        [TestCase("localhost:123456", ExpectedResult = true)]
        [TestCase("localhost:6610", ExpectedResult = false)]
        [TestCase("127.0.0.1:6610", ExpectedResult = false)]
        public bool GivenInput_HasNoErrors(string text)
        {
            Target.Update(text);
            return Target.HasErrors;
        }
    }
}