using System;

namespace CoreRpc.Utilities
{
    public interface IDateTimeProvider
    {
        DateTime GetCurrent();
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime GetCurrent() => DateTime.Now;
    }
}