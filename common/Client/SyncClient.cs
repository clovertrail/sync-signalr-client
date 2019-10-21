using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.sync
{
    public class SyncClient
    {
        public string TransportHubUrl { get; set; }

        public AccessData InfoToTransportHub { get; set; }

        public SyncClient(string transportHubUrl)
        {
            TransportHubUrl = transportHubUrl;
        }

        public async Task<HubConnection> ConnectToHub(
            bool isPrimaryClient,
            string groupName,
            string userId,
            TaskCompletionSource<object> notificationHub)
        {
            var hubConnectionBuilder = new HubConnectionBuilder();

            var hubConnection = hubConnectionBuilder.WithUrl(TransportHubUrl).WithAutomaticReconnect().Build();
            hubConnection.Closed += HubConnection_Closed;
            hubConnection.On<StickyPayloadData>(ClientSyncConstants.TransportHubInfo, async (payload) =>
            {
                if (isPrimaryClient)
                {
                    //_transportHubInfo = payload;
                    await HubConnectionHelpers.ProvideTransportHubInfo(hubConnection, payload);
                    Console.WriteLine("Received sticky Hub info");
                }
                else
                {
                    // Ignore sticky for secondary client
                }
            });
            if (isPrimaryClient)
            {
                await HubConnectionHelpers.JoinNotificationGroupAfterConnected(hubConnection, groupName, null);
            }
            else
            {
                await HubConnectionHelpers.RequestAccessTokenAfterJoinNotificationGroup(hubConnection, groupName, userId);
                hubConnection.On<string>(ClientSyncConstants.ClientPartnerDropped, (droppedConnectionId) => {
                    Console.WriteLine($"connection {droppedConnectionId} is dropped");
                });
            }
            hubConnection.On<AccessData>(ClientSyncConstants.ResponseToTargetUrlAccessToken, (payload) =>
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

        public async Task<HubConnection> DirectConnectToTransportHub()
        {
            return await DirectConnectToTransportHub(InfoToTransportHub);
        }

        public async Task<HubConnection> DirectConnectToTransportHub(AccessData infoToTransportHub)
        {
            if (infoToTransportHub == null)
            {
                Console.WriteLine();
                return null;
            }
            var hubConnectionBuilder = new HubConnectionBuilder();
            var hubConnection = hubConnectionBuilder.WithUrl(infoToTransportHub.Endpoint, opt => {
                opt.AccessTokenProvider = () => Task.FromResult(infoToTransportHub.AccessKey);
                //opt.SkipNegotiation = true;
                //opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
            }).Build();
            hubConnection.On<string>(ClientSyncConstants.HubConnected, (connectionId) =>
            {
                Console.WriteLine($"connection Id {connectionId}");
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
