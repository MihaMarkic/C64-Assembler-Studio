using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace TestsBase;

public abstract class BaseTest<T>
    where T : class
{
    protected Fixture Fixture = default!;
    private T? _target = default!;

    protected T Target
    {
        [DebuggerStepThrough]
        get
        {
            if (_target is null)
            {
                _target = Fixture.Build<T>().OmitAutoProperties().Create();
            }
            return _target;
        }
    }

    [SetUp]
    public void SetUp()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
    }
    [TearDown]
    public void TearDown()
    {
        _target = null!;
    }
}