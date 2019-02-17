namespace CoreRpc.Serialization
{
    public interface ISerializerFactory
    {
        ISerializer<T> CreateSerializer<T>();
    }
}