using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Utilities;

namespace CoreRpc.Logging
{
	public class ConsoleLoggerWrapper : ILogger, IDisposable
	{ 
		public ConsoleLoggerWrapper(ILogger wrappedLogger)
		{
			_wrappedLogger = wrappedLogger;
			Task.Factory.StartNew(ProcessConsoleActionsQueue, TaskCreationOptions.LongRunning);
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
		
		public void Dispose() => _isDisposed = true;

		private void WriteToConsoleWithColor(Action write, ConsoleColor color)
		{
			_consoleActionsQueue.Enqueue(() =>
			{
				var previousColor = Console.ForegroundColor;
				Console.ForegroundColor = color;
				write();
				Console.ForegroundColor = previousColor;
			});
			_queueHandlerWaitHandle.Set();
		}

		private void WriteInnerExceptionsInConsole(Exception exception)
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

		private void WriteInnerException(Exception exception)
		{
			if (exception.InnerException == null)
			{
				return;
			}

			WriteExceptionSeparatorToConsole();
			WriteExceptionToConsole(exception.InnerException);
		}

		private void WriteExceptionToConsole(Exception ex) => WriteToConsoleWithColor(() => Console.Error.WriteLine(ex), ConsoleColor.Red);

		private void WriteExceptionSeparatorToConsole() => WriteToConsoleWithColor(
			() => Console.Error.WriteLine($"====================================================="),
			ConsoleColor.Red);

		private void ProcessConsoleActionsQueue()
		{
			while (!_isDisposed)
			{
				_queueHandlerWaitHandle.WaitOne();
				while (_consoleActionsQueue.TryDequeue(out var consoleAction))
				{
					consoleAction();
				}
			}
		}

		private readonly ILogger _wrappedLogger;
		private readonly ConcurrentQueue<Action> _consoleActionsQueue = new ConcurrentQueue<Action>();
		private readonly object _consoleSyncRoot = new object();
		private readonly AutoResetEvent _queueHandlerWaitHandle = new AutoResetEvent(false);
		private bool _isDisposed;		
	}
}