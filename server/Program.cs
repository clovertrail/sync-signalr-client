using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SignalRChat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //BuildLoggerRepository("sync-server-logger", "log4net.xml");
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build().Run();
        }

        /* // log4net configuration
        private static void BuildLoggerRepository(string name, string configFilePath)
        {
            var repository = LogManager.CreateRepository(name);
            var config = ParseLog4NetConfigFile(configFilePath);
            log4net.Config.XmlConfigurator.Configure(repository, config);
        }

        public static XmlElement ParseLog4NetConfigFile(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                var config = new XmlDocument();
                config.Load(stream);
                return config["log4net"];
            }
        }
        */
    }
}
