using common;
using common.sync;
using Microsoft.AspNetCore.SignalR;
using SyncClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class NotificationHub : Hub
    {
        private Counter<NotificationHub> _pairing;
        private SyncServer _syncProtocols;

        public NotificationHub(Counter<NotificationHub> pairing, SyncServer syncProtocols)
        {
            _pairing = pairing;
            _syncProtocols = syncProtocols;
        }

        public override async Task OnConnectedAsync()
        {
            _pairing.Increase();
            Console.WriteLine($"Current connection count: {_pairing.Count()}");
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.HubConnected, Context.ConnectionId);
        }

        public async Task GroupBroadcast(int requestType, IDictionary<string, string> payload)
        {
            if (!PairingReady())
            {
                // ignore the request if pairing parties have not been connected.
                return;
            }
            var iClientProxy = Clients.Client(Context.ConnectionId);
            if (requestType == ClientSyncConstants.RequestType)
            {
                await SyncServer.HandleRequest(this, payload);
            }
            if (requestType == ClientSyncConstants.ResponseType)
            {
                await _syncProtocols.HandleResponse(this, payload);
            }
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

        private bool PairingReady()
        {
            if (_pairing.Count() < 2)
            {
                // make sure the 1st and 2nd clients are connected.
                Console.WriteLine($"The pairing connections have not been built since the connection count: {_pairing.Count()}");
                return false;
            }
            return true;
        }
    }
}
