namespace GONet.PluginAPI
{
    /// <summary>
    /// TODO document this for people to understand.
    /// </summary>
    public interface IGONetAutoMagicalSync_CustomValueBlending
    {
        GONetSyncableValueTypes AppliesOnlyToGONetType { get; }

        string Description { get; }

        bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue);
    }
}
