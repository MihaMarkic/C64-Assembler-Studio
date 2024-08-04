using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Core.Common;

public abstract class RelayCommandCore
{
    protected void LogException(Exception ex)
    {
        this.GetLogger().LogError(ex, "Error while executing command");
    }
}