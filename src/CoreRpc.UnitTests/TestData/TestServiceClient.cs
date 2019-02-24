using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization;

namespace CoreRpc.UnitTests.TestData
{
	public class TestServiceClient : ITestService
	{
		public TestServiceClient(string hostName, int port, ISerializerFactory serializerFactory)
		{
			_serviceDescriptor = ServiceDescriptor.Of<ITestService>();
			_serializerFactory = serializerFactory;
			_tcpClient = new UnprotectedRpcTcpClient(hostName, port, _serializerFactory, new LoggerStub());
		}
		
		public int GetHashCodeOfMe(SerializableObject me)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[0],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<SerializableObject>().Serialize(me)
				}
			};

			var remoteCallResult =
				_tcpClient.SendAndReceive(_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<int>().Deserialize(remoteCallResult);
		}

		public SerializableObject ConstructObject(int id, string name, double age)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[1],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<int>().Serialize(id),
					_serializerFactory.CreateSerializer<string>().Serialize(name),
					_serializerFactory.CreateSerializer<double>().Serialize(age)
				}
			};

			var remoteCallResult =
				_tcpClient.SendAndReceive(_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<SerializableObject>().Deserialize(remoteCallResult);
		}

		public void VoidMethod(string someString)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[2],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<string>().Serialize(someString)
				}
			};

			_tcpClient.Send(_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
		}

		public (int count, SerializableObject[] objects) GetObjects(int offset, int count)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[3],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<int>().Serialize(offset),
					_serializerFactory.CreateSerializer<int>().Serialize(count)
				}
			};

			var remoteCallResult =
				_tcpClient.SendAndReceive(_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<(int count, SerializableObject[] objects)>().Deserialize(remoteCallResult);
		}
		
		private readonly ServiceDescriptor _serviceDescriptor;
		private readonly ISerializerFactory _serializerFactory;
		private readonly RpcTcpClientBase _tcpClient;
	}
}