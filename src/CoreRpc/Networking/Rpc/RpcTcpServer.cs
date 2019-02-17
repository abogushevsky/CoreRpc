using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public class RpcTcpServer<TService> : IDisposable
	{
		public RpcTcpServer(
			TService serviceInstance, 
			TimeSpan serviceShutdownTimeout,
			ISerializerFactory serializerFactory,
			ILogger logger) : this(serviceInstance, serviceShutdownTimeout, null, serializerFactory, logger)
		{
		}
		
		public RpcTcpServer(
			TService serviceInstance, 
			TimeSpan serviceShutdownTimeout,
			ServerSslSettings sslSettings,
			ISerializerFactory serializerFactory,
			ILogger logger)
		{
			_serviceDispatcher = new ServiceDispatcher<TService>(serializerFactory, logger);
			_serviceInstance = serviceInstance;
			_serviceShutdownTimeout = serviceShutdownTimeout;
			_sslSettings = sslSettings;
			_logger = logger;
			_messageSerializer = serializerFactory.CreateSerializer<RpcMessage>();
			_serviceCallResultSerializer = serializerFactory.CreateSerializer<ServiceCallResult>();
			_listenerWaitHandle = new AutoResetEvent(false);
			_tcpListener = new TcpListener(IPAddress.Any, _serviceDispatcher.ServicePort);
		}

		public void Start()
		{
			_tcpListener.Start();
			Task.Factory.StartNew(async () => await AcceptClient(), TaskCreationOptions.LongRunning);
		}
		
		public void Dispose()
		{
			_logger.LogDebug($"Disposing server {typeof(TService).FullName}");
			_isDisposing = true;
			_tcpListener.Stop();
			
			if (!_listenerWaitHandle.WaitOne(_serviceShutdownTimeout))
			{
				throw new TimeoutException($"Server side close timeout in {typeof(TService).FullName}");
			}
		}

		private async Task AcceptClient()
		{
			try
			{
				while (!_isDisposing)
				{
					var client = await _tcpListener.AcceptTcpClientAsync();
					_logger.LogDebug($"{_serviceInstance.GetType()}: client connected. {client.Client.RemoteEndPoint}");
					
#pragma warning disable 4014
					Task.Factory.StartNew(async () =>
#pragma warning restore 4014
					{
						using (var handler = new ClientHandler(
							client, 
							_serviceInstance, 
							_sslSettings,
							_messageSerializer,
							_serviceCallResultSerializer,
							_serviceDispatcher,
							_logger))
						{
							await handler.Handle();
						}
					});
				}
			}
			catch (SocketException socketException)
			{
				if (socketException.SocketErrorCode != SocketError.Interrupted)
				{
					throw;
				}
				
				_logger.LogError(socketException);
			}
			finally
			{
				_listenerWaitHandle.Set();   
			}
		}
		
		private readonly TcpListener _tcpListener;
		private readonly AutoResetEvent _listenerWaitHandle;
		private readonly TService _serviceInstance;
		private readonly TimeSpan _serviceShutdownTimeout;
		private readonly ServerSslSettings _sslSettings;
		private readonly ILogger _logger;
		private readonly ServiceDispatcher<TService> _serviceDispatcher;
		private readonly ISerializer<RpcMessage> _messageSerializer;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
		private bool _isDisposing;		

		private class ClientHandler : IDisposable
		{
			public ClientHandler(
				TcpClient client,
				TService serviceInstance,
				ServerSslSettings sslSettings,
				ISerializer<RpcMessage> messageSerializer,
				ISerializer<ServiceCallResult> serviceCallResultSerializer,
				ServiceDispatcher<TService> serviceDispatcher,
				ILogger logger)
			{
				_client = client;
				_serviceInstance = serviceInstance;
				_sslSettings = sslSettings;
				_messageSerializer = messageSerializer;
				_serviceDispatcher = serviceDispatcher;
				_logger = logger;
				_serviceCallResultSerializer = serviceCallResultSerializer;
			}

			public async Task Handle()
			{
				try
				{
					var stream = GetNetworkStream();
					var message = _messageSerializer.Deserialize(await stream.ReadMessageAsync());
					var serviceCallResult = _serviceDispatcher.Dispatch(_serviceInstance, message);
					if (serviceCallResult.HasReturnValue || serviceCallResult.HasException)
					{
						var returnData = _serviceCallResultSerializer.Serialize(serviceCallResult);
						_logger.LogDebug($"Sending {returnData.Length} bytes to caller");
						await stream.WriteMessageAsync(returnData);
					}

					_logger.LogDebug("Closing client");
					//TODO: Maybe this shouldn't be closed by server
					stream.Close();
				}
				catch (Exception exception)
				{
					_logger.LogError(exception, "Error working with channel");
					throw;
				}
			}
			
			public void Dispose()
			{
				_client.Close();
				_client.Dispose();
				_logger.LogDebug($"{_serviceInstance.GetType()}: client disposed");
			}

			private Stream GetNetworkStream()
			{
				var clientStream = _client.GetStream();
				if (_sslSettings == null)
				{
					return clientStream;
				}
				
				var sslStream = new SslStream(clientStream, false);
				sslStream.AuthenticateAsServer(
					_sslSettings.Certificate, 
					_sslSettings.ClientCertificateRequired, 
					_sslSettings.CheckCertificateRevocation);
				
				return sslStream;
			}
		
			private readonly TcpClient _client;
			private readonly TService _serviceInstance;
			private readonly ServerSslSettings _sslSettings;
			private readonly ISerializer<RpcMessage> _messageSerializer;
			private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
			private readonly ServiceDispatcher<TService> _serviceDispatcher;
			private readonly ILogger _logger;
		} 
	}
}