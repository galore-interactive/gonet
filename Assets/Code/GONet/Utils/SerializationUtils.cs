/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

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
