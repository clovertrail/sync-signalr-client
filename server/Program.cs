using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.DependencyInjection;
using server;
using SyncClient;
using System;

namespace SignalRChat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please specify the ASRS connection string");
                return;
            }
            var serviceEndpoint = new SyncClient.ServiceEndpoint(args[0]);
            var asrsConfig = new ASRSConfig(args[0]);
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureServices(s => s.AddSingleton(typeof(Pairing<>))
                                         .AddSingleton(serviceEndpoint)
                                         .AddSingleton(asrsConfig))
                .Build().Run();
        }
    }
}
