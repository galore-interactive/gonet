using GONet.Utils;

namespace GONet.Generation
{
    /// <summary>
    /// TODO: make the main dll internals visible to editor dll so this can be made internal again
    /// </summary>
    public abstract class GONetParticipant_AutoMagicalSyncCompanion_Generated
    {
        protected GONetParticipant gonetParticipant;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;
        }

        internal abstract void SetAutoMagicalSyncValue(byte index, object value);

        internal abstract object GetAutoMagicalSyncValue(byte index);

        internal abstract void SerializeAll(BitStream bitStream_appendTo);

        internal abstract void SerializeSingle(BitStream bitStream_appendTo, byte singleIndex);

        /// <summary>
        ///  Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// </summary>
        internal abstract void DeserializeInitAll(BitStream bitStream_readFrom);

        /// <summary>
        ///  Deserializes a ginel value (using <paramref name="singleIndex"/> to know which) from <paramref name="bitStream_readFrom"/>
        ///  and uses them to modify appropriate member variables internally.
        /// </summary>
        internal abstract void DeserializeInitSingle(BitStream bitStream_readFrom, byte singleIndex);
    }
}
