using System;
using System.ComponentModel.DataAnnotations;
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
		public void ClientTest()
		{
			var serializerFactory = new MessagePackSerializerFactory();
			using (var server = GetTestService(serializerFactory))
			{
				using (var client = ServiceClientFactory.CreateServiceClient<ITestService>(
					"localhost",
					new LoggerStub(),
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

		private static RpcTcpServer<ITestService> GetTestService(ISerializerFactory serializerFactory)
		{
			var callsCount = 0;
			var serviceInstance = new TestService(() => callsCount++);
			var servicePublisher = new RpcTcpServicePublisher(serializerFactory, new LoggerStub());
			return servicePublisher.PublishUnsecured<ITestService>(serviceInstance, TimeSpan.FromMinutes(1));
		}
	}
}