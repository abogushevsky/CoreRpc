using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public sealed class SslRpcTcpClient : RpcTcpClientBase
	{
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			bool doUseSingleConnection,
			RemoteCertificateValidationCallback serverCertificateValidationCallback) : 
			base(hostName, port, serializerFactory)
		{
			_serverCertificateValidationCallback = serverCertificateValidationCallback;
		}
		
		public SslRpcTcpClient(
			string hostName, 
			int port, 
			ISerializerFactory serializerFactory,
			bool doUseSingleConnection,
			RemoteCertificateValidationCallback serverCertificateValidationCallback,
			LocalCertificateSelectionCallback clientCertificateSelectionCallback) : 
			base(hostName, port, serializerFactory)
		{
			_serverCertificateValidationCallback = serverCertificateValidationCallback;
			_clientCertificateSelectionCallback = clientCertificateSelectionCallback;
		}

		protected override Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient)
		{
			var sslStream = new SslStream(
				tcpClient.GetStream(), 
				leaveInnerStreamOpen: false, 
				userCertificateValidationCallback: _serverCertificateValidationCallback, 
				userCertificateSelectionCallback: _clientCertificateSelectionCallback);
			
			sslStream.AuthenticateAsClient(_hostName); // TODO: select client cert
			return sslStream;
		}
		
		private readonly RemoteCertificateValidationCallback _serverCertificateValidationCallback;
		private readonly LocalCertificateSelectionCallback _clientCertificateSelectionCallback;
	}
}