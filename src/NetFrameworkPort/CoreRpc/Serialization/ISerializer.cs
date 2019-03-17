namespace CoreRpc.Serialization
{
	public interface ISerializer<T>
	{
		byte[] Serialize(T item);

		T Deserialize(byte[] serializedData);
	}
}