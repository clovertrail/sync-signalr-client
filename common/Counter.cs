using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncClient
{
    public class Counter<THub> where THub : Hub
    {
        private long _Count;

        public Counter()
        {

        }

        public void Increase()
        {
            Interlocked.Add(ref _Count, 1);
        }

        public long Count()
        {
            return Interlocked.Read(ref _Count);
        }

        public void Decrease()
        {
            Interlocked.Decrement(ref _Count);
        }
    }
}
