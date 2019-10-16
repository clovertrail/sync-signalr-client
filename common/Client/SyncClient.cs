using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.sync
{
    public class Client
    {
        private IDictionary<string, string> _transportHubInfo;

        public string TransportHubUrl { get; set; }
        public string NotificationHubUrl { get; set; }

        public IDictionary<string, string> InfoToTransportHub { get; set; }

        public Client(string notificationHubUrl)
        {
            NotificationHubUrl = notificationHubUrl;
        }

        public Client(string transportHubUrl, string notificationHubUrl)
        {
            TransportHubUrl = transportHubUrl;
            NotificationHubUrl = notificationHubUrl;
        }

        public async Task<HubConnection> ConnectToTransportHub()
        {
            string selfConnectionId = null;
            var hubConnectionBuilder = new HubConnectionBuilder();

            var hubConnection = hubConnectionBuilder.WithUrl(TransportHubUrl).WithAutomaticReconnect().Build();
            hubConnection.Closed += HubConnection_Closed;
            hubConnection.On<IDictionary<string, string>>(ClientSyncConstants.TransportHubInfo, (payload) =>
            {
                _transportHubInfo = payload;
                Console.WriteLine("Received transport Hub info");
            });
            hubConnection.On<string>(ClientSyncConstants.HubConnected, (connectionId) =>
            {
                selfConnectionId = connectionId;
                Console.WriteLine($"connection Id {selfConnectionId}");
            });
            await hubConnection.StartAsync();
            return hubConnection;
        }

        public async Task<HubConnection> DirectConnectToTransportHub()
        {
            return await DirectConnectToTransportHub(InfoToTransportHub);
        }

        public async Task<HubConnection> DirectConnectToTransportHub(IDictionary<string, string> infoToTransportHub)
        {
            if (infoToTransportHub == null)
            {
                Console.WriteLine();
                return null;
            }
            var hubConnectionBuilder = new HubConnectionBuilder();
            var hubConnection = hubConnectionBuilder.WithUrl(infoToTransportHub["demo.sync.2ndclient.hub_url"], opt => {
                opt.AccessTokenProvider = () => Task.FromResult(infoToTransportHub["demo.sync.2ndclient.access_key"]);
                opt.SkipNegotiation = true;
                opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            }).Build();
            hubConnection.On<string>(ClientSyncConstants.HubConnected, (connectionId) =>
            {
                Console.WriteLine($"connection Id {connectionId}");
            });
            await hubConnection.StartAsync();
            return hubConnection;
        }

        public async Task<HubConnection> ConnectToNotificationHub(
            string groupName,
            string userId,
            bool isSecondaryClient,
            TaskCompletionSource<object> notificationHub)
        {
            var hubConnectionBuilder = new HubConnectionBuilder();
            var hubConnection = hubConnectionBuilder.WithUrl(NotificationHubUrl).WithAutomaticReconnect().Build();
            hubConnection.Closed += HubConnection_Closed;
            if (isSecondaryClient)
            {
                await HubConnectionHelpers.RequestAccessTokenAfterJoinNotificationGroup(hubConnection, groupName, userId);
            }
            else
            {
                await HubConnectionHelpers.JoinNotificationGroupAfterConnected(hubConnection, groupName, null);
                await HubConnectionHelpers.ProvideTransportHubInfo(hubConnection, _transportHubInfo);
            }
            
            hubConnection.On<IDictionary<string, string>>(ClientSyncConstants.ResponseToTargetUrlAccessToken, (payload) =>
            {
                // received the connection info to transport hub
                InfoToTransportHub = payload;
                Console.WriteLine("Received hub information to go to transport");
                if (notificationHub != null)
                {
                    notificationHub.TrySetResult(null);
                }
            });
            await hubConnection.StartAsync();
            return hubConnection;
        }

        private static Task HubConnection_Closed(Exception arg)
        {
            if (arg != null)
            {
                Console.WriteLine($"closed for {arg}");
            }
            else
            {
                Console.WriteLine("stop the connection");
            }
            
            return Task.CompletedTask;
        }
    }
}
