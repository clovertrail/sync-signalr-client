using client;
using System;
using System.Threading.Tasks;

namespace SignalRStreamClient
{
    class Program
    {
        public static string GenerateRandomData(int len)
        {
            var message = new byte[len];
            Random rnd = new Random();
            rnd.NextBytes(message);
            return Convert.ToBase64String(message).Substring(0, len);
        }

        public static async Task WaitUntilCancel()
        {
            var shutdown = new TaskCompletionSource<object>();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                shutdown.TrySetResult(null);
            };
            await shutdown.Task;
        }

        public static async Task TestSyncClient(string transportHubUrl, string notificationHubUrl)
        {
            var groupName = "notificationGroup"; // dummy value
            var secondClientUserId = "Iam2ndClient"; // dummy value
            var cli = new Client(transportHubUrl, notificationHubUrl);
            var firstTransportHubConnection = await cli.ConnectToTransportHub();
            var firstNotificationHubConnection = await cli.ConnectToNotificationHub(groupName, null, false, null);
            var tcs = new TaskCompletionSource<object>();
            var secondNotificationHubConnection = await cli.ConnectToNotificationHub(groupName, secondClientUserId, true, tcs);
            await tcs.Task;
            Console.WriteLine("Successfully get the transport connection information");
            var secondTransportHubConnection = await cli.DirectConnectToTransportHub();

            await firstNotificationHubConnection.StopAsync();
            await secondNotificationHubConnection.StopAsync();
            await firstTransportHubConnection.StopAsync();
            await secondTransportHubConnection.StopAsync();
        }

        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Specify the <transportHubUrl> and <notificationHubUrl>");
                return;
            }
            await TestSyncClient(args[0], args[1]);
        }
    }
}
