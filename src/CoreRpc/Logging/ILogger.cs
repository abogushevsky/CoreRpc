namespace CoreRpc.Logging
{
	public interface ILogger
	{
		void LogError(System.Exception exception);

		void LogError(System.Exception exception, string message);

		void LogError(string message);

		void LogInfo(string message);

		void LogDebug(string message);
	}
}