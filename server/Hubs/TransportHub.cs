using common;
using common.sync;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class TransportHub : Hub
    {
        private SyncServer _syncServer;
        private Counter<TransportHub> _pairing;

        public TransportHub(SyncServer syncServer, Counter<TransportHub> pairing )
        {
            _syncServer = syncServer;
            _pairing = pairing;
        }

        public override async Task OnConnectedAsync()
        {
            // Send sticky information to the client
            _pairing.Increase();
            // Show the client connection information
            Console.WriteLine($"client{_pairing.Count()} request ID: {SyncServer.ServiceStickyId(this)}");
            Console.WriteLine($"client{_pairing.Count()} goes to ASRS: {SyncServer.ASRSInstanceId(this)}");
            if (_pairing.Count() == 1)
            {
                await _syncServer.GetStickyConnectionInfo(this);
            }
            if (_pairing.Count() == 2)
            {
                Console.WriteLine("Succesfully see two clients are connected to this hub");
            }
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.HubConnected, Context.ConnectionId);
        }
        
    }
}
