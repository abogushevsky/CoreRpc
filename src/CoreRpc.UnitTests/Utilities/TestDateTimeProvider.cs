using System;
using CoreRpc.Utilities;

namespace CoreRpc.UnitTests.Utilities
{
    public class TestDateTimeProvider : IDateTimeProvider
    {
        public TestDateTimeProvider(IDateTimeProvider delegated)
        {
            _delegated = delegated;
        }

        public DateTime GetCurrent() => _fixedDateTime ?? _delegated.GetCurrent();

        public void FixDateTimeAt(DateTime fixedDateTime)
        {
            _fixedDateTime = fixedDateTime;
        }

        public void ResetFixedDateTime()
        {
            _fixedDateTime = null;
        }

        private DateTime? _fixedDateTime = null;
        private readonly IDateTimeProvider _delegated;
    }
}