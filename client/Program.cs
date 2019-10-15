using client;
using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace SignalRStreamClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<CommandOptions>(args);
        }
    }
}
