using System.IO;
using System.Net.Sockets;
using Common.Infrastructure.Networking.Rpc;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public sealed class UnprotectedRpcTcpClient : RpcTcpClientBase
	{
		public UnprotectedRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory) : base(hostName, port, serializerFactory)
		{
		}

		protected override Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient) => tcpClient.GetStream();
	}
}