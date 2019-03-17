using System;

namespace CoreRpc.Logging
{
	public class LoggerStub : ILogger
	{
		public void LogError(Exception exception)
		{
		}

		public void LogError(Exception exception, string message)
		{
		}

		public void LogError(string message)
		{
		}

		public void LogInfo(string message)
		{
		}

		public void LogDebug(string message)
		{
		}
	}
}