using System;
using System.IO;
using System.Net.Sockets;
using CoreRpc.Logging;
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
		
		private TResponse ConnectAndSend<TResponse>(Func<ServiceCallResult, TResponse> doWithServiceCallResult, byte[] data) => 
			DoWithConnectedTcpClient(tcpClient => doWithServiceCallResult(SendDataAndGetResult(tcpClient, data)));

		private ServiceCallResult SendDataAndGetResult(Stream networkStream, byte[] data)
		{			
			networkStream.WriteMessage(data);

			var serviceCallResult = _serviceCallResultSerializer.Deserialize(networkStream.ReadMessage());
			if (serviceCallResult.HasException)
			{
				throw ExceptionsSerializer.Instance.Deserialize(serviceCallResult.Exception);
			}

			return serviceCallResult;
		}

		private TResult DoWithConnectedTcpClient<TResult>(Func<Stream, TResult> doWithNetworkStream)
		{
			lock (_tcpClientSyncRoot)
			{
				if (!_tcpClient.Connected)
				{
					_tcpClient.Connect(_hostName, _port);
					_networkStream = GetNetworkStreamFromTcpClient(_tcpClient);
				}

				return doWithNetworkStream(_networkStream);
			}
		}

		protected abstract Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient);
		
		public void Dispose()
		{
			if (_tcpClient.Connected)
			{
				// TODO: Execute safely
				ExceptionsHandlingHelper.ExecuteWithExceptionLogging(
					() => SendDataAndGetResult(_networkStream, NetworkConstants.EndOfSessionMessageBytes),
					_logger);
				ExceptionsHandlingHelper.ExecuteWithExceptionLogging(
					() =>
					{
						_networkStream.Dispose();
						_tcpClient.Close();
					},
					_logger);
			}	
			
			_tcpClient.Dispose();
		}
		
		protected readonly string _hostName;
		
		private readonly int _port;
		private readonly ILogger _logger;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
		private readonly TcpClient _tcpClient;
		private Stream _networkStream;
		private readonly object _tcpClientSyncRoot = new object();
	}
}