using common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using SyncClient;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SignalRChat.Hubs
{
    public class NotificationHub : Hub
    {
        public Pairing<NotificationHub> _pairing;
        public SyncClient.ServiceEndpoint _serviceEndpoint;

        public NotificationHub(SyncClient.ServiceEndpoint serviceEndpint, Pairing<NotificationHub> pairing)
        {
            _serviceEndpoint = serviceEndpint;
            _pairing = pairing;
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

            if (requestType == ClientSyncConstants.RequestType)
            {
                // 2nd client --> 1st client : I want to connect to TransportHub
                /**
                 * payload has 
                 * {
                 *   "asrs.sync.client.groupname":"mySyncGroup",
                 *   "asrs.sync.2ndclient.userid":"2ndclient",
                 *   "asrs.sync.2ndclient.connection_id":"xxx"
                 * }
                 */
                if (!payload.TryGetValue("asrs.sync.2ndclient.userid", out _))
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("Error", "Missing the parameter 'asrs.sync.2ndclient.userid'");
                    return;
                }
                if (!payload.TryGetValue("asrs.sync.client.groupname", out _))
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("Error", "Missing the parameter 'asrs.sync.client.groupname'");
                    return;
                }
                payload["asrs.sync.2ndclient.connection_id"] = Context.ConnectionId;

                await Clients.Group(payload["asrs.sync.client.groupname"]).SendAsync(ClientSyncConstants.RequestConnectToTransportHub, payload);
            }
            if (requestType == ClientSyncConstants.ResponseType)
            {
                // 1st client --> 2nd client : AccessToken and RedirectURL
                /**
                 * payload has 
                 * {
                 *   "asrs.sync.client.groupname":"mySyncGroup",
                 *   "asrs.sync.2ndclient.userid":"2ndclient",
                 *   "asrs.sync.2ndclient.connection_id":"xxx",
                 *   "asrs.sync.1stclient.request_id":"xxxxx",
                 *   "asrs.sync.1stclient.server":"yyyyy",
                 *   "asrs.sync.1stclient.hub_url":"https://xxxxx/targethub",
                 *   "asrs.sync.1stclient.hub":"targethub"
                 * }
                 */
                if (!payload.TryGetValue("asrs.sync.2ndclient.userid", out _))
                {
                    Console.WriteLine($"Missing parameter for 'asrs.sync.2ndclient.userid'");
                }
                if (!payload.TryGetValue("asrs.sync.1stclient.server", out _))
                {
                    Console.WriteLine($"Missing parameter for 'asrs.sync.1stclient.server'");
                }
                if (!payload.TryGetValue("asrs.sync.1stclient.hub", out _))
                {
                    Console.WriteLine($"Missing parameter for 'asrs.sync.1stclient.hub'");
                }
                if (!payload.TryGetValue("asrs.sync.1stclient.request_id", out _))
                {
                    Console.WriteLine($"Missing parameter for 'asrs.sync.1stclient.request_id'");
                }
                var claims = BuildClaims(
                    payload["asrs.sync.2ndclient.userid"],
                    payload["asrs.sync.1stclient.server"]);
                var hubName = payload["asrs.sync.1stclient.hub"];
                payload["asrs.sync.2ndclient.hub_url"] = GenerateClientEndpoint(hubName);
                payload["asrs.sync.2ndclient.access_key"] = GenerateClientAccessToken(
                    hubName,
                    claims,
                    TimeSpan.FromDays(1),
                    payload["asrs.sync.1stclient.request_id"]);
                await Clients.Client(payload["asrs.sync.2ndclient.connection_id"]).SendAsync(ClientSyncConstants.ResponseToTargetUrlAccessToken, payload);
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
            if (_pairing.Count() != 2)
            {
                // make sure the 1st and 2nd clients are connected.
                Console.WriteLine($"The pairing connections have not been built since the connection count: {_pairing.Count()}");
                return false;
            }
            return true;
        }

        private IEnumerable<Claim> BuildClaims(string userId, string stickyServer)
        {
            var claimList = new List<Claim>();

            claimList.Add(new Claim("asrs.s.ssticky", "Required"));
            if (userId != null)
            {
                claimList.Add(new Claim("asrs.s.uid", userId));
            }
            if (stickyServer != null)
            {
                claimList.Add(new Claim("asrs.s.sn", stickyServer));
            }
            return claimList;
        }

        private string GenerateClientAccessToken(string hubName, IEnumerable<Claim> claims, TimeSpan lifetime, string requestId)
        {
            var audience = $"{_serviceEndpoint.Endpoint}/client/?hub={hubName}";
            return AuthenticationHelper.GenerateAccessToken(_serviceEndpoint.AccessKey, audience, claims, lifetime, requestId);
        }

        private string GenerateClientEndpoint(string hubName)
        {
            var str = $"{_serviceEndpoint.Endpoint}/client/?hub={hubName}";
            return str;
        }
    }
}
