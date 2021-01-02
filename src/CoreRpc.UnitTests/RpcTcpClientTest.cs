using System;
using System.Linq;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.UnitTests.TestData;
using Xunit;

namespace CoreRpc.UnitTests
{
	public sealed class RpcTcpClientTest
	{
		[Fact]
		public void SyncClientMethodsTest()
		{
			var serializerFactory = new MessagePackSerializerFactory();
			var serviceClientFactory = new ServiceClientFactory(new LoggerStub());
			using (var _ = GetTestService(serializerFactory))
			{
				using (var client = serviceClientFactory.CreateServiceClient<ITestService>(
					"localhost",
					serializerFactory))
				{

					var myHashcode = client.ServiceInstance.GetHashCodeOfMe(SerializableObject.GetTestInstance());
					Assert.Equal(SerializableObject.TestInt, myHashcode);

					var constructedObject = client.ServiceInstance.ConstructObject(
						SerializableObject.TestInt,
						SerializableObject.TestString,
						SerializableObject.TestDouble);
					Assert.NotNull(constructedObject);
					Assert.Equal(SerializableObject.TestInt, constructedObject.IntProperty);
					Assert.Equal(SerializableObject.TestString, constructedObject.StringProperty);
					Assert.Equal(SerializableObject.TestDouble, constructedObject.NestedObject.DoubleProperty);
				}
			}
		}

		[Fact]
		public async void AsyncClientMethodsTest()
		{
			var serializerFactory = new MessagePackSerializerFactory();
			var serviceClientFactory = new ServiceClientFactory(new LoggerStub());
			using (var _ = GetTestService(serializerFactory))
			{
				using (var client = serviceClientFactory.CreateServiceClient<ITestService>(
					"localhost",
					serializerFactory))
				{

					var myHashcode = await client.ServiceInstance.GetHashCodeOfMeAsync(SerializableObject.GetTestInstance());
					Assert.Equal(SerializableObject.TestInt, myHashcode);

					var constructedObject = await client.ServiceInstance.ConstructObjectAsync(
						SerializableObject.TestInt,
						SerializableObject.TestString,
						SerializableObject.TestDouble);
					Assert.NotNull(constructedObject);
					Assert.Equal(SerializableObject.TestInt, constructedObject.IntProperty);
					Assert.Equal(SerializableObject.TestString, constructedObject.StringProperty);
					Assert.Equal(SerializableObject.TestDouble, constructedObject.NestedObject.DoubleProperty);

					var (count, objects) = await client.ServiceInstance.GetObjectsAsync(1, 1);
					Assert.Equal(2, count);
					constructedObject = objects.Single();
					Assert.Equal(SerializableObject.TestInt, constructedObject.IntProperty);
					Assert.Equal(SerializableObject.TestString, constructedObject.StringProperty);
					Assert.Equal(SerializableObject.TestDouble, constructedObject.NestedObject.DoubleProperty);
				}
			}
		}

		private static RpcTcpServer<ITestService> GetTestService(ISerializerFactory serializerFactory)
		{
			var callsCount = 0;
			var serviceInstance = new TestService(() => callsCount++);
			var servicePublisher = new RpcTcpServicePublisher(serializerFactory, new LoggerStub());
			return servicePublisher.PublishUnsecured<ITestService>(serviceInstance, TimeSpan.FromMinutes(1));
		}
	}
}