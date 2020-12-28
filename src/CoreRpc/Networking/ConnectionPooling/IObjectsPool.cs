using System.Threading.Tasks;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal interface IObjectsPool
    {
        Task CleanupStaleObjects();
    }
    
    internal interface IObjectsPool<T> : IObjectsPool
    {
        Task<T> Acquire();

        // TODO: Make async
        void Release(T item);
    }
}