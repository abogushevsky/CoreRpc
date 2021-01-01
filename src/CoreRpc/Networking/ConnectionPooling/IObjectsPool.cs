using System;
using System.Threading.Tasks;

namespace CoreRpc.Networking.ConnectionPooling
{
    public interface IObjectsPool
    {
        Task CleanupStaleObjects();
    }
    
    public interface IObjectsPool<T> : IObjectsPool, IDisposable
    {
        Task<T> Acquire();

        // TODO: Make async
        void Release(T item);
    }
}