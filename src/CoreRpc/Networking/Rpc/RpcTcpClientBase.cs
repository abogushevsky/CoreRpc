using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.ConnectionPooling;
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
			IObjectsPoolsRegistrar poolsRegistrar,
			IDateTimeProvider dateTimeProvider,
			ISerializerFactory serializerFactory,
			ILogger logger,
			ClientParameters parameters)
		{
			HostName = hostName;
			_port = port;
			if (parameters != null)
			{
				_connectionsPool = new ObjectsPool<TcpClientHolder>(
					new PooledItemManager<TcpClientHolder>(CreateTcpClient, CloseTcpClient),
					poolsRegistrar,
					dateTimeProvider,
					logger);
			}
			else
			{
				_connectionsPool = new ObjectsPool<TcpClientHolder>(
					new PooledItemManager<TcpClientHolder>(CreateTcpClient, CloseTcpClient),
					poolsRegistrar,
					dateTimeProvider,
					logger,
					parameters.ConnectionPoolSize);
			}

				_logger = logger;
			_serviceCallResultSerializer = serializerFactory.CreateSerializer<ServiceCallResult>();
		}

		public void Send(byte[] data) => ConnectAndSend(serviceCallResult => 0, data);

		public byte[] SendAndReceive(byte[] data) =>
			ConnectAndSend(serviceCallResult => serviceCallResult.ReturnValue, data);

		public async Task SendAsync(byte[] data) => 
			await ConnectAndSendAsync(serviceCallResult => 0, data)
				.ConfigureAwait(false);

		public async Task<byte[]> SendAndReceiveAsync(byte[] data) =>
			await ConnectAndSendAsync(serviceCallResult => serviceCallResult.ReturnValue, data)
				.ConfigureAwait(false);
		
		public void Dispose()
		{
			_connectionsPool.Dispose();
		}

		private async Task<TcpClientHolder> CreateTcpClient()
		{
			var tcpClient = new TcpClient();
			await tcpClient.ConnectAsync(HostName, _port);
			var networkStream = await GetNetworkStreamFromTcpClientAsync(tcpClient);
			return new TcpClientHolder(tcpClient, networkStream);
		}

		private async Task CloseTcpClient(TcpClientHolder tcpClientHolder)
		{
			if (tcpClientHolder.TcpClient.Connected)
			{
				await ExceptionsHandlingHelper.ExecuteWithExceptionLogging(
					() => SendDataAsync(
						tcpClientHolder.NetworkStream, 
						NetworkConstants.EndOfSessionMessageBytes),
					async () =>
					{
						// ReSharper disable once AccessToDisposedClosure - synchronous code
						tcpClientHolder.TcpClient.Close();
						await tcpClientHolder.NetworkStream.DisposeAsync();
					},
					_logger);
			}

			tcpClientHolder.TcpClient.Dispose();
		}

		private TResponse ConnectAndSend<TResponse>(
			Func<ServiceCallResult, TResponse> doWithServiceCallResult,
			byte[] data) => 
			DoWithConnectedTcpClient(
				tcpClient => doWithServiceCallResult(SendDataAndGetResult(tcpClient, data)));

		private async Task<TResponse> ConnectAndSendAsync<TResponse>(
			Func<ServiceCallResult, TResponse> doWithServiceCallResult,
			byte[] data) =>
			await DoWithConnectedTcpClientAsync(
					async tcpClient => doWithServiceCallResult(
						await SendDataAndGetResultAsync(tcpClient, data))).ConfigureAwait(false);
		
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

		private async Task<ServiceCallResult> SendDataAndGetResultAsync(Stream networkStream, byte[] data)
		{			
			await SendDataAsync(networkStream, data);

			var messageFromServer = await networkStream.ReadMessageAsync();

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
		
		private static void SendData(Stream networkStream, byte[] data) => 
			networkStream.WriteMessage(data);

		private static async Task SendDataAsync(Stream networkStream, byte[] data) => 
			await networkStream.WriteMessageAsync(data);
		
		private TResult DoWithConnectedTcpClient<TResult>(Func<Stream, TResult> doWithNetworkStream)
		{
			TcpClientHolder tcpClientHolder = null;
			try
			{
				tcpClientHolder = _connectionsPool.Acquire().Result;
				return doWithNetworkStream(tcpClientHolder.NetworkStream);
			}
			catch (SocketException socketException)
			{
				_logger.LogError(socketException);
				throw new CoreRpcCommunicationException(
					$"Error dispatching a call to the server: {socketException.Message}");
			}
			finally
			{
				if (tcpClientHolder != null)
				{
					_connectionsPool.Release(tcpClientHolder);
				}
			}
		}

		private async Task<TResult> DoWithConnectedTcpClientAsync<TResult>(
			Func<Stream, Task<TResult>> doWithNetworkStream)
		{
			TcpClientHolder tcpClientHolder = null;
			try
			{
				tcpClientHolder = await _connectionsPool.Acquire();
				return await doWithNetworkStream(tcpClientHolder.NetworkStream);
			}
			catch (SocketException socketException)
			{
				_logger.LogError(socketException);
				throw new CoreRpcCommunicationException(
					$"Error dispatching a call to the server: {socketException.Message}");
			}
			finally
			{
				if (tcpClientHolder != null)
				{
					_connectionsPool.Release(tcpClientHolder);
				}
			}
		}

		protected abstract Task<Stream> GetNetworkStreamFromTcpClientAsync(TcpClient tcpClient);

		protected readonly string HostName;
		
		private readonly int _port;
		private readonly IObjectsPool<TcpClientHolder> _connectionsPool;
		private readonly ILogger _logger;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;

		private class TcpClientHolder
		{
			public TcpClientHolder(TcpClient tcpClient, Stream networkStream)
			{
				TcpClient = tcpClient;
				NetworkStream = networkStream;
			}

			public TcpClient TcpClient { get; }
			
			public Stream NetworkStream { get; }
		}
	}
}