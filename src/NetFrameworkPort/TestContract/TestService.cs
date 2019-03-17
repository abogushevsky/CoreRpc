using System;
using System.Threading;
using CoreRpc.Logging;

namespace TestContract
{
    public class TestService : ITestService
    {
        public TestService() : this(new LoggerStub())
        {

        }

        public TestService(ILogger logger)
        {
            _logger = logger;
        }
        
        public int SetTestData(TestData testData)
        {
            IncrementAndLogCallsCount();
            return _random.Next(int.MaxValue);
        }

        public TestData GetTestData()
        {
            IncrementAndLogCallsCount();
            return new TestData();
        }

        private void IncrementAndLogCallsCount()
        {
            Interlocked.Increment(ref _callsCount);
            _logger.LogInfo($"Current calls count: {_callsCount}");
        }

        private int _callsCount;
        private readonly ILogger _logger;
        private readonly Random _random = new Random();
    }
}