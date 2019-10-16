using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.sync
{
    /**
     * For simplicity purpose, a Dictionary<string, string> is used to sync data between clients and server hub.
     * A series of "demo.sync.xxxx" is defined.
     */
    public class SyncServer
    {
        private SyncClient.ServiceEndpoint _serviceEndpoint;
        private readonly IServerNameProvider _serverNameProvider;

        public SyncServer(IOptions<ServiceOptions> options, IServerNameProvider serverNameProvider)
        {
            _serverNameProvider = serverNameProvider;
            _serviceEndpoint = new SyncClient.ServiceEndpoint(options.Value.ConnectionString);
        }

        public static async Task<bool> RequestParamValidator(IClientProxy iClient, IDictionary<string, string> payload)
        {
            if (!payload.TryGetValue("demo.sync.2ndclient.userid", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter 'demo.sync.2ndclient.userid'");
                return false;
            }
            if (!payload.TryGetValue("demo.sync.client.groupname", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter 'demo.sync.client.groupname'");
                return false;
            }
            return true;
        }

        public static async Task<bool> ResponseParamValidator(IClientProxy iClient, IDictionary<string, string> payload)
        {
            if (!payload.TryGetValue("demo.sync.2ndclient.userid", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'demo.sync.2ndclient.userid'");
                return false;
            }
            if (!payload.TryGetValue("demo.sync.1stclient.server", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'demo.sync.1stclient.server'");
                return false;
            }
            if (!payload.TryGetValue("demo.sync.1stclient.hub", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'demo.sync.1stclient.hub'");
                return false;
            }
            if (!payload.TryGetValue("demo.sync.1stclient.request_id", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'demo.sync.1stclient.request_id'");
                return false;
            }
            return true;
        }

        public static string ServiceStickyId(Hub hub)
        {
            return hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
        }

        public static string ASRSInstanceId(Hub hub)
        {
            return hub.Context.GetHttpContext().Request.Headers["Asrs-Instance-Id"];
        }

        public async Task GetStickyConnectionInfo(Hub hub)
        {
            var serverName = _serverNameProvider.GetName();
            var clientRequestId = hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
            // Only the first connected client will get the sticky information
            var dic = new Dictionary<string, string>()
                {
                    { "demo.sync.1stclient.server", serverName },
                    { "demo.sync.1stclient.request_id", clientRequestId},
                    { "demo.sync.1stclient.hub", "transportHub"}
                };

            await hub.Clients.Client(hub.Context.ConnectionId).SendAsync(ClientSyncConstants.TransportHubInfo, dic);
        }

        public static async Task HandleRequest(Hub hub, IDictionary<string, string> payload)
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
            payload["demo.sync.2ndclient.connection_id"] = hub.Context.ConnectionId;

            await hub.Clients.Group(payload["demo.sync.client.groupname"]).SendAsync(ClientSyncConstants.RequestConnectToTransportHub, payload);
        }

        public async Task HandleResponse(Hub hub, IDictionary<string, string> payload)
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
             *   "demo.sync.1stclient.hub_url":"https://xxxxx/targethub",
             *   "demo.sync.1stclient.hub":"targethub"
             * }
             */

            if (!await SyncServer.ResponseParamValidator(iClientProxy, payload))
            {
                return;
            }
            var claims = Helpers.BuildClaims(
                payload["demo.sync.2ndclient.userid"],
                payload["demo.sync.1stclient.server"]);
            var hubName = payload["demo.sync.1stclient.hub"];
            var requestId = payload["demo.sync.1stclient.request_id"];
            payload["demo.sync.2ndclient.hub_url"] = Helpers.GenerateClientEndpoint(_serviceEndpoint, hubName, requestId);
            payload["demo.sync.2ndclient.access_key"] = Helpers.GenerateClientAccessToken(
                _serviceEndpoint,
                hubName,
                claims,
                TimeSpan.FromDays(1),
                requestId);
            await hub.Clients.Client(payload["demo.sync.2ndclient.connection_id"]).SendAsync(ClientSyncConstants.ResponseToTargetUrlAccessToken, payload);
        }
    }
}
