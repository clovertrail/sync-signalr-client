using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace common.ClientAPI
{
    public class HubConnectionHelpers
    {
        public static Task JoinNotificationGroupAfterConnected(HubConnection hubConnection, string groupName, string userId)
        {
            return JoinNotificationGroupAfterConnectedCore(hubConnection, groupName, userId, false);
        }

        public static Task RequestAccessTokenAfterJoinNotificationGroup(HubConnection hubConnection, string groupName, string userId)
        {
            return JoinNotificationGroupAfterConnectedCore(hubConnection, groupName, userId, true);
        }

        public static Task ProvideTransportHubInfo(HubConnection hubConnection, IDictionary<string, string> transportHubInfo)
        {
            hubConnection.On<IDictionary<string, string>>(ClientSyncConstants.RequestConnectToTransportHub, async (payload) =>
            {
                // Assume the 1st connection has already obtained hub connection info.
                // merge with the previous 1st connection's info.
                foreach (var val in transportHubInfo)
                {
                    payload.Add(val.Key, val.Value);
                }
                await hubConnection.SendAsync(ClientSyncConstants.GroupBroadcast, ClientSyncConstants.ResponseType, payload);
            });
            return Task.CompletedTask;
        }

        private static Task JoinNotificationGroupAfterConnectedCore(HubConnection hubConnection, string groupName, string userId, bool requestToken)
        {
            hubConnection.On<string>(ClientSyncConstants.HubConnected, (connectionId) =>
            {
                var selfConnectionId = connectionId;
                Console.WriteLine($"connection Id {selfConnectionId}");
                hubConnection.SendAsync(ClientSyncConstants.JoinGroup, groupName);
            });
            hubConnection.On(ClientSyncConstants.JoinedGroup, async () =>
            {
                Console.WriteLine("Joined group");
                if (requestToken)
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
            return Task.CompletedTask;
        }
    }
}
