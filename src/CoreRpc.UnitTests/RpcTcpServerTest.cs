using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.UnitTests.TestData;
using Xunit;

namespace CoreRpc.UnitTests
{
	public class RpcTcpServerTest
	{
		[Fact]
		public void ServerTest()
		{
			var callsCount = 0;
			var serviceInstance = new TestService(() => callsCount++);
			var serializerFactory = new MessagePackSerializerFactory();
			var servicePublisher = new RpcTcpServicePublisher(serializerFactory, new LoggerStub());
			
			using (servicePublisher.PublishUnsecured<ITestService>(serviceInstance, TimeSpan.FromMinutes(1)))
			{
				PerformTestWithConnectedNetworkStream(networkStream =>
				{
					var argumentsData = new byte[1][];
					argumentsData[0] = serializerFactory
						.CreateSerializer<SerializableObject>()
						.Serialize(SerializableObject.GetTestInstance());
					var rpcMessage = serializerFactory.CreateSerializer<RpcMessage>().Serialize(new RpcMessage()
					{
						ServiceCode = typeof(ITestService).FullName.GetHashCode(),
						OperationCode = 0,
						ArgumentsData = argumentsData
					});

					networkStream.Write(rpcMessage, 0, rpcMessage.Length);

					var data = new List<byte>();
					var dataChunk = new byte[128];
					do
					{
						var bytesRead = networkStream.Read(dataChunk, 0, dataChunk.Length);
						data.AddRange(dataChunk.Take(bytesRead).ToArray());
					} while (networkStream.DataAvailable);

					var serviceCallResult = serializerFactory.CreateSerializer<ServiceCallResult>().Deserialize(data.ToArray());
					var result = serializerFactory.CreateSerializer<int>().Deserialize(serviceCallResult.ReturnValue);

					Assert.Equal(SerializableObject.TestInt, result);
				});

				PerformTestWithConnectedNetworkStream(networkStream =>
				{
					var argumentsData = new byte[2][];
					argumentsData[0] = serializerFactory.CreateSerializer<int>().Serialize(0);
					argumentsData[1] = serializerFactory.CreateSerializer<int>().Serialize(2);

					var rpcMessage = serializerFactory.CreateSerializer<RpcMessage>().Serialize(new RpcMessage()
					{
						ServiceCode = typeof(ITestService).FullName.GetHashCode(),
						OperationCode = 3,
						ArgumentsData = argumentsData
					});

					networkStream.Write(rpcMessage, 0, rpcMessage.Length);

					var data = new List<byte>();
					var dataChunk = new byte[128];
					do
					{
						var bytesRead = networkStream.Read(dataChunk, 0, dataChunk.Length);
						data.AddRange(dataChunk.Take(bytesRead).ToArray());
					} while (networkStream.DataAvailable);

					var serviceCallResult = serializerFactory.CreateSerializer<ServiceCallResult>().Deserialize(data.ToArray());
					var resultTuple = serializerFactory.CreateSerializer<(int count, SerializableObject[] objects)>()
						.Deserialize(serviceCallResult.ReturnValue);

					Assert.Equal(2, resultTuple.count);
					Assert.Equal(SerializableObject.TestInt, resultTuple.objects[0].IntProperty);
					Assert.Equal(SerializableObject.TestString, resultTuple.objects[0].StringProperty);
					Assert.Equal(SerializableObject.TestDouble, resultTuple.objects[0].NestedObject.DoubleProperty);
				});
			}
		}
		
		private static void PerformTestWithConnectedNetworkStream(Action<NetworkStream> testAction)
		{
			using (var testClient = new TcpClient())
			{
				testClient.Connect("localhost", TestService.ServicePort);
				var networkStream = testClient.GetStream();
				testAction(networkStream);
				testClient.Close();
			}
		}
	}
}