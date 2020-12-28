using System;
using System.Threading;
using System.Threading.Tasks;
using CoreRpc.Networking.ConnectionPooling;
using CoreRpc.UnitTests.Utilities;
using CoreRpc.Utilities;
using Xunit;

namespace CoreRpc.UnitTests
{
    public class ConnectionPoolTest
    {
        public ConnectionPoolTest()
        {
            _poolsCleaner = new StalePooledObjectsCleaner(TimeSpan.FromMilliseconds(300));
        }
            
        [Fact]
        public async Task WhenFirstObjectIsCreatedAfterFirstCallWhenObjectIsReleasedThenSameObjectIsReturnedOnSecondCall()
        {
            var instanceNumber = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(
                    () => Task.FromResult(new PooledObject(++instanceNumber)), 
                    item => Task.CompletedTask),
                _poolsCleaner,
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
        public async Task WhenFirstObjectIsLockedAndSecondCallIsPerformedThenSecondObjectIsCreated()
        {
            var instanceNumber = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(
                    () => Task.FromResult(new PooledObject(++instanceNumber)), 
                    item => Task.CompletedTask),
                _poolsCleaner,
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
                new PooledItemManager<PooledObject>(
                    () => Task.FromResult(new PooledObject(++instanceNumber)), 
                    item => Task.CompletedTask),
                _poolsCleaner,
                TimeSpan.MaxValue,
                _testDateTimeProvider,
                1);
            
            var testObject = await testPool.Acquire();
            Assert.NotNull(testObject);
            Assert.Equal(1, testObject.InstanceNumber);

            Assert.False(Task.WaitAll(new Task[] {testPool.Acquire()}, 500));
            
            testPool.Release(testObject);
        }
        
        [Fact]
        public async Task WhenPooledObjectBecomesStaleThenItIsRemoved()
        {
            var instanceNumber = 0;
            var disposedObjectsCount = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(
                    () => Task.FromResult(new PooledObject(++instanceNumber)),
                    item =>
                    {
                        disposedObjectsCount++;
                        return Task.CompletedTask;
                    }),
                _poolsCleaner,
                TimeSpan.FromSeconds(1),
                _testDateTimeProvider,
                2);

            var fixedTime = DateTime.Now;
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            var firstObject = await testPool.Acquire();
            
            fixedTime += TimeSpan.FromMilliseconds(500);
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            var secondObject = await testPool.Acquire();
            
            testPool.Release(firstObject);
            fixedTime += TimeSpan.FromMilliseconds(500);
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            testPool.Release(secondObject);
            
            fixedTime += TimeSpan.FromMilliseconds(500);
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            Thread.Sleep(500);
            Assert.Equal(1, disposedObjectsCount);

            var freeObject = await testPool.Acquire();
            Assert.NotNull(freeObject);
            Assert.Equal(2, freeObject.InstanceNumber);
        }

        [Fact]
        public async Task WhenPooledObjectIsInUseThenItsExpirationTimeIsProlongated()
        {
            var instanceNumber = 0;
            var disposedObjectsCount = 0;
            var testPool = new ObjectsPool<PooledObject>(
                new PooledItemManager<PooledObject>(
                    () => Task.FromResult(new PooledObject(++instanceNumber)), 
                    item =>
                    {
                        disposedObjectsCount++;
                        return Task.CompletedTask;
                    }),
                _poolsCleaner,
                TimeSpan.FromSeconds(1),
                _testDateTimeProvider,
                2);

            var fixedTime = DateTime.Now;
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            var firstObject = await testPool.Acquire();
            
            fixedTime += TimeSpan.FromMilliseconds(500);
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            testPool.Release(firstObject);
            
            fixedTime += TimeSpan.FromMilliseconds(800);
            _testDateTimeProvider.FixDateTimeAt(fixedTime);
            Thread.Sleep(500);
            Assert.Equal(0, disposedObjectsCount);

            var freeObject = await testPool.Acquire();
            Assert.Equal(1, freeObject.InstanceNumber);
        }
        
        private readonly TestDateTimeProvider _testDateTimeProvider = 
            new TestDateTimeProvider(new DateTimeProvider());

        private StalePooledObjectsCleaner _poolsCleaner;
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