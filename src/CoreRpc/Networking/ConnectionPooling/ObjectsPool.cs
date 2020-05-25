using System;
using System.Collections.Generic;
using static CoreRpc.Utilities.ExecutionHelper;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal abstract class ObjectsPool<T>
    {
        public ObjectsPool(Func<T> creator) : this(creator, TimeSpan.FromSeconds(DEFAULT_LIFETIME_SECONDS))
        {
        }
        
        public ObjectsPool(Func<T> creator, TimeSpan lifetime)
        {
            _creator = creator;
            _lifetime = lifetime;
        }
        
        public T Acquire()
        {
            WithLock(_locker, () =>
            {

            });
            throw new System.NotImplementedException();
        }

        public void Release(T item)
        {
            throw new System.NotImplementedException();
        }

        protected abstract void Cleanup(T item);
        
        private readonly HashSet<T> _freeClients = new HashSet<T>();
        private readonly HashSet<T> _busyClients = new HashSet<T>();
        private readonly Func<T> _creator;
        private readonly TimeSpan _lifetime;
        private readonly object _locker = new object();
        
        private const int DEFAULT_LIFETIME_SECONDS = 30;
    }
}