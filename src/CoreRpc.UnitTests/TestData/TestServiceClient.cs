using System.Collections.Generic;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.ConnectionPooling;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization;
using CoreRpc.Utilities;

namespace CoreRpc.UnitTests.TestData
{
	public class TestServiceClient : ITestService
	{
		public TestServiceClient(string hostName, int port, ISerializerFactory serializerFactory)
		{
			var logger = new LoggerStub();
			_serviceDescriptor = ServiceDescriptor.Of<ITestService>();
			_serializerFactory = serializerFactory;
			_tcpClient = new UnprotectedRpcTcpClient(
				hostName, 
				port, 
				new StalePooledObjectsCleaner(logger),
				new DateTimeProvider(),
				_serializerFactory, 
				logger,
				null);
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

		public async Task<(int count, SerializableObject[] objects)> GetObjectsAsync(int offset, int count)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[6],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<int>().Serialize(offset),
					_serializerFactory.CreateSerializer<int>().Serialize(count)
				}
			};

			var remoteCallResult = await _tcpClient.SendAndReceiveAsync(
				_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<(int count, SerializableObject[] objects)>().Deserialize(remoteCallResult);
		}

		public async Task VoidMethodAsync(string someString)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[7],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<string>().Serialize(someString)
				}
			};

			await _tcpClient.SendAsync(_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
		}

		public async Task<List<SerializableObject>> ConstructObjectsListAsync(int[] ids, string[] names, double[] ages)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[5],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<int[]>().Serialize(ids),
					_serializerFactory.CreateSerializer<string[]>().Serialize(names),
					_serializerFactory.CreateSerializer<double[]>().Serialize(ages)
				}
			};

			var remoteCallResult = await _tcpClient.SendAndReceiveAsync(
				_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<List<SerializableObject>>().Deserialize(remoteCallResult);
		}

		public async Task<int> GetHashCodeOfMeAsync(SerializableObject me)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[4],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<SerializableObject>().Serialize(me)
				}
			};

			var remoteCallResult = await _tcpClient.SendAndReceiveAsync(
				_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<int>().Deserialize(remoteCallResult);
		}

		public async Task<SerializableObject> ConstructObjectAsync(int id, string name, double age)
		{
			var rpcMessage = new RpcMessage
			{
				ServiceCode = _serviceDescriptor.ServiceCode,
				OperationCode = _serviceDescriptor.OperationCodes[5],
				ArgumentsData = new[]
				{
					_serializerFactory.CreateSerializer<int>().Serialize(id),
					_serializerFactory.CreateSerializer<string>().Serialize(name),
					_serializerFactory.CreateSerializer<double>().Serialize(age)
				}
			};

			var remoteCallResult = await _tcpClient.SendAndReceiveAsync(
				_serializerFactory.CreateSerializer<RpcMessage>().Serialize(rpcMessage));
			return _serializerFactory.CreateSerializer<SerializableObject>().Deserialize(remoteCallResult);
		}

		private readonly ServiceDescriptor _serviceDescriptor;
		private readonly ISerializerFactory _serializerFactory;
		private readonly RpcTcpClientBase _tcpClient;
	}
}