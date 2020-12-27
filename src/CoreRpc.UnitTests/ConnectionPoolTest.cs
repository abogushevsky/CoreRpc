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
            var instanceNumber = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(() => new PooledObject(++instanceNumber), item => {}),
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
        public async Task GivenFirstObjectIsLockedAndSecondCallIsPerformedThenSecondObjectIsCreated()
        {
            var instanceNumber = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(() => new PooledObject(++instanceNumber), item => {}),
                TimeSpan.MaxValue,
                _testDateTimeProvider,
                2);

            var testObject = await testPool.Acquire();
            Assert.NotNull(testObject);
            Assert.Equal(1, testObject.InstanceNumber);

            var secondTestObject = await testPool.Acquire();
            Assert.NotNull(secondTestObject);
            Assert.Equal(2, secondTestObject.InstanceNumber);
            
            testPool.Release(secondTestObject);

            secondTestObject = await testPool.Acquire();
            Assert.NotNull(secondTestObject);
            Assert.Equal(2, secondTestObject.InstanceNumber);
        }

        [Fact]
        public async Task WhenAllObjectsAreLockedAndMaxPoolCapacityIsReachedThenCallerThreadIsLocked()
        {
            var instanceNumber = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(() => new PooledObject(++instanceNumber), item => {}),
                TimeSpan.MaxValue,
                _testDateTimeProvider,
                1);
            
            var testObject = await testPool.Acquire();
            Assert.NotNull(testObject);
            Assert.Equal(1, testObject.InstanceNumber);

            Assert.False(Task.WaitAll(new Task[] {testPool.Acquire()}, 500));
            
            testPool.Release(testObject);
        }

        private readonly TestDateTimeProvider _testDateTimeProvider = 
            new TestDateTimeProvider(new DateTimeProvider());
    }

    internal class PooledObject
    {
        public PooledObject(int instanceNumber)
        {
            InstanceNumber = instanceNumber;
        }

        public int InstanceNumber { get; }
    }
}