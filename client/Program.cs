using McMaster.Extensions.CommandLineUtils;
using System.Threading.Tasks;

namespace common.sync.client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<CommandOptions>(args);
        }
    }
}
