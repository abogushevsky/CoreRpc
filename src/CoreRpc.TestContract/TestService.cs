using System;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Logging;

namespace CoreRpc.TestContract
{
    public class TestService : ITestService
    {        
        public TestService(ILogger logger)
        {
            _logger = logger;
            // Task.Factory.StartNew(LogCallsCount, TaskCreationOptions.LongRunning);
        }
        
        public int SetTestData(TestData testData)
        {
            IncrementCallsCount();
            return _random.Next(int.MaxValue);
        }

        public TestData GetTestData()
        {
            IncrementCallsCount();
            return new TestData();
        }

        public async Task<TestData> GetTestDataAsync()
        {
            IncrementCallsCount();
            await Task.Delay(100);
            return new TestData();
        }

        public async Task TestAsync() => await Task.Delay(100);

        private void IncrementCallsCount()
        {
            Interlocked.Increment(ref _callsCount);
        }

        private void LogCallsCount()
        {
            while (true)
            {
                Console.WriteLine($"Current calls count: {_callsCount}");
                Thread.Sleep(500);
            }
        }

        private int _callsCount;
        private readonly ILogger _logger;
        private readonly Random _random = new Random();
    }
}