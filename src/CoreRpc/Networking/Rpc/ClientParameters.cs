namespace CoreRpc.Networking.Rpc
{
    public class ClientParameters
    {
        public ClientParameters(int connectionPoolSize)
        {
            ConnectionPoolSize = connectionPoolSize;
        }

        public int ConnectionPoolSize { get; }
    }
}