namespace C64AssemblerStudio.Core.Services.Abstract;

/// <summary>
/// Serves like a unit test friendly way of getting required services.
/// It's method can be mocked, whereas GetRequiredService<T> extension method can't.
/// </summary>
/// <remarks>
/// Should be registered as scoped.
/// </remarks>
public interface IServiceCreator
{
    /// <summary>
    /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type of service object to get.</typeparam>
    /// <param name="provider">The <see cref="IServiceProvider"/> to retrieve the service object from.</param>
    /// <returns>A service object of type <typeparamref name="T"/>.</returns>
    /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
    T GetRequiredService<T>() where T : notnull;
}