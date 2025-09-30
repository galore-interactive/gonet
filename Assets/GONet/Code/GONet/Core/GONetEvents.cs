/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using GONet.Utils;
using MemoryPack;
using NetcodeIO.NET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace GONet
{
    #region base stuffs

    /// <summary>
    /// This alone does not mean much.  Implement either <see cref="ITransientEvent"/> or <see cref="IPersistentEvent"/>.
    /// </summary>
    public partial interface IGONetEvent
    {
        long OccurredAtElapsedTicks { get; }
    }

    /// <summary>
    /// Implement this to indicate the information herein is only relevant while it is happening and while subscribers are notified and NOT to be passed along to newly connecting clients and can safely be skipped over during replay skip-ahead or fast-forward.
    ///
    /// POOLING COMPATIBILITY: Events implementing ITransientEvent are typically compatible with object pooling via ISelfReturnEvent,
    /// as they are processed immediately and not stored for future use. This allows for efficient memory reuse patterns.
    /// </summary>
    public partial interface ITransientEvent : IGONetEvent
    {
        /// <summary>
        /// GONet sends all events to all other machines in the simulation/game by default.
        /// This need to return true if this event (type) is supposed to only be sent to the
        /// singular/first recipient (and not subsequently relayed to the others connected to it) 
        /// when one of the following typical APIs is called:
        /// -<see cref="GONetMain.SendBytesToRemoteConnection(GONetConnection, byte[], int, byte)"/>
        /// -<see cref="GONetConnection.SendMessageOverChannel(byte[], int, byte)"/>
        /// </summary>
        [MemoryPackIgnore]
        bool IsSingularRecipientOnly { get => false; } // TODO consider moving this up to IGONetEvent if applicable to IPersistentEvent as well
    }

    /// <summary>
    /// Implement this to indicate the information herein should be stored and sent to newly connecting clients.
    /// These events are kept in GONet's persistentEventsThisSession collection for late-joining client delivery.
    ///
    /// CRITICAL: Events implementing IPersistentEvent should NOT also implement ISelfReturnEvent.
    ///
    /// POOLING INCOMPATIBILITY: Persistent events are stored by reference in GONet's persistence system
    /// (see OnPersistentEvent_KeepTrack in GONet.cs). If these events were pooled and returned after execution,
    /// their data would be cleared/corrupted when sent to late-joining clients, causing critical data integrity issues.
    ///
    /// DESIGN DECISION: GONet prioritizes data safety over memory efficiency for persistent events.
    /// The cost of allocating new objects is acceptable given:
    /// - Persistent events are used less frequently than transient events
    /// - Data integrity is more critical than micro-optimizations for these events
    /// - The pattern aligns with existing GONet persistent events (e.g., InstantiateGONetParticipantEvent)
    ///
    /// For performance-critical scenarios, consider using transient events where persistence is not required.
    /// </summary>
    public partial interface IPersistentEvent : IGONetEvent { }

    /// <summary>
    /// Tack this on to any event type to ensure calls to <see cref="GONetEventBus.Publish{T}(T, uint?)"/> only publish locally (i.e., not sent across the network to anyone else)
    /// </summary>
    public interface ILocalOnlyPublish { }

    /// <summary>
    /// This is something that would only apply to event class that implement <see cref="IPersistentEvent"/> that get queued up on server and sent to newly connecting clients.
    /// Instances that implement this tell GONet to look for instances of the other events of type <see cref="OtherEventTypesCancelledOut"/> and see if they cancel one another out 
    /// so these messages can be removed from consideration in pairs as to not send these events anywhere.
    /// Example: <see cref="InstantiateGONetParticipantEvent"/> is cancelled out by <see cref="DestroyGONetParticipantEvent"/>.
    /// </summary>
    public interface ICancelOutOtherEvents
    {
        /// <summary>
        /// At time of writing, this should only be types that implement <see cref="IPersistentEvent"/>.
        /// </summary>
        Type[] OtherEventTypesCancelledOut { get; }

        /// <summary>
        /// This will only get called when <paramref name="otherEvent"/> is of the type <see cref="OtherEventTypesCancelledOut"/>.
        /// </summary>
        bool DoesCancelOutOtherEvent(IGONetEvent otherEvent);
    }

    #endregion

    [MemoryPackable]
    public partial class ServerSaysClientInitializationCompletion : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new NotImplementedException();
    }

    [MemoryPackable]
    public partial class AutoMagicalSync_AllCurrentValues_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    [MemoryPackable]
    public partial class AutoMagicalSync_ValueChanges_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    [MemoryPackable]
    public partial class AutoMagicalSync_ValuesNowAtRest_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    [MemoryPackable]
    public partial class OwnerAuthorityIdAssignmentEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    [MemoryPackable]
    public partial class ClientRemotelyControlledGONetIdServerBatchAssignmentEvent : ITransientEvent
    {
        [MemoryPackIgnore]
        public bool IsSingularRecipientOnly => true;

        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetIdRawBatchStart { get; set; }
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its OnEnable() method called.
    /// IMPORTANT: This is not the proper time to indicate it is ready for use by other game logic, for that use <see cref="GONetParticipantStartedEvent"/> instead to be certain.
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipantEnabledEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        public GONetParticipantEnabledEvent(uint gonetId)
        {
            GONetId = gonetId;
        }
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its Start() method called and it is ready to be used by other game logic.
    /// IMPORTANT: When this is fired/published, this is the first time it is certain that the <see cref="GONetParticipant.GONetId"/> value is fully assigned!
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipantStartedEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        [MemoryPackConstructor] GONetParticipantStartedEvent() { }

        public GONetParticipantStartedEvent(GONetParticipant gonetParticipant)
        {
            GONetId = gonetParticipant.GONetId;
        }
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its OnDisable() method called and will no longer be active in the game.
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipantDisabledEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        [MemoryPackConstructor] public GONetParticipantDisabledEvent() { }

        public GONetParticipantDisabledEvent(GONetParticipant gonetParticipant)
        {
            GONetId = gonetParticipant.GONetId;
        }
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its related 
    /// <see cref="GONet.Generation.GONetParticipant_AutoMagicalSyncCompanion_Generated.DeserializeInitAll(BitByBitByteArrayBuilder, long)"/> 
    /// method called.
    /// This is useful because individual SyncEvents will NOT be fired in those cases and there may be a need to do something once initial 
    /// values are known (from remote source/authority).
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipantDeserializeInitAllCompletedEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        [MemoryPackConstructor] public GONetParticipantDeserializeInitAllCompletedEvent() { }

        public GONetParticipantDeserializeInitAllCompletedEvent(GONetParticipant gonetParticipant)
        {
            GONetId = gonetParticipant.GONetId;
        }
    }

    [MemoryPackable]
    public partial class RequestMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        public long OccurredAtElapsedTicks { get; set; }

        public long UID;

        public RequestMessage(long occurredAtElapsedTicks)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;

            UID = GUID.Generate().AsInt64();
        }
    }

    [MemoryPackable]
    public partial class ResponseMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        public long OccurredAtElapsedTicks { get; set; }

        public long CorrelationRequestUID;

        public ResponseMessage(long occurredAtElapsedTicks, long correlationRequestUID)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            CorrelationRequestUID = correlationRequestUID;
        }
    }

    [MemoryPackable]
    public partial class InstantiateGONetParticipantEvent : IPersistentEvent
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// this is the information necessary to lookup the source <see cref="UnityEngine.GameObject"/> from which to use as the template in order to call <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// TODO add the persisted int->string lookup table that is updated each time a new design time location is encountered (at design time...duh)..so this can be an int!
        /// </summary>
        public string DesignTimeLocation;

        public uint GONetId;

        public ushort OwnerAuthorityId;

        public Vector3 Position;

        public Quaternion Rotation;

        public string InstanceName;

        public string ParentFullUniquePath;

        public uint GONetIdAtInstantiation;

        public bool ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority;

        internal static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = gonetParticipant.DesignTimeLocation;
            @event.ParentFullUniquePath = gonetParticipant.transform.parent == null ? string.Empty : HierarchyUtils.GetFullUniquePath(gonetParticipant.transform.parent.gameObject);

            @event.GONetId = gonetParticipant.GONetId;
            @event.GONetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;
            @event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority = false;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }

        internal static InstantiateGONetParticipantEvent Create_WithNonAuthorityInfo(GONetParticipant gonetParticipant, string nonAuthorityAlternate_designTimeLocation)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = nonAuthorityAlternate_designTimeLocation;

            @event.GONetId = gonetParticipant.GONetId;
            @event.GONetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;
            @event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority = false;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }

        internal static InstantiateGONetParticipantEvent Create_WithRemotelyControlledByInfo(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = gonetParticipant.DesignTimeLocation;
            @event.ParentFullUniquePath = gonetParticipant.transform.parent == null ? string.Empty : HierarchyUtils.GetFullUniquePath(gonetParticipant.transform.parent.gameObject);

            @event.GONetId = gonetParticipant.GONetId;
            @event.GONetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;
            @event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority = true;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }
    }

    /// <summary>
    /// This is used internally to command all machines in the system to destroy the <see cref="GONetParticipant"/> and its <see cref="GameObject"/>.
    /// </summary>
    [MemoryPackable]
    public partial class DestroyGONetParticipantEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        public uint GONetId;

        static readonly Type[] otherEventsTypeCancelledOut = new[] {
            typeof(InstantiateGONetParticipantEvent),
            typeof(ValueMonitoringSupport_NewBaselineEvent),
            typeof(ValueMonitoringSupport_BaselineExpiredEvent)
        };

        [MemoryPackIgnore]
        public Type[] OtherEventTypesCancelledOut => otherEventsTypeCancelledOut;

        public bool DoesCancelOutOtherEvent(IGONetEvent otherEvent)
        {
            if (otherEvent is InstantiateGONetParticipantEvent)
            {
                InstantiateGONetParticipantEvent instantiationEvent = (InstantiateGONetParticipantEvent)otherEvent;
                return instantiationEvent.GONetId != GONetParticipant.GONetId_Unset && 
                    (instantiationEvent.GONetId == GONetId || instantiationEvent.GONetId == GONetMain.GetGONetIdAtInstantiation(GONetId));
            }
            else if (otherEvent is ValueMonitoringSupport_NewBaselineEvent)
            {
                ValueMonitoringSupport_NewBaselineEvent newBaselineEvent = (ValueMonitoringSupport_NewBaselineEvent)otherEvent;
                return newBaselineEvent.GONetId != GONetParticipant.GONetId_Unset && newBaselineEvent.GONetId == GONetId;
            }
            else if (otherEvent is ValueMonitoringSupport_BaselineExpiredEvent)
            {
                ValueMonitoringSupport_BaselineExpiredEvent expiredBaselineEvent = (ValueMonitoringSupport_BaselineExpiredEvent)otherEvent;
                return expiredBaselineEvent.GONetId != GONetParticipant.GONetId_Unset && expiredBaselineEvent.GONetId == GONetId;
            }

            return false;
        }
    }

    [MemoryPack.MemoryPackUnion(0, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Single))]
    [MemoryPack.MemoryPackUnion(1, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2))]
    [MemoryPack.MemoryPackUnion(2, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3))]
    [MemoryPack.MemoryPackUnion(3, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4))]
    [MemoryPack.MemoryPackUnion(4, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion))]
    [MemoryPack.MemoryPackUnion(5, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Boolean))]
    [MemoryPack.MemoryPackUnion(6, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Byte))]
    [MemoryPack.MemoryPackUnion(7, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_SByte))]
    [MemoryPack.MemoryPackUnion(8, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int16))]
    [MemoryPack.MemoryPackUnion(9, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt16))]
    [MemoryPack.MemoryPackUnion(10, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int32))]
    [MemoryPack.MemoryPackUnion(11, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt32))]
    [MemoryPack.MemoryPackUnion(12, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int64))]
    [MemoryPack.MemoryPackUnion(13, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt64))]
    [MemoryPack.MemoryPackUnion(14, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Double))]
    [MemoryPackable]
    public abstract partial class ValueMonitoringSupport_NewBaselineEvent : IPersistentEvent
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        public uint GONetId { get; set; }

        public byte ValueIndex { get; set; }
    }

    #region ValueMonitoringSupport_NewBaselineEvent child classes for each supported type
    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Single : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Single NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2 : ValueMonitoringSupport_NewBaselineEvent
    {
        public UnityEngine.Vector2 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3 : ValueMonitoringSupport_NewBaselineEvent
    {
        public UnityEngine.Vector3 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4 : ValueMonitoringSupport_NewBaselineEvent
    {
        public UnityEngine.Vector4 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion : ValueMonitoringSupport_NewBaselineEvent
    {
        public UnityEngine.Quaternion NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Boolean : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Boolean NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Byte : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Byte NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_SByte : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.SByte NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Int16 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Int16 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_UInt16 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.UInt16 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Int32 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Int32 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_UInt32 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.UInt32 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Int64 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Int64 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_UInt64 : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.UInt64 NewBaselineValue { get; set; }
    }

    [MemoryPackable]
    public partial class ValueMonitoringSupport_NewBaselineEvent_System_Double : ValueMonitoringSupport_NewBaselineEvent
    {
        public System.Double NewBaselineValue { get; set; }
    }
    #endregion

    /// <summary>
    /// <para>
    /// This class uses a feature of the <see cref="ICancelOutOtherEvents"/> processing to allow us to only send newly connecting clients just
    /// the most recent <see cref="ValueMonitoringSupport_NewBaselineEvent"/> instead of the entire history along the way of the game.
    /// </para>
    /// <para>
    /// IMPORTANT: The semantics of this class and how GONet promises to use it is: for every instance of this class/event published, it is 
    /// immediately followed by publishing a corresponding instance of <see cref="ValueMonitoringSupport_NewBaselineEvent"/>.
    /// </para>
    /// </summary>
    [MemoryPackable]
    public partial class ValueMonitoringSupport_BaselineExpiredEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        public uint GONetId { get; set; }

        public byte ValueIndex { get; set; }

        static readonly Type[] otherEventTypesCancelledOut = new[] { typeof(ValueMonitoringSupport_NewBaselineEvent) };

        [MemoryPackIgnore]
        public Type[] OtherEventTypesCancelledOut => otherEventTypesCancelledOut;

        public bool DoesCancelOutOtherEvent(IGONetEvent otherEvent)
        {
            ValueMonitoringSupport_NewBaselineEvent newBaselineEvent = (ValueMonitoringSupport_NewBaselineEvent)otherEvent;
            return newBaselineEvent.GONetId != GONetParticipant.GONetId_Unset && newBaselineEvent.GONetId == GONetId
                && newBaselineEvent.ValueIndex == ValueIndex
                ; // TODO && depending on if the evaluation order is important or not may need to check if this is the one immediately after the new baseline but only if OccurredAtElapsedTicks value is populated and reliable to reference for this
        }
    }

    [MemoryPackable]
    public partial class PersistentEvents_Bundle : ITransientEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        public LinkedList<IPersistentEvent> PersistentEvents;

        public PersistentEvents_Bundle() { }

        [MemoryPackConstructor]
        public PersistentEvents_Bundle(long occurredAtElapsedTicks, LinkedList<IPersistentEvent> persistentEvents) : this()
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            PersistentEvents = persistentEvents;
        }
    }

    [MemoryPackable]
    public partial class ClientTypeFlagsChangedEvent : ITransientEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        public ushort ClientAuthorityId { get; set; }

        public ClientTypeFlags FlagsPrevious { get; set; }

        public ClientTypeFlags FlagsNow { get; set; }

        public ClientTypeFlagsChangedEvent(long occurredAtElapsedTicks, ushort clientAuthorityId, ClientTypeFlags flagsPrevious, ClientTypeFlags flagsNow)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            ClientAuthorityId = clientAuthorityId;
            FlagsPrevious = flagsPrevious;
            FlagsNow = flagsNow;
        }
    }

    /// <summary>
    /// IMPORTANT: This event is initiated (and first published) from a client once the state changes locally on that client, which is slightly different than <see cref="RemoteClientStateChangedEvent"/>
    /// </summary>
    [MemoryPackable]
    public partial class ClientStateChangedEvent : ITransientEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// NOTE: When processing this event on the server, this value can be used to lookup the corresponding <see cref="GONetRemoteClient"/> instance by 
        ///       calling <see cref="GONetServer.TryGetClientByConnectionUID(ulong, out GONetRemoteClient)"/>.
        /// </summary>
        public ulong InitiatingClientConnectionUID { get; set; }

        public ClientState StatePrevious { get; set; }

        public ClientState StateNow { get; set; }

        public ClientStateChangedEvent(long occurredAtElapsedTicks, ulong initiatingClientConnectionUID, ClientState statePrevious, ClientState stateNow)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            InitiatingClientConnectionUID = initiatingClientConnectionUID;
            StatePrevious = statePrevious;
            StateNow = stateNow;
        }
    }

    /// <summary>
    /// IMPORTANT: This event is initiated (and first published) from the server once the state changes locally on the server for a client, which is slightly different than <see cref="ClientStateChangedEvent"/>
    ///            When this event is fired and is received/processed on a client, the client's local data representing the client state may likely NOT be updated to reflect the state change
    ///            and if it is important that the client IS updated to reflect the state change, subscribe to <see cref="ClientStateChangedEvent"/> instead.
    /// </summary>
    [MemoryPackable]
    public partial class RemoteClientStateChangedEvent : ITransientEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// NOTE: When processing this event on the server, this value can be used to lookup the corresponding <see cref="GONetRemoteClient"/> instance by 
        ///       calling <see cref="GONetServer.TryGetClientByConnectionUID(ulong, out GONetRemoteClient)"/>.
        /// </summary>
        public ulong InitiatingClientConnectionUID { get; set; }

        /// <summary>
        /// Since this event initiates server side and the server will not have as many possible states for a client, the only values this might be are:
        /// <see cref="ClientState.Connected"/> and <see cref="ClientState.Disconnected"/> TODO: see about getting all other values working as well!
        /// </summary>
        public ClientState StatePrevious { get; set; }

        public ClientState StateNow { get; set; }

        public RemoteClientStateChangedEvent(long occurredAtElapsedTicks, ulong initiatingClientConnectionUID, ClientState statePrevious, ClientState stateNow)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            InitiatingClientConnectionUID = initiatingClientConnectionUID;
            StatePrevious = statePrevious;
            StateNow = stateNow;
        }
    }

    public enum SyncEvent_ValueChangeProcessedExplanation : byte
    {
        OutboundToOthers = 1,

        InboundFromOther,

        BlendingBetweenInboundValuesFromOther,
    }

    /// <summary>
    /// Once this event is sent through <see cref="GONetEventBus.Publish{T}(T, uint?)"/>, it will automatically have <see cref="Return"/> called on it.
    /// At time of writing, this is to support (automatic) object pool usage for better memory/garbage/GC performance.
    ///
    /// IMPORTANT COMPATIBILITY CONSTRAINT: Events implementing ISelfReturnEvent should NOT also implement IPersistentEvent.
    ///
    /// REASON: GONet's persistence system stores direct references to persistent events for late-joining clients.
    /// If a persistent event also implemented ISelfReturnEvent, its Return() method would clear the event data
    /// after processing, corrupting the data when it's later sent to new clients.
    ///
    /// DESIGN PATTERN:
    /// - Transient events (ITransientEvent) + ISelfReturnEvent = SAFE (immediate processing, pooling enabled)
    /// - Persistent events (IPersistentEvent) + NO pooling = SAFE (stored references, data preserved)
    /// - Persistent events + ISelfReturnEvent = DANGEROUS (data corruption for late-joining clients)
    ///
    /// This constraint ensures data integrity in GONet's event persistence mechanism.
    /// </summary>
    public interface ISelfReturnEvent
    {
        void Return();
    }

    public interface IHaveRelatedGONetId
    {
        uint GONetId { get; set; }
    }

    [MemoryPackable]
    public partial class InternalOnlyMemoryPackComilationAssistanceForGenerated : SyncEvent_ValueChangeProcessed
    {
        public override GONetSyncableValue ValuePrevious => throw new NotImplementedException();

        public override GONetSyncableValue ValueNew => throw new NotImplementedException();

        public override SyncEvent_GeneratedTypes SyncEvent_GeneratedType => throw new NotImplementedException();

        public override void Return()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This represents that a sync value change has been processed locally.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MemoryPack.MemoryPackUnion(ushort.MaxValue, typeof(InternalOnlyMemoryPackComilationAssistanceForGenerated))]
    [MemoryPackable]
    public abstract partial class SyncEvent_ValueChangeProcessed : ITransientEvent, ILocalOnlyPublish, ISelfReturnEvent
    {
        public double OccurredAtElapsedSeconds { get => TimeSpan.FromTicks(OccurredAtElapsedTicks).TotalSeconds; set { OccurredAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [MemoryPackIgnore] public long OccurredAtElapsedTicks { get; set; }

        public double ProcessedAtElapsedSeconds { get => TimeSpan.FromTicks(ProcessedAtElapsedTicks).TotalSeconds; set { ProcessedAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [MemoryPackIgnore] public long ProcessedAtElapsedTicks;

        public ushort RelatedOwnerAuthorityId;
        public uint GONetId;

        [MemoryPackIgnore] public byte CodeGenerationId;

        public byte SyncMemberIndex;
        public SyncEvent_ValueChangeProcessedExplanation Explanation;

        [MemoryPackIgnore] public abstract GONetSyncableValue ValuePrevious { get; }
        [MemoryPackIgnore] public abstract GONetSyncableValue ValueNew { get; }
        [MemoryPackIgnore] public abstract SyncEvent_GeneratedTypes SyncEvent_GeneratedType { get; }

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// </summary>
        public SyncEvent_ValueChangeProcessed() { }

        public abstract void Return();
    }

    [MemoryPackable]
    public partial class SyncEvent_PersistenceBundle
    {
        public Queue<SyncEvent_ValueChangeProcessed> bundle;

        public static readonly SyncEvent_PersistenceBundle Instance = new SyncEvent_PersistenceBundle();
    }

    /// <summary>
    /// This represents that a sync value change has been processed.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MemoryPackable]
    public sealed partial class SyncEvent_Time_ElapsedTicks_SetFromAuthority : SyncEvent_ValueChangeProcessed
    {
        public double ElapsedSeconds_Previous { get => TimeSpan.FromTicks(ElapsedTicks_Previous).TotalSeconds; set { ElapsedTicks_Previous = TimeSpan.FromSeconds(value).Ticks; } }
        [MemoryPackIgnore] public long ElapsedTicks_Previous { get; private set; }

        public double ElapsedSeconds_New { get => TimeSpan.FromTicks(ElapsedTicks_New).TotalSeconds; set { ElapsedTicks_New = TimeSpan.FromSeconds(value).Ticks; } }
        [MemoryPackIgnore] public long ElapsedTicks_New { get; private set; }

        public double RoundTripSeconds_Latest { get; set; }
        public double RoundTripSeconds_RecentAverage { get; set; }
        public float RoundTripMilliseconds_LowLevelTransportProtocol { get; set; }

        public override GONetSyncableValue ValuePrevious => ElapsedTicks_Previous;
        public override GONetSyncableValue ValueNew => ElapsedTicks_New;
        public override SyncEvent_GeneratedTypes SyncEvent_GeneratedType => throw new NotImplementedException();

        static readonly ObjectPool<SyncEvent_Time_ElapsedTicks_SetFromAuthority> pool = new ObjectPool<SyncEvent_Time_ElapsedTicks_SetFromAuthority>(5, 1);
        static readonly ConcurrentQueue<SyncEvent_Time_ElapsedTicks_SetFromAuthority> returnQueue_onceOnBorrowThread = new ConcurrentQueue<SyncEvent_Time_ElapsedTicks_SetFromAuthority>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, long, long)"/>.
        /// </summary>
        public SyncEvent_Time_ElapsedTicks_SetFromAuthority() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_Time_ElapsedTicks_SetFromAuthority)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_Time_ElapsedTicks_SetFromAuthority Borrow(long elapsedTicks_previous, long elapsedTicks_new, float roundTripSeconds_latest, float roundTripSeconds_recentAverage, float roundTripMilliseconds_LowLevelTransportProtocol)
        {
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_Time_ElapsedTicks_SetFromAuthority autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();

            @event.RoundTripSeconds_Latest = roundTripSeconds_latest;
            @event.RoundTripSeconds_RecentAverage = roundTripSeconds_recentAverage;
            @event.RoundTripMilliseconds_LowLevelTransportProtocol = roundTripMilliseconds_LowLevelTransportProtocol;

            @event.Explanation = SyncEvent_ValueChangeProcessedExplanation.InboundFromOther;
            @event.OccurredAtElapsedTicks = elapsedTicks_previous;
            @event.RelatedOwnerAuthorityId = GONetMain.OwnerAuthorityId_Server;

            { // meaningless for this event:
                @event.GONetId = GONetParticipant.GONetId_Unset;
                @event.CodeGenerationId = 0;
                @event.SyncMemberIndex = 0;
            }

            @event.ElapsedTicks_Previous = elapsedTicks_previous;
            @event.ElapsedTicks_New = elapsedTicks_new;

            return @event;
        }

        public override void Return()
        {
            Return(this);
        }

        public static void Return(SyncEvent_Time_ElapsedTicks_SetFromAuthority borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    #region Scene Management Events

    /// <summary>
    /// Indicates which loading system to use for the scene.
    /// </summary>
    public enum SceneLoadType : byte
    {
        /// <summary>
        /// Traditional: Scene in Build Settings, loaded by name/build index
        /// </summary>
        BuildSettings = 0,

        /// <summary>
        /// Modern: Scene loaded via Unity Addressables system
        /// </summary>
        Addressables = 1
    }

    /// <summary>
    /// Persistent event for scene loading.
    /// Server publishes this when loading a scene, clients receive and load accordingly.
    /// Late-joining clients receive this event to sync scene state.
    /// </summary>
    [MemoryPackable]
    public partial class SceneLoadEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// Scene name (for Build Settings) or addressable key (for Addressables)
        /// </summary>
        public string SceneName;

        /// <summary>
        /// Build index for scenes in Build Settings (fallback identifier)
        /// </summary>
        public int SceneBuildIndex = -1;

        /// <summary>
        /// Which loading system to use
        /// </summary>
        public SceneLoadType LoadType;

        /// <summary>
        /// Single or Additive loading mode
        /// </summary>
        public UnityEngine.SceneManagement.LoadSceneMode Mode;

        /// <summary>
        /// For Addressables: Whether to activate scene immediately after loading
        /// </summary>
        public bool ActivateOnLoad = true;

        /// <summary>
        /// For Addressables: Loading priority
        /// </summary>
        public int Priority = 100;
    }

    /// <summary>
    /// Persistent event for scene unloading.
    /// Cancels out corresponding SceneLoadEvent for late-joining clients.
    /// </summary>
    [MemoryPackable]
    public partial class SceneUnloadEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// Scene name or addressable key to unload
        /// </summary>
        public string SceneName;

        /// <summary>
        /// Build index for fallback identification
        /// </summary>
        public int SceneBuildIndex = -1;

        /// <summary>
        /// Which loading system was used
        /// </summary>
        public SceneLoadType LoadType;

        static readonly Type[] otherEventsTypeCancelledOut = new[] {
            typeof(SceneLoadEvent)
        };

        [MemoryPackIgnore]
        public Type[] OtherEventTypesCancelledOut => otherEventsTypeCancelledOut;

        public bool DoesCancelOutOtherEvent(IGONetEvent otherEvent)
        {
            if (otherEvent is SceneLoadEvent loadEvent)
            {
                // Cancel if same scene name
                if (!string.IsNullOrEmpty(SceneName) && SceneName == loadEvent.SceneName)
                    return true;

                // Fallback: cancel if same build index (and both are build settings scenes)
                if (SceneBuildIndex >= 0 &&
                    SceneBuildIndex == loadEvent.SceneBuildIndex &&
                    LoadType == SceneLoadType.BuildSettings &&
                    loadEvent.LoadType == SceneLoadType.BuildSettings)
                    return true;
            }

            return false;
        }
    }

    #endregion
}

