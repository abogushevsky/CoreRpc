using System;

namespace CoreRpc.Networking.Rpc.ServiceAnnotations
{
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(int port)
        {
            Port = port;
        }

        public int Port { get; }
    }
}