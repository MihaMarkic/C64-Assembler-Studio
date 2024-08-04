using C64AssemblerStudio.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class LoggerExtension
{
    public static ILogger<T> GetLogger<T>(this T source)
        where T: notnull
    {
        return IoC.Host.Services.GetRequiredService<ILogger<T>>();
    }
}