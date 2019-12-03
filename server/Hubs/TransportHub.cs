using common;
using common.sync;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class TransportHub : Hub
    {
        private SyncServer _syncServer;
        private ClientStatTracker<TransportHub> _pairing;
        private LoggerFactory _loggerFactory;
        private ILogger _logger;

        public TransportHub(SyncServer syncServer, ClientStatTracker<TransportHub> pairing )
        {
            _syncServer = syncServer;
            _pairing = pairing;
            _logger = _loggerFactory.CreateLogger<TransportHub>();
        }

        public override async Task OnConnectedAsync()
        {
            // Show the client connection information
            var requestId = SyncServer.ServiceRequestId(this);
            var asrsInstanceId = SyncServer.ASRSInstanceId(this);
            Console.WriteLine($"client {Context.ConnectionId} request ID: {requestId}");
            Console.WriteLine($"client {Context.ConnectionId} goes to ASRS: {asrsInstanceId}");
            //Console.WriteLine($"client{_pairing.Count()} userId: {SyncServer.UserId(this)}");
            var clientInfo = new ClientInfo()
            {
                ConnectionId = Context.ConnectionId,
                RequestId = requestId,
                ASRSInstance = asrsInstanceId
            };

            _pairing.AddClient(clientInfo);

            if (_pairing.TryGetAll(clientInfo, out List<ClientInfo> clientList))
            {
                if (clientList.Count == 1)
                {
                    // Send sticky information to the clients. For the secondary client, it will ignore this.
                    await _syncServer.GetStickyConnectionInfo(this);
                }
                else if (clientList.Count == 2)
                {
                    // Two clients with the same sticky information
                    Console.WriteLine("Succesfully see two clients are connected to this hub");
                }
            }
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.HubConnected, Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var requestId = SyncServer.ServiceRequestId(this);
            var asrsInstanceId = SyncServer.ASRSInstanceId(this);
            var clientInfo = new ClientInfo()
            {
                ConnectionId = Context.ConnectionId,
                RequestId = requestId,
                ASRSInstance = asrsInstanceId
            };
            _pairing.RemoveClient(clientInfo);
            if (_pairing.TryGetAll(clientInfo, out List<ClientInfo> clientList))
            {
                if (clientList.Count == 1)
                {
                    // A client of the pairing has dropped, but the other one is alive,
                    // we need to inform the online client.
                    var onlineClient = clientList.ToArray()[0];
                    await Clients.Client(onlineClient.ConnectionId).SendAsync(ClientSyncConstants.ClientPartnerDropped, Context.ConnectionId);
                }
            }
            Console.WriteLine($"Connection {Context.ConnectionId} is dropped");
        }

        public async Task RequestAccess(RequestAccessData payload)
        {
            var iClientProxy = Clients.Client(Context.ConnectionId);
            await _syncServer.HandleRequest(this, payload);
        }

        public async Task ResponseAccess(ResponseToRequestAccessData payload)
        {
            var iClientProxy = Clients.Client(Context.ConnectionId);
            await _syncServer.HandleResponse(this, payload);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.JoinedGroup);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.LeftGroup);
        }
    }
}
