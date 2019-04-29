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
            var logger = new LoggerStub();
            Helpers.LogCurrentMemoryUsage(logger);
            Console.ReadLine();

            var messagePackSerializerFactory = new MessagePackSerializerFactory();

            using (var testServiceClient = ServiceClientFactory.CreateServiceClient<ITestService>(
                "localhost",
                logger,
                messagePackSerializerFactory))
            {
                Console.WriteLine("Test service client created.");

                // Warm up
                const int warmUpCallsCount = 5000;
                Enumerable
                    .Range(0, warmUpCallsCount)
                    .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient.ServiceInstance, logger));

                GC.Collect(2, GCCollectionMode.Forced);
                Thread.Sleep(TimeSpan.FromSeconds(5));
                
                const int callsCount = 1000;
                RunTests(() => TestSyncOperations(testServiceClient.ServiceInstance, logger, callsCount), logger);
                RunTests(() => TestAsyncOperations(testServiceClient.ServiceInstance, logger, callsCount), logger);

                Console.WriteLine("All requests send.");
                Console.ReadLine();
            }
        }

        private static void RunTests(Action testsAction, ILogger logger)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            testsAction();
            
            stopwatch.Stop();
            logger.LogInfo($"Elapsed ms: {stopwatch.Elapsed.TotalMilliseconds}");
            Console.WriteLine($"Elapsed ms: {stopwatch.Elapsed.TotalMilliseconds}");

            Helpers.LogCurrentMemoryUsage(logger);
        }

        private static void TestAsyncOperations(ITestService testService, ILogger logger, int callsCount)
        {
            // TODO: call async methods
        }

        private static void TestSyncOperations(ITestService testService, ILogger logger, int callsCount)
        {
            Enumerable
                .Range(0, callsCount)
                .ParallelForEach(_ => SendRequestAndLogResult(testService, logger));
        }

        private static void SendRequestAndLogResult(ITestService testServiceClient, ILogger logger)
        {
            var result = testServiceClient.GetTestData();
            logger.LogDebug($"Result of GetTestData: {result.Id}");
            logger.LogDebug($"Result of SetTestData: {testServiceClient.SetTestData(new TestData())}");
        }
    }
}