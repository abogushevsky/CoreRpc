using System.Threading.Tasks;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal interface IObjectsPool<T>
    {
        Task<T> Acquire();

        void Release(T item);
    }
}