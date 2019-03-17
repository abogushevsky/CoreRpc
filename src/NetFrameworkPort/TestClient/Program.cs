using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.Utilities;
using TestContract;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerStub();
            // using (var logger = new ConsoleLoggerWrapper(new LoggerStub()))
            // {
                Helpers.LogCurrentMemoryUsage(logger);
                // Console.ReadLine();

                var messagePackSerializerFactory = new MessagePackSerializerFactory();

                using (var testServiceClient = ServiceClientFactory.CreateServiceClient<ITestService>(
                    "localhost",
                    logger,
                    messagePackSerializerFactory))
                {
                    using (var wcfTestServiceClient = CommonWcfComponents.ServiceClient<ITestService>.Create())
                    {
                        Console.WriteLine("Test service client created.");
                        
                        RunTest(wcfTestServiceClient.Service, logger);

                        Helpers.LogCurrentMemoryUsage(logger);

                        Console.WriteLine("All requests send.");
                        Console.ReadLine();
                    }
                }
            // }
        }

        private static void RunTest(ITestService testServiceClient, ILogger logger)
        {
            // Warm up
            const int warmUpCallsCount = 5000;
            Enumerable
                .Range(0, warmUpCallsCount)
                .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient, logger));

            Thread.Sleep(TimeSpan.FromSeconds(5));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            const int callsCount = 1000;
            Enumerable
                .Range(0, callsCount)
                .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient, logger));
            stopwatch.Stop();
            Console.WriteLine($"Elapsed ms: {stopwatch.Elapsed.TotalMilliseconds}");
        }

        private static void SendRequestAndLogResult(ITestService testServiceClient, ILogger logger)
        {
            var result = testServiceClient.GetTestData();
            logger.LogDebug($"Result of GetTestData: {result.Id}");
            logger.LogDebug($"Result of SetTestData: {testServiceClient.SetTestData(new TestData())}");
        }
    }
}