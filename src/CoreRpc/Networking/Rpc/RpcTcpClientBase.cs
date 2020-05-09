using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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

		public void Send(byte[] data) => ConnectAndSendAsync(serviceCallResult => 0, data).Wait();

		public byte[] SendAndReceive(byte[] data) =>
			ConnectAndSendAsync(serviceCallResult => serviceCallResult.ReturnValue, data).Result;

		public Task SendAsync(byte[] data) => ConnectAndSendAsync(serviceCallResult => 0, data);

		public Task<byte[]> SendAndReceiveAsync(byte[] data) =>
			ConnectAndSendAsync(serviceCallResult => serviceCallResult.ReturnValue, data);
		
		public void Dispose()
		{
			try
			{
				_tcpClientSemaphoreSlim.Wait(_closeTimeout);
				if (_tcpClient.Connected)
				{
					ExceptionsHandlingHelper.ExecuteWithExceptionLogging(
						() => SendDataAsync(
							_networkStream, 
							NetworkConstants.EndOfSessionMessageBytes).Wait(),
						() =>
						{
							// ReSharper disable once AccessToDisposedClosure - synchronous code
							_tcpClient.Close();
							_networkStream.Dispose();
						},
						_logger);
				}

				_tcpClient.Dispose();
			}
			finally
			{
				_tcpClientSemaphoreSlim.Release();
			}
		}

		private async Task<TResponse> ConnectAndSendAsync<TResponse>(
			Func<ServiceCallResult, TResponse> doWithServiceCallResult,
			byte[] data) =>
			await DoWithConnectedTcpClientAsync(
					async tcpClient => doWithServiceCallResult(
						await SendDataAndGetResultAsync(tcpClient, data))).ConfigureAwait(false);

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

		private static async Task SendDataAsync(Stream networkStream, byte[] data) => 
			await networkStream.WriteMessageAsync(data);

		private async Task<TResult> DoWithConnectedTcpClientAsync<TResult>(
			Func<Stream, Task<TResult>> doWithNetworkStream)
		{
			try
			{
				await _tcpClientSemaphoreSlim.WaitAsync();
				if (!_tcpClient.Connected)
				{
					await _tcpClient.ConnectAsync(_hostName, _port);
					_networkStream = await GetNetworkStreamFromTcpClientAsync(_tcpClient);
				}

				return await doWithNetworkStream(_networkStream);
			}
			catch (SocketException socketException)
			{
				_logger.LogError(socketException);
				throw new CoreRpcCommunicationException(
					$"Error dispatching a call to the server: {socketException.Message}");
			}
			finally
			{
				_tcpClientSemaphoreSlim.Release();
			}
		}

		protected abstract Task<Stream> GetNetworkStreamFromTcpClientAsync(TcpClient tcpClient);

		protected readonly string _hostName;
		
		private readonly int _port;
		private readonly ILogger _logger;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
		private readonly TcpClient _tcpClient;
		private Stream _networkStream;
		private readonly SemaphoreSlim _tcpClientSemaphoreSlim = new SemaphoreSlim(1);
		// TODO: Make configurable
		private readonly TimeSpan _closeTimeout = TimeSpan.FromSeconds(DefaultCloseTimeoutSeconds);

		private const int DefaultCloseTimeoutSeconds = 30;
	}
}