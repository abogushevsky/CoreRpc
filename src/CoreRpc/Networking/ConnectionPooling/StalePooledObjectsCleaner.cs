using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CoreRpc.Networking.ConnectionPooling
{
    internal interface IObjectsPoolsRegistrar
    {
        void RegisterPool(IObjectsPool objectsPool);

        void Deregister(IObjectsPool objectsPool);
    }
    
    internal class StalePooledObjectsCleaner : IObjectsPoolsRegistrar, IDisposable 
    {
        public StalePooledObjectsCleaner() : this(TimeSpan.FromSeconds(DefaultCleanupIntervalSeconds))
        {
            
        }
        
        public StalePooledObjectsCleaner(TimeSpan cleanupInterval)
        {
            CleanupInterval = cleanupInterval;
            Task.Factory.StartNew(
                Cleanup, 
                _cancellationTokenSource.Token, 
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void RegisterPool(IObjectsPool objectsPool) => PoolsToClean[objectsPool] = true;

        public void Deregister(IObjectsPool objectsPool)
        {
            if (!PoolsToClean.TryRemove(objectsPool, out _))
            {
                Trace.TraceError($"ObjectsPool {objectsPool} was not found in {PoolsToClean}");
            }
        }
        
        public void Dispose()
        {
            _isDisposed = true;
            _cancellationTokenSource.Cancel();
        }

        private async Task Cleanup()
        {
            while(!_isDisposed)
            {
                await Task.Delay(CleanupInterval, _cancellationTokenSource.Token);
                
                foreach (var pool in PoolsToClean.Keys)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    try
                    {
                        await pool.CleanupStaleObjects();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Failed to clean pool {pool}: {ex}");
                    }
                }
            }
        }

        private readonly ConcurrentDictionary<IObjectsPool, bool> PoolsToClean = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TimeSpan CleanupInterval;
        private volatile bool _isDisposed;

        private const int DefaultCleanupIntervalSeconds = 5;
    }
}