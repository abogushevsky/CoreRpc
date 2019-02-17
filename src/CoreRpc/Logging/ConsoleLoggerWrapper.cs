using System;
using CoreRpc.Utilities;

namespace CoreRpc.Logging
{
	public class ConsoleLoggerWrapper : ILogger
	{ 
		public ConsoleLoggerWrapper(ILogger wrappedLogger)
		{
			_wrappedLogger = wrappedLogger;
		}

		public void LogError(Exception exception)
		{
			_wrappedLogger.LogError(exception);
			WriteToConsoleWithColor(() => Console.Error.WriteLine(exception), ConsoleColor.Red);
			WriteInnerExceptionsInConsole(exception);
		}

		public void LogError(Exception exception, string message)
		{
			_wrappedLogger.LogError(exception, message);
			WriteToConsoleWithColor(() => Console.Error.WriteLine($"{message}: \n{exception}"), ConsoleColor.Red);
			WriteInnerExceptionsInConsole(exception);
		}

		public void LogError(string message)
		{
			_wrappedLogger.LogError(message);
			WriteToConsoleWithColor(() => Console.Error.WriteLine(message), ConsoleColor.Red);
		}

		public void LogInfo(string message)
		{
			_wrappedLogger.LogInfo(message);
			WriteToConsoleWithColor(() => Console.WriteLine($"Info: {message}"), ConsoleColor.White);
		}

		public void LogDebug(string message)
		{
			_wrappedLogger.LogDebug(message);
			WriteToConsoleWithColor(() => Console.WriteLine($"Debug: {message}"), ConsoleColor.Green);
		}

		private static void WriteToConsoleWithColor(Action write, ConsoleColor color)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			write();
			Console.ForegroundColor = previousColor;
		}

		private static void WriteInnerExceptionsInConsole(Exception exception)
		{
			switch (exception)
			{
				case AggregateException aggregateException:
					aggregateException.InnerExceptions.ForEach(exceptionPart =>
					{
						WriteExceptionSeparatorToConsole();
						WriteExceptionToConsole(exceptionPart);
						WriteInnerException(exceptionPart);
					});
					break;
				default:
					if (exception.InnerException != null)
					{
						WriteExceptionSeparatorToConsole();
						WriteExceptionToConsole(exception.InnerException);
					}
					break;
			}
		}

		private static void WriteInnerException(Exception exception)
		{
			if (exception.InnerException == null)
			{
				return;
			}

			WriteExceptionSeparatorToConsole();
			WriteExceptionToConsole(exception.InnerException);
		}

		private static void WriteExceptionToConsole(Exception ex) => WriteToConsoleWithColor(() => Console.Error.WriteLine(ex), ConsoleColor.Red);

		private static void WriteExceptionSeparatorToConsole() => WriteToConsoleWithColor(
			() => Console.Error.WriteLine($"====================================================="),
			ConsoleColor.Red);

		private readonly ILogger _wrappedLogger;
	}
}