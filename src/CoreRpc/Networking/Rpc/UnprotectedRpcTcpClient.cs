using System.IO;
using System.Net.Sockets;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public sealed class UnprotectedRpcTcpClient : RpcTcpClientBase
	{
		public UnprotectedRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			bool doUseSingleConnection) : base(hostName, port, serializerFactory, doUseSingleConnection)
		{
		}

		protected override Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient) => tcpClient.GetStream();
	}
}