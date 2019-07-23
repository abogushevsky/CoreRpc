using System;
using System.Collections.Concurrent;

namespace CoreRpc.Serialization.MessagePack
{
    public class MessagePackSerializerFactory : ISerializerFactory
    {
        public MessagePackSerializerFactory()
        {
            _cachedSerializers = new ConcurrentDictionary<Type, object>();
        }

        public ISerializer<T> CreateSerializer<T>() => 
            (ISerializer<T>) _cachedSerializers.GetOrAdd(typeof(T), _ => new MessagePackObjectSerializer<T>());

        private readonly ConcurrentDictionary<Type, object> _cachedSerializers;
    }
}
