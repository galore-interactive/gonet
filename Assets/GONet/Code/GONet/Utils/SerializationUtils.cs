/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.Utils
{
    public static class SerializationUtils
    {
        static SerializationUtils()
        {
            //MessagePackSerializer.SetDefaultResolver();

            CompositeResolver.RegisterAndSetAsDefault(
                // TODO figure out how to get this: TypelessObjectResolver
#if !UNITY_WSA
#if !NET_STANDARD_2_0
                DynamicObjectResolver.Instance,
#endif
#endif
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

        public static T DeserializeFromBytes<T>(byte[] bytes, int offset, out int bytesRead)
        {
            return MessagePackSerializer.Deserialize<T>(bytes, offset, MessagePackSerializer.DefaultResolver, out bytesRead);
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

namespace GONet.Serializables
{
    [Serializable]
    public abstract class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        public int GetCustomKeyIndex(TKey key)
        {
            return keys.IndexOf(key);
        }

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();

            int keyCount = keys.Count;
            if (keys.Count != values.Count)
            {
                throw new Exception($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable.");
            }

            for (int i = 0; i < keyCount; ++i)
            {
                Add(keys[i], values[i]);
            }
        }
    }
}