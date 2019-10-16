using System;
using System.Collections.Generic;
using System.Text;

namespace common
{
    // The sticky information for primary client
    public class StickyPayloadData
    {
        public string ServerName { get; set; }
        public string RequestId { get; set; }
        public string HubName { get; set; } 
    }


    public class RequestAccessData
    {
        public string GroupName { get; set; }
        public string SecondaryClientUserId { get; set; }
        public string SecondaryClientConnectionId { get; set; }

    }

    public class ResponseToRequestAccessData
    {
        public RequestAccessData RequestAccessData { get; set; }
        public StickyPayloadData StickyPayloadData { get; set; }
    }

    public class AccessData
    {
        public string Endpoint { get; set; }
        public string AccessKey { get; set; }
    }
}
