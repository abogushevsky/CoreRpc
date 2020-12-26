using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal class ObjectsPool<T> : IObjectsPool<T>
    {
        public ObjectsPool(Func<T> creator, IDateTimeProvider dateTimeProvider, int capacity = DEFAULT_CAPACITY) : 
            this(creator, TimeSpan.FromSeconds(DEFAULT_LIFETIME_SECONDS), dateTimeProvider, capacity)
        {
        }
        
        public ObjectsPool(
            Func<T> creator, 
            TimeSpan lifetime, 
            IDateTimeProvider dateTimeProvider, 
            int capacity = DEFAULT_CAPACITY)
        {
            _creator = creator;
            _lifetime = lifetime;
            _dateTimeProvider = dateTimeProvider;
            _semaphore = new SemaphoreSlim(capacity);
        }
        
        public async Task<T> Acquire()
        {
            await _semaphore.WaitAsync();
            var item = _freeClients.TryPop(out var pooled) && IsActual(pooled)
                ? pooled
                : new PooledItem(
                    _creator(), 
                    _lifetime == TimeSpan.MaxValue ? DateTime.MaxValue : _dateTimeProvider.GetCurrent().Add(_lifetime));
            _busyClients[item.Item] = item;
            return item.Item;
        }

        public void Release(T item)
        {
            if (_busyClients.TryRemove(item, out var pooledItem))
            {
                _freeClients.Push(pooledItem);
            }
            else
            {
                // TODO: Replace own logging with Trace or use own logger here
                Trace.TraceError($"Pooled object for {item} not found");
            }

            _semaphore.Release();
        }
        
        private bool IsActual(PooledItem item) => _dateTimeProvider.GetCurrent() < item.ExpirationTime;

        protected void Cleanup(T item)
        {
            
        }
        
        private readonly ConcurrentStack<PooledItem> _freeClients = new ConcurrentStack<PooledItem>();
        private readonly ConcurrentDictionary<T, PooledItem> _busyClients = new ConcurrentDictionary<T, PooledItem>();
        private readonly Func<T> _creator;
        private readonly TimeSpan _lifetime;
        private readonly SemaphoreSlim _semaphore;
        private readonly IDateTimeProvider _dateTimeProvider;
        
        private const int DEFAULT_LIFETIME_SECONDS = 30;
        private const int DEFAULT_CAPACITY = 10;

        private class PooledItem
        {
            public PooledItem(T item, DateTime expirationTime)
            {
                Item = item;
                ExpirationTime = expirationTime;
            }
            
            public T Item { get; }
            
            public DateTime ExpirationTime { get; }
        }
    }
}