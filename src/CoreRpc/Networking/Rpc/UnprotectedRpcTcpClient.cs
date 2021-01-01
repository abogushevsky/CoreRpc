using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.ConnectionPooling;
using CoreRpc.Serialization;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.Rpc
{
	internal sealed class UnprotectedRpcTcpClient : RpcTcpClientBase
	{
		public UnprotectedRpcTcpClient(
			string hostName, 
			int port, 
			IObjectsPoolsRegistrar poolsRegistrar,
			IDateTimeProvider dateTimeProvider,
			ISerializerFactory serializerFactory,
			ILogger logger) : base(hostName, port, poolsRegistrar, dateTimeProvider, serializerFactory, logger)
		{
		}
		
		protected override Task<Stream> GetNetworkStreamFromTcpClientAsync(TcpClient tcpClient) =>
			Task.FromResult(tcpClient.GetStream() as Stream);
	}
}