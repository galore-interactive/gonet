/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
using MessagePack;
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
    /// Implement this to this indicates the information herein is only relevant while it is happening and while subscribers are notified and NOT to be passed along to newly connecting clients and can safely be skipped over during replay skip-ahead or fast-forward.
    /// </summary>
    public partial interface ITransientEvent : IGONetEvent { }

    /// <summary>
    /// Implement this for persistent events..opposite of extending <see cref="ITransientEvent"/> (see the comments there for more).
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

    public struct ServerSaysClientInitializationCompletion : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new NotImplementedException();
    }

    public struct AutoMagicalSync_AllCurrentValues_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    public struct AutoMagicalSync_ValueChanges_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    public struct AutoMagicalSync_ValuesNowAtRest_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    public struct OwnerAuthorityIdAssignmentEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its OnEnable() method called.
    /// IMPORTANT: This is not the proper time to indicate it is ready for use by other game logic, for that use <see cref="GONetParticipantStartedEvent"/> instead to be certain.
    /// </summary>
    public struct GONetParticipantEnabledEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
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
    /// </summary>
    public struct GONetParticipantStartedEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        public GONetParticipantStartedEvent(GONetParticipant gonetParticipant)
        {
            GONetId = gonetParticipant.GONetId;
        }
    }

    /// <summary>
    /// Fired locally-only when any <see cref="GONetParticipant"/> finished having its OnDisable() method called and will no longer be active in the game.
    /// </summary>
    public struct GONetParticipantDisabledEvent : ITransientEvent, ILocalOnlyPublish, IHaveRelatedGONetId
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        public uint GONetId { get; set; }

        public GONetParticipantDisabledEvent(GONetParticipant gonetParticipant)
        {
            GONetId = gonetParticipant.GONetId;
        }
    }

    [MessagePackObject]
    public struct RequestMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public long UID;

        public RequestMessage(long occurredAtElapsedTicks)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;

            UID = GUID.Generate().AsInt64();
        }
    }

    [MessagePackObject]
    public struct ResponseMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public long CorrelationRequestUID;

        public ResponseMessage(long occurredAtElapsedTicks, long correlationRequestUID)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            CorrelationRequestUID = correlationRequestUID;
        }
    }

    [MessagePackObject]
    public struct InstantiateGONetParticipantEvent : IPersistentEvent
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// this is the information necessary to lookup the source <see cref="UnityEngine.GameObject"/> from which to use as the template in order to call <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// TODO add the persisted int->string lookup table that is updated each time a new design time location is encountered (at design time...duh)..so this can be an int!
        /// </summary>
        [Key(0)]
        public string DesignTimeLocation;

        [Key(1)]
        public uint GONetId;

        [Key(2)]
        public ushort OwnerAuthorityId;

        [Key(3)]
        public Vector3 Position;

        [Key(4)]
        public Quaternion Rotation;

        [Key(5)]
        public string InstanceName;

        [Key(6)]
        public string ParentFullUniquePath;

        [Key(7)]
        public uint GONetIdAtInstantiation;

        [Key(8)]
        public bool ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority;

        internal static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = gonetParticipant.designTimeLocation;
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
            @event.DesignTimeLocation = gonetParticipant.designTimeLocation;
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
    [MessagePackObject]
    public struct DestroyGONetParticipantEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(0)]
        public uint GONetId;

        static readonly Type[] otherEventsTypeCancelledOut = new[] {
            typeof(InstantiateGONetParticipantEvent),
            typeof(ValueMonitoringSupport_NewBaselineEvent),
            typeof(ValueMonitoringSupport_BaselineExpiredEvent)
        };

        [IgnoreMember]
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

    [MessagePack.Union(0, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Single))]
    [MessagePack.Union(1, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2))]
    [MessagePack.Union(2, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3))]
    [MessagePack.Union(3, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4))]
    [MessagePack.Union(4, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion))]
    [MessagePack.Union(5, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Boolean))]
    [MessagePack.Union(6, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Byte))]
    [MessagePack.Union(7, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_SByte))]
    [MessagePack.Union(8, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int16))]
    [MessagePack.Union(9, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt16))]
    [MessagePack.Union(10, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int32))]
    [MessagePack.Union(11, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt32))]
    [MessagePack.Union(12, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int64))]
    [MessagePack.Union(13, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt64))]
    [MessagePack.Union(14, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Double))]
    [MessagePackObject]
    public abstract class ValueMonitoringSupport_NewBaselineEvent : IPersistentEvent
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(0)]
        public uint GONetId { get; set; }

        [Key(1)]
        public byte ValueIndex { get; set; }
    }

    #region ValueMonitoringSupport_NewBaselineEvent child classes for each supported type
    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Single : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Single NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public UnityEngine.Vector2 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public UnityEngine.Vector3 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public UnityEngine.Vector4 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public UnityEngine.Quaternion NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Boolean : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Boolean NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Byte : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Byte NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_SByte : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.SByte NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Int16 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Int16 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_UInt16 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.UInt16 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Int32 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Int32 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_UInt32 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.UInt32 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Int64 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.Int64 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_UInt64 : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
        public System.UInt64 NewBaselineValue { get; set; }
    }

    [MessagePackObject]
    public class ValueMonitoringSupport_NewBaselineEvent_System_Double : ValueMonitoringSupport_NewBaselineEvent
    {
        [Key(2)]
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
    [MessagePackObject]
    public struct ValueMonitoringSupport_BaselineExpiredEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(0)]
        public uint GONetId { get; set; }

        [Key(1)]
        public byte ValueIndex { get; set; }

        static readonly Type[] otherEventTypesCancelledOut = new[] { typeof(ValueMonitoringSupport_NewBaselineEvent) };

        [IgnoreMember]
        public Type[] OtherEventTypesCancelledOut => otherEventTypesCancelledOut;

        public bool DoesCancelOutOtherEvent(IGONetEvent otherEvent)
        {
            ValueMonitoringSupport_NewBaselineEvent newBaselineEvent = (ValueMonitoringSupport_NewBaselineEvent)otherEvent;
            return newBaselineEvent.GONetId != GONetParticipant.GONetId_Unset && newBaselineEvent.GONetId == GONetId
                && newBaselineEvent.ValueIndex == ValueIndex
                ; // TODO && depending on if the evaluation order is important or not may need to check if this is the one immediately after the new baseline but only if OccurredAtElapsedTicks value is populated and reliable to reference for this
        }
    }

    [MessagePackObject]
    public struct PersistentEvents_Bundle : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public LinkedList<IPersistentEvent> PersistentEvents;

        public PersistentEvents_Bundle(long occurredAtElapsedTicks, LinkedList<IPersistentEvent> persistentEvents) : this()
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            PersistentEvents = persistentEvents;
        }
    }

    [MessagePackObject]
    public struct ClientTypeFlagsChangedEvent : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public ushort ClientAuthorityId { get; set; }

        [Key(2)]
        public ClientTypeFlags FlagsPrevious { get; set; }

        [Key(3)]
        public ClientTypeFlags FlagsNow { get; set; }

        public ClientTypeFlagsChangedEvent(long occurredAtElapsedTicks, ushort myAuthorityId, ClientTypeFlags flagsPrevious, ClientTypeFlags flagsNow)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            ClientAuthorityId = myAuthorityId;
            FlagsPrevious = flagsPrevious;
            FlagsNow = flagsNow;
        }
    }

    /// <summary>
    /// IMPORTANT: This event is initiated (and first published) from a client once the state changes locally on that client, which is slightly different than <see cref="RemoteClientStateChangedEvent"/>
    /// </summary>
    [MessagePackObject]
    public struct ClientStateChangedEvent : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// NOTE: When processing this event on the server, this value can be used to lookup the corresponding <see cref="GONetRemoteClient"/> instance by 
        ///       calling <see cref="GONetServer.TryGetClientByConnectionUID(ulong, out GONetRemoteClient)"/>.
        /// </summary>
        [Key(1)]
        public ulong InitiatingClientConnectionUID { get; set; }

        [Key(2)]
        public ClientState StatePrevious { get; set; }

        [Key(3)]
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
    [MessagePackObject]
    public struct RemoteClientStateChangedEvent : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// NOTE: When processing this event on the server, this value can be used to lookup the corresponding <see cref="GONetRemoteClient"/> instance by 
        ///       calling <see cref="GONetServer.TryGetClientByConnectionUID(ulong, out GONetRemoteClient)"/>.
        /// </summary>
        [Key(1)]
        public ulong InitiatingClientConnectionUID { get; set; }

        /// <summary>
        /// Since this event initiates server side and the server will not have as many possible states for a client, the only values this might be are:
        /// <see cref="ClientState.Connected"/> and <see cref="ClientState.Disconnected"/> TODO: see about getting all other values working as well!
        /// </summary>
        [Key(2)]
        public ClientState StatePrevious { get; set; }

        [Key(3)]
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
    /// </summary>
    public interface ISelfReturnEvent
    {
        void Return();
    }

    public interface IHaveRelatedGONetId
    {
        uint GONetId { get; set; }
    }

    /// <summary>
    /// This represents that a sync value change has been processed locally.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MessagePackObject]
    public abstract partial class SyncEvent_ValueChangeProcessed : ITransientEvent, ILocalOnlyPublish, ISelfReturnEvent
    {
        [Key(0)] public double OccurredAtElapsedSeconds { get => TimeSpan.FromTicks(OccurredAtElapsedTicks).TotalSeconds; set { OccurredAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember] public long OccurredAtElapsedTicks { get; set; }

        [Key(1)] public double ProcessedAtElapsedSeconds { get => TimeSpan.FromTicks(ProcessedAtElapsedTicks).TotalSeconds; set { ProcessedAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember] public long ProcessedAtElapsedTicks;

        [Key(2)] public ushort RelatedOwnerAuthorityId;
        [Key(3)] public uint GONetId;

        [IgnoreMember] public byte CodeGenerationId;

        [Key(4)] public byte SyncMemberIndex;
        [Key(5)] public SyncEvent_ValueChangeProcessedExplanation Explanation;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// </summary>
        public SyncEvent_ValueChangeProcessed() { }

        public abstract void Return();
    }

    [MessagePackObject]
    public class SyncEvent_PersistenceBundle
    {
        [Key(0)] public Queue<SyncEvent_ValueChangeProcessed> bundle;

        public static readonly SyncEvent_PersistenceBundle Instance = new SyncEvent_PersistenceBundle();
    }

    /// <summary>
    /// This represents that a sync value change has been processed.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MessagePackObject]
    public sealed class SyncEvent_Time_ElapsedTicks_SetFromAuthority : SyncEvent_ValueChangeProcessed
    {
        [Key(6)]        public double ElapsedSeconds_Previous { get => TimeSpan.FromTicks(ElapsedTicks_Previous).TotalSeconds; set { ElapsedTicks_Previous = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember]  public long ElapsedTicks_Previous { get; private set; }

        [Key(7)]        public double ElapsedSeconds_New { get => TimeSpan.FromTicks(ElapsedTicks_New).TotalSeconds; set { ElapsedTicks_New = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember]  public long ElapsedTicks_New { get; private set; }

        [Key(8)]        public double RoundTripSeconds_Latest { get; set; }
        [Key(9)]        public double RoundTripSeconds_RecentAverage { get; set; }
        [Key(10)]       public float RoundTripMilliseconds_LowLevelTransportProtocol { get; set; }

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
}
