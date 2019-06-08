using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;

namespace GONet.Utils
{
    public static class SerializationUtils
    {
        static SerializationUtils()
        {
            //MessagePackSerializer.SetDefaultResolver();

            CompositeResolver.RegisterAndSetAsDefault(
                // TODO figure out how to get this: TypelessObjectResolver
                DynamicObjectResolver.Instance,
                PrimitiveObjectResolver.Instance,
                StandardResolver.Instance,
                ContractlessStandardResolver.Instance
            );
        }

        /*
        public class IPersistentEventResolver : IFormatterResolver
        {
            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return StandardResolver.Instance;
            }
        }
        */

        /// <summary>
        /// This is the best general purpose object serializer GONet can provide.
        /// </summary>
        public static byte[] SerializeToBytes<T>(T @object)
        {
            return MessagePackSerializer.Serialize(@object);
            //return MessagePackSerializer.Serialize(@object, StandardResolverAllowPrivate.Instance);
        }

        /// <summary>
        /// This is the best general purpose object (de)serializer GONet can provide.
        /// </summary>
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }

        /// <summary>
        /// This is the best general purpose object (de)serializer GONet can provide.
        /// </summary>
        public static T DeserializeFromBytes<T>(ArraySegment<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}
