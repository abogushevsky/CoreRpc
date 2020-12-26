using System;

namespace CoreRpc.Networking.ConnectionPooling
{
    public class PooledItemManager<T>
    {
        public PooledItemManager(Func<T> create, Action<T> cleanup)
        {
            Create = create;
            Cleanup = cleanup;
        }

        public Func<T> Create { get; }
        
        public Action<T> Cleanup { get; }
    }
}