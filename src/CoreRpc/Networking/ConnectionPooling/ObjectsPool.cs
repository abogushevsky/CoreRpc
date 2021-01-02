using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal class ObjectsPool<T> : IObjectsPool<T>
    {
        public ObjectsPool(
            PooledItemManager<T> itemManager,
            IObjectsPoolsRegistrar poolsRegistrar,
            IDateTimeProvider dateTimeProvider,
            int capacity = DefaultCapacity,
            int gracefulCompletionTimeoutSeconds = DefaultGracefulCompletionTimeoutSeconds) :
            this(
                itemManager,
                poolsRegistrar,
                TimeSpan.FromSeconds(DefaultLifetimeSeconds),
                dateTimeProvider,
                capacity,
                gracefulCompletionTimeoutSeconds)
        {
        }

        public ObjectsPool(
            PooledItemManager<T> itemManager,
            IObjectsPoolsRegistrar poolsRegistrar,
            TimeSpan lifetime,
            IDateTimeProvider dateTimeProvider,
            int capacity = DefaultCapacity,
            int gracefulCompletionTimeoutSeconds = DefaultGracefulCompletionTimeoutSeconds)
        {
            _itemManager = itemManager;
            _poolsRegistrar = poolsRegistrar;
            _lifetime = lifetime;
            _dateTimeProvider = dateTimeProvider;
            _capacity = capacity;
            _gracefulCompletionTimeoutSeconds = gracefulCompletionTimeoutSeconds;
            _semaphore = new SemaphoreSlim(capacity);

            _poolsRegistrar.RegisterPool(this);
        }

        public async Task<T> Acquire()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("This ObjectsPool instance has been disposed");
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            PooledItem item;
            if (_freeClients.TryPop(out var pooled) && IsActual(pooled))
            {
                item = pooled;
            }
            else
            {
                if (pooled != null)
                {
#pragma warning disable 4014 - we don't want to wait until cleanup is completed
                    Cleanup(pooled);
#pragma warning restore 4014
                }

                item = new PooledItem(await _itemManager.Create().ConfigureAwait(false), GetExpirationDate());
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
                    _freeClients.Push(
                        new PooledItem(pooledItem.Item, GetExpirationDate()));
                }

                _semaphore.Release();
            }
            else
            {
                Trace.TraceError($"Pooled object for {item} not found");
            }
        }

        public async Task CleanupStaleObjects()
        {
            var staleObjects = new List<PooledItem>();
            var actualObjects = new List<PooledItem>();
            while (!_freeClients.IsEmpty)
            {
                if (_freeClients.TryPop(out var item))
                {
                    if (IsActual(item))
                    {
                        actualObjects.Add(item);
                    }
                    else
                    {
                        staleObjects.Add(item);
                    }
                }
            }

            _freeClients.PushRange(actualObjects.ToArray());

            foreach (var staleObject in staleObjects)
            {
                await Cleanup(staleObject).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _poolsRegistrar.Deregister(this);
            Cleanup(_freeClients).Wait();
            var semaphore = _semaphore;
            if (!SpinWait.SpinUntil(
                () => semaphore.CurrentCount == _capacity,
                TimeSpan.FromSeconds(_gracefulCompletionTimeoutSeconds)))
            {
                Trace.TraceError(
                    $"Busy objects were not released in {_gracefulCompletionTimeoutSeconds} seconds");
                Cleanup(_busyClients.Values).Wait();
            }

            _semaphore.Dispose();
        }
        
        private DateTime GetExpirationDate() => _lifetime == TimeSpan.MaxValue
            ? DateTime.MaxValue
            : _dateTimeProvider.GetCurrent().Add(_lifetime);

        private bool IsActual(PooledItem item) => _dateTimeProvider.GetCurrent() < item.ExpirationTime;

        private async Task Cleanup(IEnumerable<PooledItem> items)
        {
            foreach (var item in items)
            {
                await Cleanup(item).ConfigureAwait(false);
            }
        }

        private async Task Cleanup(PooledItem item)
        {
            try
            {
                await _itemManager.Cleanup(item.Item).ConfigureAwait(false);
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
        private readonly int _capacity;
        private readonly IObjectsPoolsRegistrar _poolsRegistrar;
        private readonly int _gracefulCompletionTimeoutSeconds;
        private volatile bool _isDisposed;

        private const int DefaultLifetimeSeconds = 60;
        private const int DefaultCapacity = 30;
        private const int DefaultGracefulCompletionTimeoutSeconds = 10;

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