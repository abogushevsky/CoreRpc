using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CoreRpc.Serialization
{
	internal class ExceptionsSerializer : ISerializer<Exception>
	{
		public ExceptionsSerializer()
		{
			_binaryFormatter = new BinaryFormatter();
		}
		
		public byte[] Serialize(Exception item)
		{
			using (var stream = new MemoryStream())
			{
				_binaryFormatter.Serialize(stream, item);
				return stream.ToArray();
			}
		}

		public Exception Deserialize(byte[] serializedData)
		{
			using (var stream = new MemoryStream())
			{
				stream.Write(serializedData, 0, serializedData.Length);
				stream.Position = 0;
				return (Exception) _binaryFormatter.Deserialize(stream);
			}
		}

		public static ExceptionsSerializer Instance { get; } = new ExceptionsSerializer();

		private readonly BinaryFormatter _binaryFormatter;
	}
}