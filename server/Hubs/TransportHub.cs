using common;
using common.SyncProtocol;
using Microsoft.AspNetCore.SignalR;
using SyncClient;
using System;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class TransportHub : Hub
    {
        private SyncProtocols _syncProtocols;
        private Pairing<TransportHub> _pairing;

        public TransportHub(SyncProtocols syncProtocols, Pairing<TransportHub> pairing )
        {
            _syncProtocols = syncProtocols;
            _pairing = pairing;
        }

        public override async Task OnConnectedAsync()
        {
            // Send sticky information to the client
            _pairing.Increase();
            // Show the client connection information
            Console.WriteLine($"client{_pairing.Count()} request ID: {SyncProtocols.ServiceStickyId(this)}");
            Console.WriteLine($"client{_pairing.Count()} goes to ASRS: {SyncProtocols.ASRSInstanceId(this)}");
            if (_pairing.Count() == 1)
            {
                await _syncProtocols.GetStickyConnectionInfo(this);
            }
            if (_pairing.Count() == 2)
            {
                Console.WriteLine("Succesfully see two clients are connected to this hub");
            }
            await Clients.Client(Context.ConnectionId).SendAsync(ClientSyncConstants.HubConnected, Context.ConnectionId);
        }
        
    }
}
