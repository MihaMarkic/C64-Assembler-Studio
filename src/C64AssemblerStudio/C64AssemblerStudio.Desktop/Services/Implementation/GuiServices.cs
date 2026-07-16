using Avalonia.Threading;
using C64AssemblerStudio.Engine.Services.Abstract;

namespace C64AssemblerStudio.Desktop.Services.Implementation;

/// <inheritdoc />
public class GuiServices: IGuiServices
{
    /// <inheritdoc />
    public void WaitUntilCancellation(CancellationToken ct)
    {
        Dispatcher.UIThread.MainLoop(ct);
    }
}