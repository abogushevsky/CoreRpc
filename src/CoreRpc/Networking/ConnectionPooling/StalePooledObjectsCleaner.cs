using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Logging;

namespace CoreRpc.Networking.ConnectionPooling
{
    public interface IObjectsPoolsRegistrar
    {
        void RegisterPool(IObjectsPool objectsPool);

        void Deregister(IObjectsPool objectsPool);
    }
    
    internal class StalePooledObjectsCleaner : IObjectsPoolsRegistrar, IDisposable 
    {
        public StalePooledObjectsCleaner(ILogger logger) : this(
            logger, 
            TimeSpan.FromSeconds(DefaultCleanupIntervalSeconds))
        {
            
        }
        
        public StalePooledObjectsCleaner(ILogger logger, TimeSpan cleanupInterval)
        {
            _logger = logger;
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
                _logger.LogError($"ObjectsPool {objectsPool} was not found in {PoolsToClean}");
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
                        _logger.LogError($"Failed to clean pool {pool}: {ex}");
                    }
                }
            }
        }

        private readonly ConcurrentDictionary<IObjectsPool, bool> PoolsToClean = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ILogger _logger;
        private readonly TimeSpan CleanupInterval;
        private volatile bool _isDisposed;

        private const int DefaultCleanupIntervalSeconds = 5;
    }
}