using System.Text;
using Chronos.Configuration;
using Common.Logging;
using ScriptCs;
using ScriptCs.Engine.Roslyn;
using ScriptCs.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Extras.Topshelf;
using Topshelf;
using LogLevel = ScriptCs.Contracts.LogLevel;

namespace StatusCheck
{
    class Program
    {
        public static WebAppHost AppHost;
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.RollingFile(ConfigUtilities.GetAppSetting("LogPath"), LogEventLevel.Information)
                .WriteTo.ColoredConsole()
                .CreateLogger();

            Log.Information("Logger started");


            AppHost = new WebAppHost();
            AppHost.Start();


            HostFactory.Run(x =>
            {
                x.UseSerilog();

                x.Service<ServiceProcessor>(s =>
                {
                    s.ConstructUsing(i => new ServiceProcessor());
                    s.WhenStarted(ts => ts.Start());
                    s.WhenStopped(ts => ts.Stop());
                });

                x.RunAsLocalSystem();

                x.SetDescription("Runs various status checks and alerts if something is weird");
                x.SetDisplayName("Mobile.StatusCheck");
                x.SetServiceName("MobileStatusCheck");
            });

        }
    }
}
