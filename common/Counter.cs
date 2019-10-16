using Microsoft.AspNetCore.SignalR;
using System.Threading;

namespace common
{
    public class Counter<THub> where THub : Hub
    {
        private long _count;

        public Counter()
        {

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
    }
}
