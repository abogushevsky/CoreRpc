using System;
using System.Threading.Tasks;

namespace CoreRpc.Networking.ConnectionPooling
{
    public class PooledItemManager<T>
    {
        public PooledItemManager(Func<Task<T>> create, Func<T, Task> cleanup)
        {
            Create = create;
            Cleanup = cleanup;
        }

        public Func<Task<T>> Create { get; }
        
        public Func<T, Task> Cleanup { get; }
    }
}