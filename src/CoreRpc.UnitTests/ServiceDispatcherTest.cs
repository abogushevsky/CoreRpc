using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.UnitTests.TestData;
using Xunit;

namespace CoreRpc.UnitTests
{
	public class ServiceDispatcherTest
	{
		[Fact]
		public void ServiceTest()
		{
			var callsCount = 0;
			var serviceInstance = new TestService(() => callsCount++);
			var serializerFactory = new MessagePackSerializerFactory();
			var testObject = SerializableObject.GetTestInstance();
			var argumentsData = new byte[1][];
			argumentsData[0] = serializerFactory.CreateSerializer<SerializableObject>().Serialize(testObject);

			var serviceDispatcher = new ServiceDispatcher<ITestService>(serializerFactory, new LoggerStub());
			Assert.Equal(serviceDispatcher.ServicePort, TestService.ServicePort);
			var result = serviceDispatcher.Dispatch(serviceInstance, new RpcMessage
			{
				ServiceCode = typeof(ITestService).FullName.GetHashCode(),
				OperationCode = 0,
				ArgumentsData = argumentsData
			});
			
			Assert.NotNull(result);
			Assert.True(result.HasReturnValue);
			Assert.Equal(testObject.IntProperty, serializerFactory.CreateSerializer<int>().Deserialize(result.ReturnValue));

			argumentsData = new byte[3][];
			argumentsData[0] = serializerFactory.CreateSerializer<int>().Serialize(SerializableObject.TestInt);
			argumentsData[1] = serializerFactory.CreateSerializer<string>().Serialize(SerializableObject.TestString);
			argumentsData[2] = serializerFactory.CreateSerializer<double>().Serialize(SerializableObject.TestDouble);
			
			result = serviceDispatcher.Dispatch(serviceInstance, new RpcMessage
			{
				ServiceCode = typeof(ITestService).FullName.GetHashCode(),
				OperationCode = 1,
				ArgumentsData = argumentsData
			});

			Assert.NotNull(result);
			Assert.True(result.HasReturnValue);
			var resultObject = serializerFactory.CreateSerializer<SerializableObject>().Deserialize(result.ReturnValue);
			Assert.NotNull(resultObject);
			Assert.Equal(testObject.IntProperty, resultObject.IntProperty);
			Assert.Equal(testObject.StringProperty, resultObject.StringProperty);
			Assert.Equal(testObject.NestedObject.DoubleProperty, resultObject.NestedObject.DoubleProperty);
			
			argumentsData = new byte[1][];
			argumentsData[0] = serializerFactory.CreateSerializer<string>().Serialize(SerializableObject.TestString);
			
			result = serviceDispatcher.Dispatch(serviceInstance, new RpcMessage
			{
				ServiceCode = typeof(ITestService).FullName.GetHashCode(),
				OperationCode = 2,
				ArgumentsData = argumentsData
			});
			
			Assert.NotNull(result);
			Assert.False(result.HasReturnValue);
			Assert.Equal(1, callsCount);
		}

		[Fact]
		public void ReturnTupleTest()
		{
			var callsCount = 0;
			var serviceInstance = new TestService(() => callsCount++);
			var serializerFactory = new MessagePackSerializerFactory();
			var serviceDispatcher = new ServiceDispatcher<ITestService>(serializerFactory, new LoggerStub());

			var argumentsData = new byte[2][];
			argumentsData[0] = serializerFactory.CreateSerializer<int>().Serialize(0);
			argumentsData[1] = serializerFactory.CreateSerializer<int>().Serialize(2);

			var result = serviceDispatcher.Dispatch(serviceInstance, new RpcMessage()
			{
				ServiceCode = typeof(ITestService).FullName.GetHashCode(),
				OperationCode = 3,
				ArgumentsData = argumentsData
			});

			Assert.NotNull(result);
			Assert.True(result.HasReturnValue);
			var resultTuple = serializerFactory.CreateSerializer<(int count , SerializableObject[] objects)>().Deserialize(result.ReturnValue);
			Assert.Equal(2, resultTuple.count);
			Assert.Equal(SerializableObject.TestInt, resultTuple.objects[0].IntProperty);
			Assert.Equal(SerializableObject.TestString, resultTuple.objects[0].StringProperty);
			Assert.Equal(SerializableObject.TestDouble, resultTuple.objects[0].NestedObject.DoubleProperty);
		}
	}
}