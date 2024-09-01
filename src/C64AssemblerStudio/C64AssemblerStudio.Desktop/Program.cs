using Avalonia;
using Microsoft.Extensions.Hosting;
using NLog;
using Velopack;

namespace C64AssemblerStudio.Desktop
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            NLog.Common.InternalLogger.LogToConsole = true;
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                VelopackApp.Build().Run();
                var host = CreateHostBuilder(args);
                Core.IoC.Init(host.Build());
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                logger.Error(ex, "Stopped program because of an exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args)
        {
            // not using Host.CreateDefaultBuilder because FileWatcher (used by default for tracking appsettings)
            // on Mac is broken - waits a long time
            var hostBuilder = new HostBuilder();
            return hostBuilder
                .ConfigureServices((_, services) =>
                    services.Configure()
                );
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
