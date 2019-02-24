using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.TestContract;
using CoreRpc.Utilities;

namespace CoreRpc.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var logger = new ConsoleLoggerWrapper(new LoggerStub()))
            {
                Helpers.LogCurrentMemoryUsage(logger);
                Console.ReadLine();

                var messagePackSerializerFactory = new MessagePackSerializerFactory();

                // TODO: client should implement IDisposable
                using (var testServiceClient = ServiceClientFactory.CreateServiceClient<ITestService>(
                    "localhost",
                    logger,
                    messagePackSerializerFactory,
                    doUseSingleConnection: true))
                {
                    Console.WriteLine("Test service client created.");

                    // Warm up
                    const int warmUpCallsCount = 5000;
                    Enumerable
                        .Range(0, warmUpCallsCount)
                        .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient.ServiceInstance, logger));

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    const int callsCount = 1000;
                    Enumerable
                        .Range(0, callsCount)
                        .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient.ServiceInstance, logger));
                    stopwatch.Stop();
                    logger.LogInfo($"Elapsed ms: {stopwatch.Elapsed.TotalMilliseconds}");

                    Helpers.LogCurrentMemoryUsage(logger);

                    Console.WriteLine("All requests send.");
                    Console.ReadLine();
                }
            }
        }

        private static void SendRequestAndLogResult(ITestService testServiceClient, ILogger logger)
        {
            var result = testServiceClient.GetTestData();
            logger.LogDebug($"Result of GetTestData: {result.Id}");
            logger.LogDebug($"Result of SetTestData: {testServiceClient.SetTestData(new TestData())}");
        }
    }
}