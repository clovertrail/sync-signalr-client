﻿using McMaster.Extensions.CommandLineUtils;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace client
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
        [Option("-t|--transportHubUrl", Description = "Specify the transport hub URL.")]
        public string TransportHubUrl { get; set; }

        [Option("-l|--groupNameLength", Description = "Specify the group name length to do sync. The group name is auto generated.")]
        public int GroupNameLength { get; set; } = 8;

        protected override async Task OnExecuteAsync(CommandLineApplication app)
        {
            var groupName = GenRandomName(this.GroupNameLength);
            var cli = new Client(this.TransportHubUrl, this.NotificationHubUrl);
            var firstTransportHub = await cli.ConnectToTransportHub();
            var firstNotificationHub = await cli.ConnectToNotificationHub(groupName, null, false, null);
            Console.WriteLine($"Group for sync: {groupName}");
            Console.WriteLine("Press Ctrl+C to stop");
            await WaitUntilCancel();
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
            var cli = new Client(this.NotificationHubUrl);
            var tcs = new TaskCompletionSource<object>();
            var userId = GenRandomName(8);
            var secondNotificationHub = await cli.ConnectToNotificationHub(GroupName, userId, true, tcs);
            await tcs.Task;
            var info = cli.InfoToTransportHub;
            var secondTransportHubConnection = await cli.DirectConnectToTransportHub(info);
            Console.WriteLine("Press Ctrl+C to stop");
            await WaitUntilCancel();
        }
    }

    [HelpOption("--help")]
    internal abstract class BaseOption
    {
        [Option("-n|--notificationHubUrl", Description = "Specify the notification hub URL.")]
        public string NotificationHubUrl { get; set; }

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
