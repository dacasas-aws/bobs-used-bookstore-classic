using BobsBookstoreClassic.Data;
using Bookstore.Common;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace Bookstore.Web
{
    public static class LoggingSetup
    {
        public static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();

            Target loggingTarget;

            if (BookstoreConfiguration.GetSetting("Services/LoggingService") == "aws")
            {
                loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
            }
            else
            {
                loggingTarget = new DebuggerTarget();
            }

            config.AddTarget("aws", loggingTarget);

            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, loggingTarget));

            // Program.cs calls this before building the host and routes Microsoft.Extensions.Logging to NLog via builder.Host.UseNLog().
            LogManager.Configuration = config;
        }
    }
}