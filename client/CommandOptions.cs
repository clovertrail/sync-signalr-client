using McMaster.Extensions.CommandLineUtils;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace common.sync.client
{
    [VersionOptionFromMember(MemberName = nameof(GetVersion))]
    [HelpOption("--help")]
    [Subcommand(
        typeof(PrimaryConnectionOptions),
        typeof(SecondaryConnectionOptions))]
    internal class CommandOptions : BaseOption
    {
        public string GetVersion()
            => typeof(CommandOptions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        protected override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }

    [Command(Name = "primary", FullName = "primary", Description = "Primary connection options")]
    internal class PrimaryConnectionOptions : BaseOption
    {
        [Option("-l|--groupNameLength", Description = "Specify the group name length to do sync. The group name is auto generated.")]
        public int GroupNameLength { get; set; } = 8;

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            var groupName = GenRandomName(this.GroupNameLength);
            var cli = new SyncClient(this.TransportHubUrl);
            var firstHub = await cli.ConnectToHub(true, groupName, null, null);
            Console.WriteLine($"Group for sync: {groupName}");
            Console.WriteLine("Press Ctrl+C to stop");
            await WaitUntilCancel();
            // stop all connections.
            await SyncClient.LeaveNegotiationGroupAsync(firstHub, groupName);
            await firstHub.StopAsync();
        }
        
    }

    [Command(Name = "secondary", FullName = "secondary", Description = "Secondary connection options")]
    internal class SecondaryConnectionOptions : BaseOption
    {
        [Option("-g|--groupName", Description = "Specify the group name to do sync.")]
        public string GroupName { get; set; }

        [Option("-u|--userId", Description = "Specify the user Id to connect transport Hub. A random value is generated if you do not specify it")]
        public string UserId { get; set; }

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            if (String.IsNullOrEmpty(GroupName))
            {
                Console.WriteLine("Missing groupName parameter");
                return;
            }
            var cli = new SyncClient(this.TransportHubUrl);
            var tcs = new TaskCompletionSource<object>();
            var userId = GenRandomName(8);
            var secondNotificationHub = await cli.ConnectToHub(false, GroupName, userId, tcs);
            await tcs.Task; // waiting until it gets the target URL and access token. TODO: a time out is required.
            var info = cli.InfoToTransportHub;
            var secondTransportHubConnection = await cli.DirectConnectToTransportHub(info);
            Console.WriteLine("Press Ctrl+C to stop");
            await WaitUntilCancel();
            await SyncClient.LeaveNegotiationGroupAsync(secondNotificationHub, GroupName);
            await secondNotificationHub.StopAsync();
            await secondTransportHubConnection.StopAsync();
        }
    }

    [HelpOption("--help")]
    internal abstract class BaseOption
    {
        [Option("-t|--transportHubUrl", Description = "Specify the transport hub URL. Default value is 'http://localhost:5000/transporthub'")]
        public string TransportHubUrl { get; set; } = "http://localhost:5000/transporthub";

        protected virtual Task OnExecuteAsync(CommandLineApplication app)
        {
            return Task.CompletedTask;
        }

        protected static void ReportError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Unexpected error: {ex}");
            Console.ResetColor();
        }

        protected string GenRandomName(int len)
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
    }
}
