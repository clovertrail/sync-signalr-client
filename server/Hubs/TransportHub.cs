using common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using SyncClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class TransportHub : Hub
    {
        private readonly IServerNameProvider _serverNameProvider;
        private Pairing<TransportHub> _pairing;

        public TransportHub(IServerNameProvider serverNameProvider, Pairing<TransportHub> pairing)
        {
            _serverNameProvider = serverNameProvider;
            _pairing = pairing;
        }

        public override async Task OnConnectedAsync()
        {
            // Send sticky information to the client
            var serverName = _serverNameProvider.GetName();
            var clientRequestId = Context.GetHttpContext().Request.Query["asrs_request_id"];
            _pairing.Increase();
            Console.WriteLine($"client{_pairing.Count()} request ID: {clientRequestId}");
            if (_pairing.Count() == 1)
            {
                // Only the first connected client will get the sticky information
                var dic = new Dictionary<string, string>()
                {
                    { "asrs.sync.1stclient.server", serverName },
                    { "asrs.sync.1stclient.request_id", clientRequestId},
                    { "asrs.sync.1stclient.hub", "transportHub"}
                };
                
                await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.TransportHubInfo, dic);
            }
            if (_pairing.Count() == 2)
            {
                Console.WriteLine("Succesfully see two clients are connected to this hub");
            }
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.HubConnected, Context.ConnectionId);
        }
        
    }
}
