using System.Diagnostics;
using CoreRpc.Logging;

namespace CoreRpc.TestContract
{
    public static class Helpers
    {
        public static void LogCurrentMemoryUsage(ILogger logger)
        {
            var currentProcess = Process.GetCurrentProcess();
            logger.LogInfo($"Memory working set: {currentProcess.WorkingSet64 / 1024 / 1024} Mb");
        }
    }
}