namespace C64AssemblerStudio.Engine.Services.Abstract;

/// <summary>
/// Provides GUI related services.
/// </summary>
public interface IGuiServices
{
    /// <summary>
    /// Blocks until CancellationToken <paramref name="ct"/> is cancelled.
    /// </summary>
    /// <param name="ct"></param>
    void WaitUntilCancellation(CancellationToken ct);
}