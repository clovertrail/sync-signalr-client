using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace common.sync
{
    /**
     * For simplicity purpose, a Dictionary<string, string> is used to sync data between clients and server hub.
     * A series of "demo.sync.xxxx" is defined.
     */
    public class SyncServer
    {
        private global::SyncClient.ServiceEndpoint _serviceEndpoint;
        private readonly IServerNameProvider _serverNameProvider;

        public SyncServer(IOptions<ServiceOptions> options, IServerNameProvider serverNameProvider)
        {
            _serverNameProvider = serverNameProvider;
            _serviceEndpoint = new global::SyncClient.ServiceEndpoint(options.Value.ConnectionString);
        }

        public static async Task<bool> RequestParamValidator(IClientProxy iClient, RequestAccessData payload)
        {
            if (String.IsNullOrEmpty(payload.SecondaryClientUserId))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter of secondary client's userid'");
                return false;
            }
            if (String.IsNullOrEmpty(payload.GroupName))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter of the group name for sync");
                return false;
            }
            return true;
        }

        public static async Task<bool> ResponseParamValidator(IClientProxy iClient, ResponseToRequestAccessData payload)
        {
            if (String.IsNullOrEmpty(payload.RequestAccessData.SecondaryClientUserId))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for secondary client's userid'");
                return false;
            }
            if (String.IsNullOrEmpty(payload.StickyPayloadData.ServerName))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for primary client's server");
                return false;
            }
            if (String.IsNullOrEmpty(payload.StickyPayloadData.HubName))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for transport hub");
                return false;
            }
            if (String.IsNullOrEmpty(payload.StickyPayloadData.RequestId))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for primary client's request_id");
                return false;
            }
            return true;
        }

        public static string ServiceRequestId(Hub hub)
        {
            return hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
        }

        public static string ASRSInstanceId(Hub hub)
        {
            return hub.Context.GetHttpContext().Request.Headers["Asrs-Instance-Id"];
        }

        public static string UserId(Hub hub)
        {
            return hub.Context.UserIdentifier;
        }

        public async Task GetStickyConnectionInfo(Hub hub)
        {
            var serverName = _serverNameProvider.GetName();
            var clientRequestId = hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
            // Only the first connected client will get the sticky information
            var data = new StickyPayloadData()
            {
                HubName = "transportHub",
                RequestId = clientRequestId,
                ServerName = serverName
            };
            await hub.Clients.Client(hub.Context.ConnectionId).SendAsync(ClientSyncConstants.TransportHubInfo, data);
        }

        public async Task HandleRequest(Hub hub, RequestAccessData payload)
        {
            // 2nd client --> 1st client : I want to connect to TransportHub
            /**
             * payload has the following values:
             * {
             *   "demo.sync.client.groupname":"mySyncGroup",
             *   "demo.sync.2ndclient.userid":"2ndclient",
             *   "demo.sync.2ndclient.connection_id":"xxx"
             * }
             */
            var iClientProxy = hub.Clients.Client(hub.Context.ConnectionId);
            if (!await SyncServer.RequestParamValidator(iClientProxy, payload))
            {
                return;
            }
            payload.SecondaryClientConnectionId = hub.Context.ConnectionId;
            await hub.Clients.Group(payload.GroupName).SendAsync(ClientSyncConstants.RequestConnectToTransportHub, payload);
        }

        public async Task HandleResponse(Hub hub, ResponseToRequestAccessData payload)
        {
            var iClientProxy = hub.Clients.Client(hub.Context.ConnectionId);
            // 1st client --> 2nd client : AccessToken and RedirectURL
            /**
             * payload has the following values:
             * {
             *   "demo.sync.client.groupname":"mySyncGroup",
             *   "demo.sync.2ndclient.userid":"2ndclient",
             *   "demo.sync.2ndclient.connection_id":"xxx",
             *   "demo.sync.1stclient.request_id":"xxxxx",
             *   "demo.sync.1stclient.server":"yyyyy",
             *   "demo.sync.1stclient.hub":"targethub"
             * }
             */

            if (!await SyncServer.ResponseParamValidator(iClientProxy, payload))
            {
                return;
            }
            var claims = Helpers.BuildClaims(
                payload.RequestAccessData.SecondaryClientUserId,
                payload.StickyPayloadData.ServerName);
            var hubName = payload.StickyPayloadData.HubName;
            var requestId = payload.StickyPayloadData.RequestId;
            var endpoint = Helpers.GenerateClientEndpoint(_serviceEndpoint, hubName, requestId);
            var accessKey = Helpers.GenerateClientAccessToken(
                _serviceEndpoint,
                hubName,
                claims,
                TimeSpan.FromDays(1),
                requestId);
            var accessResponse = new AccessData()
            {
                Endpoint = endpoint,
                AccessKey = accessKey
            };
            await hub.Clients.Client(payload.RequestAccessData.SecondaryClientConnectionId).SendAsync(ClientSyncConstants.ResponseToTargetUrlAccessToken, accessResponse);
        }
    }
}
