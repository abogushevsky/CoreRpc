using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	internal sealed class SslRpcTcpClient : RpcTcpClientBase
	{
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			ILogger logger) : 
			base(hostName, port, serializerFactory, logger)
		{
			_serverCertificateValidationCallback = serverCertificateValidationCallback;
		}
		
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			LocalCertificateSelectionCallback clientCertificateSelectionCallback,
			ILogger logger) : 
			base(hostName, port, serializerFactory, logger)
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
			
			await sslStream.AuthenticateAsClientAsync(_hostName); // TODO: select client cert
			return sslStream;
		}
		
		private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;
		private readonly LocalCertificateSelectionCallback _clientCertificateSelectionCallback;
	}
}