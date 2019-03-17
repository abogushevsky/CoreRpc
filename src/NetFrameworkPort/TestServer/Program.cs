using System;
using System.Diagnostics;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.TestContract;
using CoreRpc.Utilities;

namespace CoreRpc.TestServer
{
    class Program
    {
        public static void Main(string[] args)
        {
            var logger = new LoggerStub();
            // using (var logger = new ConsoleLoggerWrapper(new LoggerStub()))
            // {
                Helpers.LogCurrentMemoryUsage(logger);
                Console.ReadLine();
                
                var messagePackSerializerFactory = new MessagePackSerializerFactory();
                var rpcTcpServicePublisher = new RpcTcpServicePublisher(messagePackSerializerFactory, logger);
                var defaultServiceTimeout = TimeSpan.FromMinutes(1);
                var testService = new TestService(logger);

                using (new DisposableCollectionHolder(
                    rpcTcpServicePublisher.PublishUnsecured<ITestService>(
                        testService,
                        defaultServiceTimeout)))
                {
                    Console.WriteLine("Services started. Press Enter to stop.");
                    Console.ReadLine();
                    
                    Helpers.LogCurrentMemoryUsage(logger);
                }                
            // }
                       
            Console.WriteLine("Services stopped.");
        }
    }
}