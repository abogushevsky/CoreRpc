using System;

namespace CoreRpc.UnitTests.TestData
{
	[Serializable]
	public class SerializableObject
	{
		public string StringProperty { get; set; }

		public int IntProperty { get; set; }

		public NestedSerializableObject NestedObject { get; set; }

		public static SerializableObject GetTestInstance() =>
			new SerializableObject
			{
				StringProperty = TestString,
				IntProperty = TestInt,
				NestedObject = new NestedSerializableObject
				{
					DoubleProperty = TestDouble
				}
			};
		
		public const string TestString = "Test string";
		public const int TestInt = 7;
		public const double TestDouble = Math.PI;
	}
	
	[Serializable]
	public class NestedSerializableObject 
	{
		public double DoubleProperty { get; set; }
	}

	[Serializable]
	public class ImmutableSerializableObject
	{
		public ImmutableSerializableObject(string stringProperty, int intProperty, NestedSerializableObject nestedObject)
		{
			StringProperty = stringProperty;
			IntProperty = intProperty;
			NestedObject = nestedObject;
		}

		public string StringProperty { get; }

		public int IntProperty { get; }

		public NestedSerializableObject NestedObject { get; }

		public static ImmutableSerializableObject GetTestInstance() =>
			new ImmutableSerializableObject(
				SerializableObject.TestString, 
				SerializableObject.TestInt, 
				new NestedSerializableObject()
				{
					DoubleProperty = SerializableObject.TestDouble
				});
	}
}