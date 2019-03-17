using System.IO;
using MsgPack.Serialization;

namespace CoreRpc.Serialization.MessagePack
{
	public class MessagePackObjectSerializer<T> : ISerializer<T>
	{
		public MessagePackObjectSerializer()
		{
			_messagePackSerializer = SerializationContext.Default.GetSerializer<T>();
		}

		public byte[] Serialize(T item)
		{
			using(var stream = new MemoryStream()) 
			{
				_messagePackSerializer.Pack(stream, item);
				return stream.ToArray();
			}
		}

		public T Deserialize(byte[] serializedData)
		{
			using(var stream = new MemoryStream()) 
			{
				stream.Write(serializedData, 0, serializedData.Length);
				stream.Position = 0;
				return _messagePackSerializer.Unpack(stream);
			}
		}

		private readonly MessagePackSerializer<T> _messagePackSerializer;
	}
}