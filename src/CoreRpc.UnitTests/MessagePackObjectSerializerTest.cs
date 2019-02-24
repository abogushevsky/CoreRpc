using System;
using CoreRpc.Serialization;
using CoreRpc.Serialization.MessagePack;
using CoreRpc.UnitTests.TestData;
using Xunit;

namespace CoreRpc.UnitTests
{
	public class MessagePackObjectSerializerTest
	{
		[Fact]
		public void CustomReferenceTypeSerializerTest()
		{
			var testObject = SerializableObject.GetTestInstance();

			var testSerializer = new MessagePackObjectSerializer<SerializableObject>();
			var serializedData = testSerializer.Serialize(testObject);

			Assert.NotEmpty(serializedData);

			var deserializedObject = testSerializer.Deserialize(serializedData);

			Assert.NotNull(deserializedObject);
			Assert.NotNull(deserializedObject.NestedObject);
			Assert.Equal(TestString, deserializedObject.StringProperty);
			Assert.Equal(TestInt, deserializedObject.IntProperty);
			Assert.Equal(TestDouble, deserializedObject.NestedObject.DoubleProperty);
		}

		[Fact]
		public void SimpleValueTypeSerializerTest()
		{
			var originalValue = 7;

			var testSerializer = new MessagePackObjectSerializer<int>();
			var serializedData = testSerializer.Serialize(originalValue);

			Assert.NotEmpty(serializedData);

			var deserializedValue = testSerializer.Deserialize(serializedData);

			Assert.Equal(originalValue, deserializedValue);

			originalValue = 1028;
			serializedData = testSerializer.Serialize(originalValue);

			Assert.NotEmpty(serializedData);

			deserializedValue = testSerializer.Deserialize(serializedData);

			Assert.Equal(originalValue, deserializedValue);
		}

		[Fact]
		public void StringSerializerTest()
		{
			var testSerializer = new MessagePackObjectSerializer<string>();
			var serializedData = testSerializer.Serialize(TestString);

			Assert.NotEmpty(serializedData);

			var deserializedString = testSerializer.Deserialize(serializedData);

			Assert.False(string.IsNullOrEmpty(deserializedString));
			Assert.Equal(TestString, deserializedString);
		}

		[Fact]
		public void TupleSerializerTest()
		{
			const int testTimeSpanMinutes = 5;
			const int testIntValue = 15;

			var testSerializer = new MessagePackObjectSerializer<(TimeSpan, SerializableObject, int)>();
			var testTuple = (TimeSpan.FromMinutes(testTimeSpanMinutes), SerializableObject.GetTestInstance(), testIntValue);

			var serializedData = testSerializer.Serialize(testTuple);

			Assert.NotEmpty(serializedData);

			var deserializedTuple = testSerializer.Deserialize(serializedData);

			Assert.Equal(testTuple.Item1, deserializedTuple.Item1);
			Assert.Equal(testTuple.Item2.StringProperty, deserializedTuple.Item2.StringProperty);
			Assert.Equal(testTuple.Item2.IntProperty, deserializedTuple.Item2.IntProperty);
			Assert.Equal(testTuple.Item2.NestedObject.DoubleProperty, deserializedTuple.Item2.NestedObject.DoubleProperty);
			Assert.Equal(testTuple.Item3, deserializedTuple.Item3);
		}

		[Fact]
		public void ImmutableTypeSerializerTest()
		{
			var testObject = ImmutableSerializableObject.GetTestInstance();

			var testSerializer = new MessagePackObjectSerializer<ImmutableSerializableObject>();
			var serializedData = testSerializer.Serialize(testObject);

			Assert.NotEmpty(serializedData);

			var deserializedObject = testSerializer.Deserialize(serializedData);

			Assert.NotNull(deserializedObject);
			Assert.NotNull(deserializedObject.NestedObject);
			Assert.Equal(TestString, deserializedObject.StringProperty);
			Assert.Equal(TestInt, deserializedObject.IntProperty);
			Assert.Equal(TestDouble, deserializedObject.NestedObject.DoubleProperty);
		}

		[Fact]
		public async void ExceptionSerializerTest()
		{
			const string exceptionArgument = "some parameter";
			var testObject = new ArgumentNullException(exceptionArgument);
			var testSerializer = new ExceptionsSerializer();
			
			var serializedData = testSerializer.Serialize(testObject);
			Assert.NotEmpty(serializedData);

			var deserializedObject = testSerializer.Deserialize(serializedData);
			var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => throw deserializedObject);
			Assert.Equal(exceptionArgument, exception.ParamName);
		}

		private const string TestString = "Test string";
		private const int TestInt = 7;
		private const double TestDouble = 3.14;
	}
}
