using System;
using System.ServiceModel;

namespace CommonWcfComponents
{
    public class ServiceHostContainer<TContract> : IDisposable
    {
        internal ServiceHostContainer(ServiceHost host, TContract instance)
        {
            _host = host;
            Instance = instance;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public TContract Instance { get; }

        private readonly ServiceHost _host;

        #region IDisposable

        public void Dispose()
        {
            ((IDisposable) _host)?.Dispose();
        }

        #endregion
    }
}
