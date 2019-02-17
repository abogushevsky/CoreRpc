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

        public ISerializer<T> CreateSerializer<T>() 
        {
            var cacheKey = typeof(T);
            if (!_cachedSerializers.TryGetValue(cacheKey, out object result))
            {
                result = new MessagePackObjectSerializer<T>();
                _cachedSerializers[cacheKey] = result;
            }

            return (ISerializer<T>) result;
        }

        private readonly ConcurrentDictionary<Type, object> _cachedSerializers;
    }
}