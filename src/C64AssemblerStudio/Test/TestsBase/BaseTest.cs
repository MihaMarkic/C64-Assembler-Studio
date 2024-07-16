using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;

namespace TestsBase;

public abstract class BaseTest<T>
    where T : class
{
    protected Fixture _fixture = default!;
    T _target = default!;
    public T Target
    {
        [DebuggerStepThrough]
        get
        {
            if (_target is null)
            {
                _target = _fixture.Build<T>().OmitAutoProperties().Create();
            }
            return _target;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoNSubstituteCustomization());
    }
    [TearDown]
    public void TearDown()
    {
        _target = null!;
    }
}