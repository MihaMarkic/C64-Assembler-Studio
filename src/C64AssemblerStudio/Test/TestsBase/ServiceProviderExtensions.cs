using AutoFixture;
using AutoFixture.Kernel;
using NSubstitute;

namespace TestsBase;

public static class ServiceProviderExtensions
{
    public static void GetRequiredServiceMock<T>(this IServiceProvider serviceProvider, Fixture fixture,
        T service)
    {
        serviceProvider.GetService(Arg.Any<Type>())
            .Returns(x =>
            {
                if ((Type)x[0] == typeof(T))
                {
                    return service;
                }
                return fixture.Create((Type)x[0], new SpecimenContext(fixture));
            });
    }
    public static T GetRequiredServiceMock<T>(this IServiceProvider serviceProvider, Fixture fixture)
    {
        var service = fixture.Create<T>();
        serviceProvider.GetService(Arg.Any<Type>())
            .Returns(x =>
            {
                if ((Type)x[0] == typeof(T))
                {
                    return service;
                }
                return fixture.Create((Type)x[0], new SpecimenContext(fixture));
            });
        return service;
    }
}