using System;
using System.IO;
using System.Net.Sockets;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc.Exceptions;
using CoreRpc.Serialization;
using CoreRpc.Utilities;

namespace CoreRpc.Networking.Rpc
{
	public abstract class RpcTcpClientBase : IDisposable
	{
		protected RpcTcpClientBase(
			string hostName,
			int port,
			ISerializerFactory serializerFactory,
			ILogger logger)
		{
			_hostName = hostName;
			_port = port;
			_logger = logger;
			_serviceCallResultSerializer = serializerFactory.CreateSerializer<ServiceCallResult>();

			_tcpClient = new TcpClient();
		}

		// TODO: Rename Send and SendAndReceive
		
		public void Send(byte[] data) => ConnectAndSend(serviceCallResult => 0, data);

		public byte[] SendAndReceive(byte[] data) => 
			ConnectAndSend(serviceCallResult => serviceCallResult.ReturnValue, data);
		
		public void Dispose()
		{
			if (_tcpClient.Connected)
			{				
				ExceptionsHandlingHelper.ExecuteWithExceptionLogging(
					() => SendData(_networkStream, NetworkConstants.EndOfSessionMessageBytes),
					() =>
					{
						_tcpClient.Close();
						_networkStream.Dispose();
					},
					_logger);
			}

			_tcpClient.Dispose();
		}
		
		private TResponse ConnectAndSend<TResponse>(Func<ServiceCallResult, TResponse> doWithServiceCallResult, byte[] data) => 
			DoWithConnectedTcpClient(tcpClient => doWithServiceCallResult(SendDataAndGetResult(tcpClient, data)));

		private ServiceCallResult SendDataAndGetResult(Stream networkStream, byte[] data)
		{			
			SendData(networkStream, data);

			var messageFromServer = networkStream.ReadMessage();

			try
			{
				var serviceCallResult = _serviceCallResultSerializer.Deserialize(messageFromServer);
				if (serviceCallResult.HasException)
				{
					throw ExceptionsSerializer.Instance.Deserialize(serviceCallResult.Exception);
				}

				return serviceCallResult;
			}
			catch (Exception exception)
			{
				_logger.LogError(exception);
				throw new CoreRpcCommunicationException(
					$"Server doesn't provide a meanfull response. Original exception text {exception.Message}");
			}
		}

		private static void SendData(Stream networkStream, byte[] data) => networkStream.WriteMessage(data);

		private TResult DoWithConnectedTcpClient<TResult>(Func<Stream, TResult> doWithNetworkStream)
		{
			lock (_tcpClientSyncRoot)
			{
				try
				{
					if (!_tcpClient.Connected)
					{
						_tcpClient.Connect(_hostName, _port);
						_networkStream = GetNetworkStreamFromTcpClient(_tcpClient);
					}

					return doWithNetworkStream(_networkStream);
				}
				catch (SocketException socketException)
				{
					_logger.LogError(socketException);
					throw new CoreRpcCommunicationException($"Error dispatching a call to the server: {socketException.Message}");
				}
			}
		}

		protected abstract Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient);

		protected readonly string _hostName;
		
		private readonly int _port;
		private readonly ILogger _logger;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
		private readonly TcpClient _tcpClient;
		private Stream _networkStream;
		private readonly object _tcpClientSyncRoot = new object();
	}
}