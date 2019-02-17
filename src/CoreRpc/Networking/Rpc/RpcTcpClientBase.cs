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
			ISerializerFactory serializerFactory)
		{
			_hostName = hostName;
			_port = port;
			_serviceCallResultSerializer = serializerFactory.CreateSerializer<ServiceCallResult>();
		}
		
		public void Send(byte[] data) => ConnectAndSend(serviceCallResult => 0, data);

		public byte[] SendAndReceive(byte[] data) => 
			ConnectAndSend(serviceCallResult => serviceCallResult.ReturnValue, data);
		
		private TResult ConnectAndSend<TResult>(Func<ServiceCallResult, TResult> doWithServiceCallResult, byte[] data)
		{
			using (var tcpClient = new TcpClient())
			{
				try
				{
					tcpClient.Connect(_hostName, _port);

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
				}
				finally
				{
					tcpClient.Close();
				}
			}
		}
		
		protected abstract Stream GetNetworkStreamFromTcpClient(TcpClient tcpClient);
		
		public void Dispose()
		{
			
		}
		
		protected readonly string _hostName;
		
		private readonly int _port;
		private readonly ISerializer<ServiceCallResult> _serviceCallResultSerializer;
	}
}