using System;
using System.IO;
using System.Net.Sockets;
using CoreRpc.Serialization;

namespace CoreRpc.Networking.Rpc
{
	public abstract class RpcTcpClientBase : IDisposable
	{
		protected RpcTcpClientBase(
			string hostName,
			int port,
			ISerializerFactory serializerFactory,
			bool doUseSingleConnection)
		{
			_hostName = hostName;
			_port = port;
			_doUseSingleConnection = doUseSingleConnection;
			_serviceCallResultSerializer = serializerFactory.CreateSerializer<ServiceCallResult>();

			if (_doUseSingleConnection)
			{
				_tcpClient = new TcpClient();
			}
		}
		
		public void Send(byte[] data) => ConnectAndSend(serviceCallResult => 0, data);

		public byte[] SendAndReceive(byte[] data) => 
			ConnectAndSend(serviceCallResult => serviceCallResult.ReturnValue, data);
		
		private TResult ConnectAndSend<TResult>(Func<ServiceCallResult, TResult> doWithServiceCallResult, byte[] data)
		{
			return DoWithConnectedTcpClient(tcpClient =>
			{
				using (var networkStream = GetNetworkStreamFromTcpClient(tcpClient))
				{
					networkStream.WriteMessage(data);

					var serviceCallResult = _serviceCallResultSerializer.Deserialize(networkStream.ReadMessage());
					if (serviceCallResult.HasException)
					{
						throw ExceptionsSerializer.Instance.Deserialize(serviceCallResult.Exception);
					}

					return doWithServiceCallResult(serviceCallResult);
				}
			});
		}

		private TResult DoWithConnectedTcpClient<TResult>(Func<TcpClient, TResult> doWithTcpClient)
		{
			if (!_doUseSingleConnection)
			{
				using (var tcpClient = new TcpClient())
				{
					try
					{
						tcpClient.Connect(_hostName, _port);
						return doWithTcpClient(tcpClient);
					}
					finally
					{
						if (tcpClient.Connected)
						{
							// TODO: send end of session message
						}
						
						tcpClient.Close();
					}
				}
			}

			lock (_tcpClientSyncRoot)
			{
				if (!_tcpClient.Connected)
				{
					_tcpClient.Connect(_hostName, _port);
				}

				return doWithTcpClient(_tcpClient);
			}
		}
					
		protected abstract Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient);
		
		public void Dispose()
		{
			if (_tcpClient.Connected)
			{
				// TODO: send end of session message
				_tcpClient.Close();
			}	
			
			_tcpClient.Dispose();
		}
		
		protected readonly string _hostName;
		
		private readonly int _port;
		private readonly bool _doUseSingleConnection;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
		private readonly TcpClient _tcpClient;		
		private readonly object _tcpClientSyncRoot = new object();
	}
}