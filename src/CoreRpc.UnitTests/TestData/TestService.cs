using System;

namespace CoreRpc.UnitTests.TestData
{
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

		private readonly Action _callback;
		public const int ServicePort = 57001;
	}
}