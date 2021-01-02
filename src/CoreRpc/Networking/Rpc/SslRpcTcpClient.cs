using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.ConnectionPooling;
using CoreRpc.Serialization;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.Rpc
{
	internal sealed class SslRpcTcpClient : RpcTcpClientBase
	{
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			IObjectsPoolsRegistrar poolsRegistrar,
			IDateTimeProvider dateTimeProvider,
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			ILogger logger,
			ClientParameters parameters) : 
			base(hostName, port, poolsRegistrar, dateTimeProvider, serializerFactory, logger, parameters)
		{
			_serverCertificateValidationCallback = serverCertificateValidationCallback;
		}
		
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			IObjectsPoolsRegistrar poolsRegistrar,
			IDateTimeProvider dateTimeProvider,
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			LocalCertificateSelectionCallback clientCertificateSelectionCallback,
			ILogger logger,
			ClientParameters parameters) : 
			base(hostName, port, poolsRegistrar, dateTimeProvider, serializerFactory, logger, parameters)
		{
			_serverCertificateValidationCallback = serverCertificateValidationCallback;
			_clientCertificateSelectionCallback = clientCertificateSelectionCallback;
		}

		protected override async Task<Stream> GetNetworkStreamFromTcpClientAsync(TcpClient tcpClient)
		{
			var sslStream = new SslStream(
				tcpClient.GetStream(), 
				leaveInnerStreamOpen: false, 
				userCertificateValidationCallback: _serverCertificateValidationCallback, 
				userCertificateSelectionCallback: _clientCertificateSelectionCallback);
			
			await sslStream.AuthenticateAsClientAsync(HostName); // TODO: select client cert
			return sslStream;
		}

		private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;
		private readonly LocalCertificateSelectionCallback _clientCertificateSelectionCallback;
	}
}