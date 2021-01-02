using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CoreRpc.Logging;
using CoreRpc.Networking.Rpc;
using CoreRpc.TestContract;

namespace CoreRpc.TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new LoggerStub();
            Helpers.LogCurrentMemoryUsage(logger);

            var serviceClientFactory = new ServiceClientFactory(logger);

            using (var testServiceClient = serviceClientFactory.CreateServiceClient<ITestService>(
                "localhost"))
            {
                Console.WriteLine("Test service client created.");

                // Warm up
                const int warmUpCallsCount = 5000;
                Enumerable
                    .Range(0, warmUpCallsCount)
                    .ParallelForEach(_ => SendRequestAndLogResult(testServiceClient.ServiceInstance, logger));
                
                Console.WriteLine("Warmup done");
                GC.Collect(2, GCCollectionMode.Forced);
                // Thread.Sleep(TimeSpan.FromSeconds(5));
                
                const int callsCount = 10000;
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
            var tasks = Enumerable
                .Range(0, callsCount)
                .Select(async _ => await SendRequestAndLogResultAsync(testService, logger))
                .ToArray();
            Task.WaitAll(tasks);
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
            // logger.LogDebug($"Result of GetTestData: {result.Id}");
            // logger.LogDebug($"Result of SetTestData: {testServiceClient.SetTestData(new TestData())}");
        }

        private static async Task SendRequestAndLogResultAsync(ITestService testServiceClient, ILogger logger)
        {
            var result = await testServiceClient.GetTestDataAsync();
            // logger.LogDebug($"Result of GetTestData: {result.Id}");
            // logger.LogDebug($"Result of SetTestData: {testServiceClient.SetTestData(new TestData())}");
        }
    }
}