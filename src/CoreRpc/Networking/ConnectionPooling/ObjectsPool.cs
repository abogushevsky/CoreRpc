using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal class ObjectsPool<T> : IObjectsPool<T>, IDisposable
    {
        public ObjectsPool(
            PooledItemManager<T> itemManager, 
            IDateTimeProvider dateTimeProvider, 
            int capacity = DefaultCapacity) : 
            this(itemManager, TimeSpan.FromSeconds(DefaultLifetimeSeconds), dateTimeProvider, capacity)
        {
        }
        
        public ObjectsPool(
            PooledItemManager<T> itemManager, 
            TimeSpan lifetime, 
            IDateTimeProvider dateTimeProvider, 
            int capacity = DefaultCapacity)
        {
            _itemManager = itemManager;
            _lifetime = lifetime;
            _dateTimeProvider = dateTimeProvider;
            _semaphore = new SemaphoreSlim(capacity);
        }
        
        public async Task<T> Acquire()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("This ObjectsPool instance has been disposed");
            }

            await _semaphore.WaitAsync();
            PooledItem item;
            if (_freeClients.TryPop(out var pooled) && IsActual(pooled))
            {
                item = pooled;
            }
            else
            {
                if (pooled != null)
                {
                    Cleanup(pooled.Item);
                }

                item = new PooledItem(
                    _itemManager.Create(),
                    _lifetime == TimeSpan.MaxValue
                        ? DateTime.MaxValue
                        : _dateTimeProvider.GetCurrent().Add(_lifetime));
            }
            _busyClients[item.Item] = item;
            return item.Item;
        }

        public void Release(T item)
        {
            if (_busyClients.TryRemove(item, out var pooledItem))
            {
                if (!_isDisposed)
                {
                    _freeClients.Push(pooledItem);
                }
                
                _semaphore.Release();
            }
            else
            {
                // TODO: Replace own logging with Trace or use own logger here
                Trace.TraceError($"Pooled object for {item} not found");
            }
        }
        
        public void Dispose()
        {
            _isDisposed = true;
            _freeClients.ForEach(item => Cleanup(item.Item));
            // TODO: Gracefully wait for completion of busy objects
        }
        
        private bool IsActual(PooledItem item) => _dateTimeProvider.GetCurrent() < item.ExpirationTime;

        private void Cleanup(T item)
        {
            try
            {
                _itemManager.Cleanup(item);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Exception occured during pooled item {item} cleanup: {ex}");
            }
        } 
        
        private readonly ConcurrentStack<PooledItem> _freeClients = new();
        private readonly ConcurrentDictionary<T, PooledItem> _busyClients = new();
        private readonly PooledItemManager<T> _itemManager;
        private readonly TimeSpan _lifetime;
        private readonly SemaphoreSlim _semaphore;
        private readonly IDateTimeProvider _dateTimeProvider;
        private volatile bool _isDisposed;
        
        private const int DefaultLifetimeSeconds = 30;
        private const int DefaultCapacity = 10;

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