using System;
using CoreRpc.Logging;

namespace CoreRpc.Utilities
{
    internal static class ExceptionsHandlingHelper
    {
        public static TResult ExecuteSafely<TResult>(
            Func<TResult> function, 
            Func<Exception, TResult> errorHandler,
            Func<TResult> finallyFunction)
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
            finally
            {
                finallyFunction();
            }
        }

        public static TResult ExecuteWithExceptionLogging<TResult>(
            Func<TResult> function,
            Func<TResult> finallyFunction,
            ILogger logger) =>
            ExecuteSafely(
                function,
                exception =>
                {
                    logger.LogError(exception);
                    throw exception;
                },
                finallyFunction);

        public static void ExecuteWithExceptionLogging(Action action, Action finallyAction, ILogger logger) => 
            ExecuteWithExceptionLogging(
            () =>
            {
                action();
                return 0;
            },
            () =>
            {
                finallyAction();
                return 0;
            },
            logger);

        private static bool IsCriticalException(Exception exception) =>
            exception is OutOfMemoryException ||
            exception is AccessViolationException ||
            exception is StackOverflowException;
    }
}