using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace common.sync
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

        public static Task ProvideTransportHubInfo(HubConnection hubConnection, StickyPayloadData transportHubInfo)
        {
            hubConnection.On<RequestAccessData>(ClientSyncConstants.RequestConnectToTransportHub, async (payload) =>
            {
                // Assume the 1st connection has already obtained hub connection info.
                // merge with the previous 1st connection's info.
                var response = new ResponseToRequestAccessData()
                {
                    RequestAccessData = payload,
                    StickyPayloadData = transportHubInfo
                };
                await hubConnection.SendAsync(ClientSyncConstants.ResponseAccess, response);
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
                    var requestAccess = new RequestAccessData()
                    {
                        GroupName = groupName,
                        SecondaryClientUserId = userId
                    };
                    await hubConnection.SendAsync(ClientSyncConstants.RequestAccess, requestAccess);
                }
            });
            return Task.CompletedTask;
        }
    }
}
