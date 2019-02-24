using System;
using CoreRpc.Logging;

namespace CoreRpc.Utilities
{
    internal static class ExceptionsHandlingHelper
    {
        public static TResult ExecuteSafely<TResult>(Func<TResult> function, Func<Exception, TResult> errorHandler)
        {
            try
            {
                return function();
            }
            catch (Exception exception)
            {
                if (IsCriticalException(exception))
                {
                    throw;
                }
                
                return errorHandler(exception);
            }
        }

        public static TResult ExecuteWithExceptionLogging<TResult>(Func<TResult> function, ILogger logger) =>
            ExecuteSafely(function, exception =>
            {
                logger.LogError(exception);
                throw exception;
            });

        public static void ExecuteWithExceptionLogging(Action action, ILogger logger) => ExecuteWithExceptionLogging(
            () =>
            {
                action();
                return 0;
            },
            logger);

        private static bool IsCriticalException(Exception exception) =>
            exception is OutOfMemoryException ||
            exception is AccessViolationException ||
            exception is StackOverflowException;
    }
}