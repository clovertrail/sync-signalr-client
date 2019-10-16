using Microsoft.Azure.SignalR;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;

namespace common.sync
{
    public class Helpers
    {
        public static IEnumerable<Claim> BuildClaims(string userId, string stickyServer)
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

        public static string GenerateClientAccessToken(
            global::SyncClient.ServiceEndpoint serviceEndpoint,
            string hubName,
            IEnumerable<Claim> claims,
            TimeSpan lifetime,
            string requestId)
        {
            var audience = $"{serviceEndpoint.Endpoint}/client/?hub={hubName}";
            return AuthenticationHelper.GenerateAccessToken(serviceEndpoint.AccessKey, audience, claims, lifetime, requestId);
        }

        public static string GenerateClientEndpoint(global::SyncClient.ServiceEndpoint serviceEndpoint, string hubName, string requestId)
        {
            var clientRequestId = WebUtility.UrlEncode(requestId);
            var str = $"{serviceEndpoint.Endpoint}/client/?hub={hubName}&{Constants.QueryParameter.ConnectionRequestId}={clientRequestId}";
            return str;
        }
    }
}
