using System;
using System.Collections.Generic;
using CoreRpc.Networking.ConnectionPooling;
using Xunit;

namespace CoreRpc.UnitTests
{
    public class ConnectionPoolTest
    {
        [Fact]
        public void GivedFirstObjectIsCreatedAfterFirstCallWhenObjectIsReleasedThenSameObjectIsReturnedOnSecondCall()
        {
            var testPool = new TestObjectsPool(
                () => new PooledObject(),
                new List<PooledObject>(),
                TimeSpan.MaxValue);
            var testObject = testPool.Acquire();
            Assert.NotNull(testObject);
            Assert.Equal(1, testObject.InstanceNumber);
            
            testPool.Release(testObject);
            var secondTestObject = testPool.Acquire();
            Assert.Equal(1, testObject.InstanceNumber);
            Assert.Equal(testObject, secondTestObject);
            Assert.True(ReferenceEquals(testObject, secondTestObject));
        }

        [Fact]
        public void GivenFirstObjectIsLockedAndSecondCallIsPerformedThenSecondObjectIsCreated()
        {
            var testPool = new TestObjectsPool(
                () => new PooledObject(),
                new List<PooledObject>(),
                TimeSpan.MaxValue);
            
            Assert.True(false);
        }

        [Fact]
        public void WhenAllObjectsAreLockedAndMaxPoolCapacityIsReachedThenCallerThreadIsLocked()
        {
            Assert.True(false);
        }
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

    internal class TestObjectsPool : ObjectsPool<PooledObject>
    {
        public TestObjectsPool(
            Func<PooledObject> creator,
            List<PooledObject> cleanedUpObjects,
                TimeSpan lifetime) : base(creator, lifetime)
        {
            _cleanedUpObjects = cleanedUpObjects;
        }

        protected override void Cleanup(PooledObject item)
        {
            _cleanedUpObjects.Add(item);
        }
        
        private readonly List<PooledObject> _cleanedUpObjects;
    }
}