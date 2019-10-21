using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace common
{
    public class ClientStatTracker<THub> where THub : Hub
    {
        private ConcurrentDictionary<string, List<ClientInfo>> _clientTracker;
        private long _count;

        public ClientStatTracker()
        {
            _clientTracker = new ConcurrentDictionary<string, List<ClientInfo>>();
        }

        public void AddClient(ClientInfo clientInfo)
        {
            var key = Key(clientInfo);
            _clientTracker.AddOrUpdate(key,
                new List<ClientInfo>() { clientInfo },
                (k, values) => { values.Add(clientInfo); return values; });
        }

        public void RemoveClient(ClientInfo clientInfo)
        {
            var key = Key(clientInfo);
            if (_clientTracker.ContainsKey(key))
            {
                _clientTracker.TryGetValue(key, out List<ClientInfo> values);
                values.RemoveAll(i => i.ConnectionId.Equals(clientInfo.ConnectionId));
            }
        }

        public bool TryGetAll(ClientInfo clientInfo, out List<ClientInfo> ret)
        {
            var key = Key(clientInfo);
            if (_clientTracker.ContainsKey(key))
            {
                _clientTracker.TryGetValue(key, out ret);
                return true;
            }
            else
            {
                ret = null;
                return false;
            }
        }

        public void Increase()
        {
            Interlocked.Add(ref _count, 1);
        }

        public long Count()
        {
            return Interlocked.Read(ref _count);
        }

        public void Decrease()
        {
            Interlocked.Decrement(ref _count);
        }

        public string Key(ClientInfo clientInfo)
        {
            return clientInfo.RequestId + "@" + clientInfo.ServerName + "/" + clientInfo.ASRSInstance;
        }
    }

    // Online Client information
    public class ClientInfo
    {
        public string ConnectionId { get; set; }
        public string RequestId { get; set; }
        public string ASRSInstance { get; set; }
        public string ServerName { get; set; }
    }
}
