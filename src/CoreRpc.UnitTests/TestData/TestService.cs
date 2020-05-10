using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRpc.UnitTests.TestData
{
	[SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
	public class TestService : ITestService
	{
		public TestService(Action callback)
		{
			_callback = callback;
		}
        
		public int GetHashCodeOfMe(SerializableObject me) => me.IntProperty;

		public SerializableObject ConstructObject(int id, string name, double age) =>
			new SerializableObject
			{
				IntProperty = id,
				StringProperty = name,
				NestedObject = new NestedSerializableObject
				{
					DoubleProperty = age
				}
			};

		public void VoidMethod(string someString)
		{
			Console.WriteLine($"Called with string: {someString}");
			_callback();
		}

		public (int count, SerializableObject[] objects) GetObjects(int offset, int count)
		{
			return (offset + count, new[] { SerializableObject.GetTestInstance() });
		}

		public Task<int> GetHashCodeOfMeAsync(SerializableObject me)
		{
			return Task.FromResult(GetHashCodeOfMe(me));
		}

		public async Task<SerializableObject> ConstructObjectAsync(int id, string name, double age)
		{
			await Task.Delay(1);
			return ConstructObject(id, name, age);
		}

		public async Task<(int count, SerializableObject[] objects)> GetObjectsAsync(int offset, int count)
		{
			await Task.Delay(1);
			return GetObjects(offset, count);
		}

		public async Task VoidMethodAsync(string someString)
		{
			await Task.Delay(1);
			VoidMethod(someString);
		}

		public async Task<List<SerializableObject>> ConstructObjectsListAsync(Int32[] ids, String[] names, Double[] ages)
		{
			var result = await ConstructObjectAsync(ids.First(), names.First(), ages.First());
			return new[] {result}.ToList();
		}

		private readonly Action _callback;
		public const int ServicePort = 57001;
	}
}