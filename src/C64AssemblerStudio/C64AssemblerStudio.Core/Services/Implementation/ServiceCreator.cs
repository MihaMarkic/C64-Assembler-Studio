using C64AssemblerStudio.Core.Services.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace C64AssemblerStudio.Core.Services.Implementation;
public class ServiceCreator : IServiceCreator
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceCreator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc cref="IServiceCreator"/>
    public T GetRequiredService<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
}