using System;
using System.Collections.Generic;
using System.Text;

namespace common
{
    public class ClientSyncConstants
    {
        // client callback methods
        public const string TransportHubInfo = "TransportHubInfo";
        public const string HubConnected = "HubConnected";
        public const string JoinedGroup = "JoinedGroupConfirmation";
        public const string LeftGroup = "LeftGroupConfirmation";
        public const string RequestConnectToTransportHub = "RequestConnectToTransportHub";
        public const string ResponseToTargetUrlAccessToken = "ResponseToTargetUrlAccessToken";
        public const string ErrorHandler = "Error";

        // server hub method
        public const string JoinGroup = "JoinGroup";
        public const string LeaveGroup = "LeaveGroup";
        public const string RequestAccess = "RequestAccess";
        public const string ResponseAccess = "ResponseAccess";
    }
}
