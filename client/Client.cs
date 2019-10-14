using common;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client
{
    public class Client
    {
        private IDictionary<string, string> _transportHubInfo;
        private IDictionary<string, string> _redirectInfoToTransportHubInfo;
        private string _transportHubUrl;
        private string _notificationHubUrl;
        private HubConnection _1st_transportHubConnection;
        private HubConnection _1st_notificationHubConnection;
        private HubConnection _2nd_notificationHubConnection;

        public Client(string transportHubUrl, string notificationHubUrl)
        {
            _transportHubUrl = transportHubUrl;
            _notificationHubUrl = notificationHubUrl;
        }

        public async Task<HubConnection> ConnectToTransportHub()
        {
            string selfConnectionId = null;
            var hubConnectionBuilder = new HubConnectionBuilder();

            var hubConnection = hubConnectionBuilder.WithUrl(_transportHubUrl).WithAutomaticReconnect().Build();
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
            if (_redirectInfoToTransportHubInfo == null)
            {
                Console.WriteLine();
                return null;
            }
            var hubConnectionBuilder = new HubConnectionBuilder();
            var hubConnection = hubConnectionBuilder.WithUrl(_redirectInfoToTransportHubInfo["asrs.sync.2ndclient.hub_url"], opt => {
                opt.AccessTokenProvider = () => Task.FromResult(_redirectInfoToTransportHubInfo["asrs.sync.2ndclient.access_key"]);
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
            bool wantToConnectTransportHub,
            TaskCompletionSource<object> notificationHub)
        {
            string selfConnectionId = null;
            var hubConnectionBuilder = new HubConnectionBuilder();
            var hubConnection = hubConnectionBuilder.WithUrl(_notificationHubUrl).WithAutomaticReconnect().Build();
            hubConnection.Closed += HubConnection_Closed;
            hubConnection.On(ClientSyncConstants.JoinedGroup, async () =>
            {
                Console.WriteLine("Joined group");
                if (wantToConnectTransportHub)
                {
                    // the 2nd connection to notification hub wants to connect transport hub
                    var requestConnection = new Dictionary<string, string>()
                    {
                        { "asrs.sync.client.groupname", groupName},
                        { "asrs.sync.2ndclient.userid", userId}
                    };
                    await hubConnection.SendAsync(ClientSyncConstants.GroupBroadcast, ClientSyncConstants.RequestType, requestConnection);
                }
            });
            hubConnection.On<string>(ClientSyncConstants.HubConnected, (connectionId) =>
            {
                selfConnectionId = connectionId;
                Console.WriteLine($"connection Id {selfConnectionId}");
                hubConnection.SendAsync(ClientSyncConstants.JoinGroup, groupName);
            });
            hubConnection.On<IDictionary<string, string>>(ClientSyncConstants.RequestConnectToTransportHub, async (payload) =>
            {
                // Assume the 1st connection has already obtained hub connection info.
                // merge with the previous 1st connection's info.
                foreach (var val in _transportHubInfo)
                {
                    payload.Add(val.Key, val.Value);
                }
                await hubConnection.SendAsync(ClientSyncConstants.GroupBroadcast, ClientSyncConstants.ResponseType, payload);
            });
            hubConnection.On<IDictionary<string, string>>(ClientSyncConstants.ResponseToTargetUrlAccessToken, (payload) =>
            {
                // received the connection info to transport hub
                _redirectInfoToTransportHubInfo = payload;
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
