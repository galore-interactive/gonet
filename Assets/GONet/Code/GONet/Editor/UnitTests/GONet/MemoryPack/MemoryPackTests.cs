//#define SHAUN_MEMORY_PACK_GEN_IS_MESSING_UP

using MemoryPack;
using NUnit.Framework;
using System.Collections.Generic;


namespace Assets.Editor.UnitTests.MemoryPack
{
    [TestFixture]
    public class MemoryPackTests
    {
        [Test]
        public void TestMemoryPack_Plain()
        {
#if SHAUN_MEMORY_PACK_GEN_IS_MESSING_UP
            if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<Something>())
            {
                global::MemoryPack.MemoryPackFormatterProvider.Register(new SomethingFormatter_SHAUN());
            }
#endif
            //*
            Something something = new Something();
            something.map = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } };

            byte[] serialized = MemoryPackSerializer.Serialize(something, default, out int gnb, out bool gn);

            Something deserialized = MemoryPackSerializer.Deserialize<Something>(serialized);
            Assert.AreEqual(something.map.Count, deserialized.map.Count);
            Assert.AreEqual(something.map[1], deserialized.map[1]);
            Assert.AreEqual(something.map[3], deserialized.map[3]);
            //*/
        }
    }

    [MemoryPackable]
    public partial class Something
    {
        public Dictionary<int, int> map;

#if SHAUN_MEMORY_PACK_GEN_IS_MESSING_UP
        [global::MemoryPack.Internal.Preserve]
        public static void Serialize_SHAUN(ref MemoryPackWriter writer, ref Something? value)
        {

            if (value == null)
            {
                writer.WriteNullObjectHeader();
                goto END;
            }

            writer.WriteObjectHeader(1);
            writer.WriteValue(value.@map);

        END:

            return;
        }

        [global::MemoryPack.Internal.Preserve]
        public static void Deserialize_SHAUN(ref MemoryPackReader reader, ref Something? value)
        {

            if (!reader.TryReadObjectHeader(out var count))
            {
                value = default!;
                goto END;
            }



            global::System.Collections.Generic.Dictionary<int, int> __map;


            if (count == 1)
            {
                if (value == null)
                {
                    __map = reader.ReadValue<global::System.Collections.Generic.Dictionary<int, int>>();


                    goto NEW;
                }
                else
                {
                    __map = value.@map;

                    reader.ReadValue(ref __map);

                    goto SET;
                }

            }
            else if (count > 1)
            {
                MemoryPackSerializationException.ThrowInvalidPropertyCount(typeof(Something), 1, count);
                goto READ_END;
            }
            else
            {
                if (value == null)
                {
                    __map = default!;
                }
                else
                {
                    __map = value.@map;
                }


                if (count == 0) goto SKIP_READ;
                reader.ReadValue(ref __map); if (count == 1) goto SKIP_READ;

                SKIP_READ:
                if (value == null)
                {
                    goto NEW;
                }
                else
                {
                    goto SET;
                }

            }

        SET:

            value.@map = __map;
            goto READ_END;

        NEW:
            value = new Something()
            {
                @map = __map
            };
        READ_END:

        END:

            return;
        }
#endif
    }

#if SHAUN_MEMORY_PACK_GEN_IS_MESSING_UP
    sealed class SomethingFormatter_SHAUN : MemoryPackFormatter<Something>
    {
        [global::MemoryPack.Internal.Preserve]
        public override void Serialize(ref MemoryPackWriter writer, ref Something value)
        {
            Something.Serialize_SHAUN(ref writer, ref value);
        }

        [global::MemoryPack.Internal.Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref Something value)
        {
            Something.Deserialize_SHAUN(ref reader, ref value);
        }
    }
#endif
}
