using System;

namespace CoreRpc.Networking.Rpc.Exceptions
{
    public class CoreRpcCommunicationException : Exception
    {
        public CoreRpcCommunicationException(string message) : base(message)
        {
            
        }
    }
}