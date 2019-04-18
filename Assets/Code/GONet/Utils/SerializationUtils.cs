using MessagePack;
using MessagePack.Resolvers;

namespace GONet.Utils
{
    public static class SerializationUtils
    {
        static SerializationUtils()
        {
            MessagePackSerializer.SetDefaultResolver(ContractlessStandardResolver.Instance);
        }

        /// <summary>
        /// This is the best general purpose object serializer GONet can provide.
        /// </summary>
        public static byte[] SerializeToBytes<T>(T @object)
        {
            return MessagePackSerializer.Serialize(@object, ContractlessStandardResolverAllowPrivate.Instance);
        }

        /// <summary>
        /// This is the best general purpose object (de)serializer GONet can provide.
        /// </summary>
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}
