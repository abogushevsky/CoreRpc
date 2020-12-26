using System;
using System.Threading.Tasks;
using CoreRpc.Networking.ConnectionPooling;
using CoreRpc.UnitTests.Utilities;
using CoreRpc.Utilities;
using Xunit;

namespace CoreRpc.UnitTests
{
    public class ConnectionPoolTest
    {
        [Fact]
        public async Task GivenFirstObjectIsCreatedAfterFirstCallWhenObjectIsReleasedThenSameObjectIsReturnedOnSecondCall()
        {
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(() => new PooledObject(), item => {}),
                TimeSpan.MaxValue,
                _testDateTimeProvider,
                1);
            var testObject = await testPool.Acquire();
            Assert.NotNull(testObject);
            Assert.Equal(1, testObject.InstanceNumber);
            
            testPool.Release(testObject);
            var secondTestObject = await testPool.Acquire();
            Assert.Equal(1, testObject.InstanceNumber);
            Assert.Equal(testObject, secondTestObject);
            Assert.True(ReferenceEquals(testObject, secondTestObject));
        }

        [Fact]
        public void GivenFirstObjectIsLockedAndSecondCallIsPerformedThenSecondObjectIsCreated()
        {
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(() => new PooledObject(), item => {}),
                TimeSpan.MaxValue,
                _testDateTimeProvider,
                2);
            
            Assert.True(false);
        }

        [Fact]
        public void WhenAllObjectsAreLockedAndMaxPoolCapacityIsReachedThenCallerThreadIsLocked()
        {
            Assert.True(false);
        }

        private readonly TestDateTimeProvider _testDateTimeProvider = 
            new TestDateTimeProvider(new DateTimeProvider());
    }

    internal class PooledObject
    {
        public PooledObject()
        {
            InstanceNumber = ++Count;
        }

        public int InstanceNumber { get; }

        private static int Count = 0;
    }
}