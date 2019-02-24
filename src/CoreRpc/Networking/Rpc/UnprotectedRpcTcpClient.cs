using System.IO;
using System.Net.Sockets;
using CoreRpc.Logging;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public sealed class UnprotectedRpcTcpClient : RpcTcpClientBase
	{
		public UnprotectedRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			ILogger logger) : base(hostName, port, serializerFactory, logger)
		{
		}

		protected override Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient) => tcpClient.GetStream();
	}
}