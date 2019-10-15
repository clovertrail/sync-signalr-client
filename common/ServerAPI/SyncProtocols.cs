using common.SyncAPI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.SyncProtocol
{
    public class SyncProtocols
    {
        private SyncClient.ServiceEndpoint _serviceEndpoint;
        private readonly IServerNameProvider _serverNameProvider;

        public SyncProtocols(IOptions<ServiceOptions> options, IServerNameProvider serverNameProvider)
        {
            _serverNameProvider = serverNameProvider;
            _serviceEndpoint = new SyncClient.ServiceEndpoint(options.Value.ConnectionString);
        }

        public static async Task<bool> RequestParamValidator(IClientProxy iClient, IDictionary<string, string> payload)
        {
            if (!payload.TryGetValue("asrs.sync.2ndclient.userid", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter 'asrs.sync.2ndclient.userid'");
                return false;
            }
            if (!payload.TryGetValue("asrs.sync.client.groupname", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, "Missing the parameter 'asrs.sync.client.groupname'");
                return false;
            }
            return true;
        }

        public static async Task<bool> ResponseParamValidator(IClientProxy iClient, IDictionary<string, string> payload)
        {
            if (!payload.TryGetValue("asrs.sync.2ndclient.userid", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'asrs.sync.2ndclient.userid'");
                return false;
            }
            if (!payload.TryGetValue("asrs.sync.1stclient.server", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'asrs.sync.1stclient.server'");
                return false;
            }
            if (!payload.TryGetValue("asrs.sync.1stclient.hub", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'asrs.sync.1stclient.hub'");
                return false;
            }
            if (!payload.TryGetValue("asrs.sync.1stclient.request_id", out _))
            {
                await iClient.SendAsync(ClientSyncConstants.ErrorHandler, $"Missing parameter for 'asrs.sync.1stclient.request_id'");
                return false;
            }
            return true;
        }

        public static string ServiceStickyId(Hub hub)
        {
            return hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
        }

        public async Task GetStickyConnectionInfo(Hub hub)
        {
            var serverName = _serverNameProvider.GetName();
            var clientRequestId = hub.Context.GetHttpContext().Request.Query["asrs_request_id"];
            // Only the first connected client will get the sticky information
            var dic = new Dictionary<string, string>()
                {
                    { "asrs.sync.1stclient.server", serverName },
                    { "asrs.sync.1stclient.request_id", clientRequestId},
                    { "asrs.sync.1stclient.hub", "transportHub"}
                };

            await hub.Clients.Client(hub.Context.ConnectionId).SendAsync(ClientSyncConstants.TransportHubInfo, dic);
        }

        public static async Task HandleRequest(Hub hub, IDictionary<string, string> payload)
        {
            // 2nd client --> 1st client : I want to connect to TransportHub
            /**
             * payload has the following values:
             * {
             *   "asrs.sync.client.groupname":"mySyncGroup",
             *   "asrs.sync.2ndclient.userid":"2ndclient",
             *   "asrs.sync.2ndclient.connection_id":"xxx"
             * }
             */
            var iClientProxy = hub.Clients.Client(hub.Context.ConnectionId);
            if (!await SyncProtocols.RequestParamValidator(iClientProxy, payload))
            {
                return;
            }
            payload["asrs.sync.2ndclient.connection_id"] = hub.Context.ConnectionId;

            await hub.Clients.Group(payload["asrs.sync.client.groupname"]).SendAsync(ClientSyncConstants.RequestConnectToTransportHub, payload);
        }

        public async Task HandleResponse(Hub hub, IDictionary<string, string> payload)
        {
            var iClientProxy = hub.Clients.Client(hub.Context.ConnectionId);
            // 1st client --> 2nd client : AccessToken and RedirectURL
            /**
             * payload has the following values:
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

            if (!await SyncProtocols.ResponseParamValidator(iClientProxy, payload))
            {
                return;
            }
            var claims = Helpers.BuildClaims(
                payload["asrs.sync.2ndclient.userid"],
                payload["asrs.sync.1stclient.server"]);
            var hubName = payload["asrs.sync.1stclient.hub"];
            var requestId = payload["asrs.sync.1stclient.request_id"];
            payload["asrs.sync.2ndclient.hub_url"] = Helpers.GenerateClientEndpoint(_serviceEndpoint, hubName, requestId);
            payload["asrs.sync.2ndclient.access_key"] = Helpers.GenerateClientAccessToken(
                _serviceEndpoint,
                hubName,
                claims,
                TimeSpan.FromDays(1),
                requestId);
            await hub.Clients.Client(payload["asrs.sync.2ndclient.connection_id"]).SendAsync(ClientSyncConstants.ResponseToTargetUrlAccessToken, payload);
        }
    }
}
