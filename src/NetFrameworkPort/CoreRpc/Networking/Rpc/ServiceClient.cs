using System;

namespace CoreRpc.Networking.Rpc
{
    public sealed class ServiceClient<TService> : IDisposable
    {
        public ServiceClient(IDisposable rpcTcpClient, TService serviceInstance)
        {
            _rpcTcpClient = rpcTcpClient;
            ServiceInstance = serviceInstance;
        }
        
        public TService ServiceInstance { get; }

        public void Dispose()
        {
            _rpcTcpClient.Dispose();
        }

        private readonly IDisposable _rpcTcpClient;        
    }
}