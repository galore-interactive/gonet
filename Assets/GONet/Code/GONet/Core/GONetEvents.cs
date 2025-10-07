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
    /// ⚠️  CRITICAL ARCHITECTURAL CONSTRAINT: NO OBJECT POOLING ALLOWED
    ///
    /// Classes implementing IPersistentEvent MUST NOT implement ISelfReturnEvent or use object pooling.
    ///
    /// WHY NO POOLING:
    /// GONet stores persistent events BY REFERENCE in persistentEventsThisSession (GONet.cs:681) for
    /// the entire session duration (minutes to hours). When late-joining clients connect, these exact
    /// stored references are serialized and transmitted (see Server_SendClientPersistentEventsSinceStart
    /// in GONet.cs:4355).
    ///
    /// WHAT HAPPENS IF YOU ADD POOLING (DON'T!):
    ///   1. Event created: new PersistentEvent { Data = "ImportantState" }
    ///   2. Stored by reference in persistentEventsThisSession
    ///   3. Event.Return() called → data cleared: { Data = null }
    ///   4. Pool reuses object for different event → overwrites: { Data = "DifferentState" }
    ///   5. Late-joiner connects 30 minutes later
    ///   6. Server serializes persistentEventsThisSession (includes corrupted reference!)
    ///   7. Late-joiner receives WRONG/CORRUPTED data
    ///   8. RESULT: Invisible bugs, state desync, crashes, game-breaking issues
    ///
    /// MEMORY COST vs SAFETY:
    /// - Cost: ~48 bytes per event × 10-200 events = 1-10 KB per session
    /// - Benefit: 100% guarantee of data integrity for late-joining clients
    /// - Trade-off: Trivial memory overhead for critical correctness
    ///
    /// USAGE FREQUENCY:
    /// - Persistent events: 1-10 per minute (setup, config, state changes)
    /// - Transient events: 100-1000+ per second (movement, combat, frequent updates)
    /// - Memory allocation cost for persistent events is negligible compared to transient event pooling savings
    ///
    /// EXAMPLES OF CORRECT IMPLEMENTATION (no ISelfReturnEvent):
    /// - PersistentRpcEvent (see GONetRpcs.cs:912 for extensive rationale)
    /// - PersistentRoutedRpcEvent (TargetRpc variant)
    /// - InstantiateGONetParticipantEvent (spawn events)
    /// - DespawnGONetParticipantEvent (despawn with cancellation logic)
    /// - SceneLoadEvent (networked scene management)
    ///
    /// FOR END USERS:
    /// When creating custom persistent events, simply create with 'new' operator (never pool).
    /// The slight memory cost ensures your game state remains correct for all players.
    ///
    /// FOR FRAMEWORK DEVELOPERS:
    /// This design constraint is architecturally required by GONet's persistence mechanism.
    /// Do NOT attempt to "optimize" by adding pooling - the memory savings (~10 KB) are
    /// trivial compared to the catastrophic risk of data corruption. This pattern has been
    /// validated through production use and is fundamental to GONet's architecture.
    ///
    /// See also:
    /// - GONet.cs:681 - persistentEventsThisSession storage (events stored by reference)
    /// - GONet.cs:1595 - OnPersistentEvent_KeepTrack() (where events are added to storage)
    /// - GONet.cs:4355 - Server_SendClientPersistentEventsSinceStart() (transmission to late-joiners)
    /// - GONetRpcs.cs:912 - PersistentRpcEvent class (detailed pooling rationale with examples)
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
    /// Example: <see cref="InstantiateGONetParticipantEvent"/> is cancelled out by <see cref="DespawnGONetParticipantEvent"/>.
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

    [MemoryPackable]
    public partial class ClientRemotelyControlledGONetIdServerBatchRequestEvent : ITransientEvent
    {
        [MemoryPackIgnore]
        public bool IsSingularRecipientOnly => true;

        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
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

        /// <summary>
        /// Identifies which scene this GONetParticipant was spawned in.
        /// <para>This is used for scene-based persistent event filtering to ensure late-joining clients
        /// only receive spawns relevant to their currently loaded scenes.</para>
        /// <para>Value is either the scene name from build settings or the addressable path for addressable scenes.</para>
        /// </summary>
        public string SceneIdentifier;

        internal static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;

            // CRITICAL: Force metadata lookup to bypass caching check
            // This ensures we get the actual DesignTimeLocation even if metadata caching hasn't completed yet
            // Without force=true, early spawns (before caching completes) would get empty DesignTimeLocation
            @event.DesignTimeLocation = GONetSpawnSupport_Runtime.GetDesignTimeMetadata_Location(gonetParticipant, force: true);

            @event.ParentFullUniquePath = gonetParticipant.transform.parent == null ? string.Empty : HierarchyUtils.GetFullUniquePath(gonetParticipant.transform.parent.gameObject);

            @event.GONetId = gonetParticipant.GONetId;
            @event.GONetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;
            @event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority = false;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            // CRITICAL: Objects with GONetSessionContext (GONetGlobal, GONetLocal) persist via DontDestroyOnLoad
            // They must ALWAYS use "DontDestroyOnLoad" as SceneIdentifier, even if currently in a regular scene
            // Otherwise SceneUnloadEvent will incorrectly cancel their spawn events when original scene unloads
            @event.SceneIdentifier = gonetParticipant.GetComponent<GONetSessionContext>() != null
                ? HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE
                : GONetSceneManager.GetSceneIdentifier(gonetParticipant.gameObject);

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

            // CRITICAL: Objects with GONetSessionContext (GONetGlobal, GONetLocal) persist via DontDestroyOnLoad
            // They must ALWAYS use "DontDestroyOnLoad" as SceneIdentifier, even if currently in a regular scene
            // Otherwise SceneUnloadEvent will incorrectly cancel their spawn events when original scene unloads
            @event.SceneIdentifier = gonetParticipant.GetComponent<GONetSessionContext>() != null
                ? HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE
                : GONetSceneManager.GetSceneIdentifier(gonetParticipant.gameObject);

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }

        internal static InstantiateGONetParticipantEvent Create_WithRemotelyControlledByInfo(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;

            // CRITICAL: Force metadata lookup to bypass caching check
            // This ensures we get the actual DesignTimeLocation even if metadata caching hasn't completed yet
            @event.DesignTimeLocation = GONetSpawnSupport_Runtime.GetDesignTimeMetadata_Location(gonetParticipant, force: true);

            @event.ParentFullUniquePath = gonetParticipant.transform.parent == null ? string.Empty : HierarchyUtils.GetFullUniquePath(gonetParticipant.transform.parent.gameObject);

            @event.GONetId = gonetParticipant.GONetId;
            @event.GONetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;
            @event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority = true;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            // CRITICAL: Objects with GONetSessionContext (GONetGlobal, GONetLocal) persist via DontDestroyOnLoad
            // They must ALWAYS use "DontDestroyOnLoad" as SceneIdentifier, even if currently in a regular scene
            // Otherwise SceneUnloadEvent will incorrectly cancel their spawn events when original scene unloads
            @event.SceneIdentifier = gonetParticipant.GetComponent<GONetSessionContext>() != null
                ? HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE
                : GONetSceneManager.GetSceneIdentifier(gonetParticipant.gameObject);

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }
    }

    /// <summary>
    /// Commands all machines to despawn a <see cref="GONetParticipant"/> and its <see cref="GameObject"/>.
    /// <para>This event represents an **intentional gameplay despawn** (not scene lifecycle destruction).</para>
    ///
    /// <para><b>Networking Behavior:</b></para>
    /// <list type="bullet">
    /// <item><b>Network Propagation:</b> YES - Sent to all remote connections</item>
    /// <item><b>Persistent Event:</b> YES - Added to persistent event history for late-joining clients</item>
    /// <item><b>Cancels Spawn:</b> YES - Cancels corresponding <see cref="InstantiateGONetParticipantEvent"/> in persistent history</item>
    /// </list>
    ///
    /// <para><b>When This Event is Published:</b></para>
    /// <list type="bullet">
    /// <item>Player/AI destroys an object through gameplay logic</item>
    /// <item>Projectile hits target and is removed</item>
    /// <item>Pickup item is collected and removed</item>
    /// <item>Any intentional, non-scene-related object removal</item>
    /// </list>
    ///
    /// <para><b>When This Event is NOT Published:</b></para>
    /// <list type="bullet">
    /// <item>Scene is unloading (objects destroyed as part of scene lifecycle)</item>
    /// <item>Application is quitting</item>
    /// <item>Object is in a DontDestroyOnLoad scene during scene transition</item>
    /// </list>
    ///
    /// <para><b>Usage Example:</b></para>
    /// <code>
    /// // Subscribe to gameplay despawns only (not scene unloads)
    /// GONetMain.EventBus.Subscribe&lt;DespawnGONetParticipantEvent&gt;(evt => {
    ///     GONetLog.Info($"Object despawned through gameplay: {evt.GONetId}");
    ///     // Handle gameplay-specific cleanup, scoring, etc.
    /// });
    /// </code>
    ///
    /// <para>See GONet scene management documentation for complete scene lifecycle details.</para>
    /// </summary>
    [MemoryPackable]
    public partial class DespawnGONetParticipantEvent : IPersistentEvent, ICancelOutOtherEvents
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// The GONetId of the GONetParticipant being despawned.
        /// </summary>
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

    /// <summary>
    /// Represents a single chunk of a large PersistentEvents_Bundle that has been split for transmission.
    /// Used when persistent events exceed safe message size limits (> 12 KB).
    /// The client reassembles all chunks before deserializing the complete bundle.
    /// NOTE: Implements ITransientEvent since chunks are transport-layer constructs (not business logic)
    /// that should only be sent to the specific recipient and not relayed/persisted.
    /// </summary>
    [MemoryPackable]
    public partial class PersistentEvents_BundleChunk : ITransientEvent
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// Unique identifier for this multi-chunk message. All chunks with the same ChunkId belong together.
        /// </summary>
        public uint ChunkId { get; set; }

        /// <summary>
        /// Zero-based index of this chunk (0, 1, 2, ..., TotalChunks-1)
        /// </summary>
        public ushort ChunkIndex { get; set; }

        /// <summary>
        /// Total number of chunks in the complete message
        /// </summary>
        public ushort TotalChunks { get; set; }

        /// <summary>
        /// Raw serialized data for this chunk (max ~12.2 KB of data per chunk, resulting in ~12 KB total after wrapper overhead)
        /// </summary>
        public byte[] ChunkData { get; set; }

        /// <summary>
        /// Total size of the original uncompressed bundle (for validation and diagnostics)
        /// </summary>
        public int OriginalBundleSize { get; set; }

        public PersistentEvents_BundleChunk() { }

        [MemoryPackConstructor]
        public PersistentEvents_BundleChunk(uint chunkId, ushort chunkIndex, ushort totalChunks, byte[] chunkData, int originalBundleSize)
        {
            ChunkId = chunkId;
            ChunkIndex = chunkIndex;
            TotalChunks = totalChunks;
            ChunkData = chunkData;
            OriginalBundleSize = originalBundleSize;
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
    ///
    /// <para><b>Cancellation Behavior (LoadSceneMode.Single only):</b></para>
    /// <para>When loading a scene with LoadSceneMode.Single, this event cancels ALL previous SceneLoadEvent instances
    /// from the persistent event history. This prevents late-joining clients from experiencing sequential scene loads
    /// that already-connected clients never saw.</para>
    ///
    /// <para><b>Example Problem Without Cancellation:</b></para>
    /// <list type="bullet">
    /// <item>Server loads Scene A (Single mode) → SceneLoadEvent #1 persists</item>
    /// <item>Server loads Scene B (Single mode) → SceneLoadEvent #2 persists</item>
    /// <item>Server loads Scene C (Single mode) → SceneLoadEvent #3 persists</item>
    /// <item>Late-joiner connects → Receives all 3 events → Loads A, then B, then C (confusion!)</item>
    /// </list>
    ///
    /// <para><b>Solution With Cancellation:</b></para>
    /// <list type="bullet">
    /// <item>Server loads Scene A (Single mode) → SceneLoadEvent #1 persists</item>
    /// <item>Server loads Scene B (Single mode) → SceneLoadEvent #2 persists, cancels #1</item>
    /// <item>Server loads Scene C (Single mode) → SceneLoadEvent #3 persists, cancels #2</item>
    /// <item>Late-joiner connects → Receives only event #3 → Loads C directly (correct!)</item>
    /// </list>
    ///
    /// <para><b>Additive Mode Behavior:</b></para>
    /// <para>LoadSceneMode.Additive events do NOT cancel previous loads, as additive scenes
    /// are meant to stack on top of existing scenes.</para>
    /// </summary>
    [MemoryPackable]
    public partial class SceneLoadEvent : IPersistentEvent, ICancelOutOtherEvents
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

        static readonly Type[] otherEventsTypeCancelledOut = new[] {
            typeof(SceneLoadEvent)
        };

        [MemoryPackIgnore]
        public Type[] OtherEventTypesCancelledOut => otherEventsTypeCancelledOut;

        public bool DoesCancelOutOtherEvent(IGONetEvent otherEvent)
        {
            // Only LoadSceneMode.Single cancels previous scene loads
            // Additive scenes should stack, not replace
            if (Mode != UnityEngine.SceneManagement.LoadSceneMode.Single)
            {
                return false;
            }

            if (otherEvent is SceneLoadEvent previousLoadEvent)
            {
                // Cancel ALL previous SceneLoadEvent instances when loading in Single mode
                // This ensures late-joiners only see the most recent scene state
                return true;
            }

            return false;
        }
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
            typeof(SceneLoadEvent),
            typeof(InstantiateGONetParticipantEvent),  // CRITICAL: Also cancel spawns from unloaded scenes
            typeof(ValueMonitoringSupport_NewBaselineEvent),  // CRITICAL: Also cancel value events for destroyed objects
            typeof(ValueMonitoringSupport_BaselineExpiredEvent)  // CRITICAL: Also cancel expired baseline events
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
            else if (otherEvent is InstantiateGONetParticipantEvent spawnEvent)
            {
                // CRITICAL FIX: When scene unloads, remove ALL spawn events for objects that were in that scene
                // WITHOUT removing DontDestroyOnLoad objects (they persist across scene changes!)
                // Without this, late-joiners receive spawn events for non-existent objects from unloaded scenes

                // CRITICAL: Never cancel spawns in DontDestroyOnLoad scene - these objects persist across ALL scene changes
                // Examples: GONet_GlobalContext, GONet_LocalContext, player objects with AutoDontDestroyOnLoad=true
                if (spawnEvent.SceneIdentifier == HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE)
                {
                    return false; // DontDestroyOnLoad objects are NEVER cancelled by scene unloads
                }

                // Cancel if spawn's scene matches the unloaded scene
                if (!string.IsNullOrEmpty(SceneName) && SceneName == spawnEvent.SceneIdentifier)
                    return true;

                // Fallback: check by build index for build settings scenes
                // Note: SceneIdentifier may be addressable path, so this only works for build settings scenes
                if (SceneBuildIndex >= 0 && LoadType == SceneLoadType.BuildSettings)
                {
                    // Try to parse build index from SceneIdentifier if it's a build settings scene
                    // SceneIdentifier format for build settings: scene name (or may match exactly)
                    if (spawnEvent.SceneIdentifier == SceneName)
                        return true;
                }
            }
            else if (otherEvent is ValueMonitoringSupport_NewBaselineEvent baselineEvent)
            {
                // CRITICAL FIX: When scene unloads, also cancel value baseline events for objects in that scene
                // Value events reference GONetIds - if the spawn for that GONetId is in the unloaded scene, cancel the value event
                // This prevents "Unable to find GONetParticipant" errors for late-joiners

                // We need to check if the GONetId belongs to an object in the unloaded scene
                // Since we don't have direct scene info in baseline events, we rely on the spawn cancellation happening first
                // The persistent event system will remove both spawn AND value events for the same GONetId
                // For now, we can't directly cancel value events by scene - they get cancelled when the spawn is cancelled
                // This is handled by the persistent event cancellation mechanism in GONet.cs OnPersistentEvent_KeepTrack
                return false;  // Let the spawn cancellation handle it indirectly
            }
            else if (otherEvent is ValueMonitoringSupport_BaselineExpiredEvent expiredEvent)
            {
                // Same logic as NewBaselineEvent - rely on spawn cancellation
                return false;  // Let the spawn cancellation handle it indirectly
            }

            return false;
        }
    }

    /// <summary>
    /// Transient event published by CLIENT when a scene finishes loading.
    /// Server uses this to know when to send scene-defined object GONetId assignments.
    /// This ensures late-joining clients have fully loaded the scene before receiving GONetIds.
    /// </summary>
    [MemoryPackable]
    public partial class SceneLoadCompleteEvent : ITransientEvent
    {
        [MemoryPackIgnore]
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();

        /// <summary>
        /// Name of the scene that finished loading
        /// </summary>
        public string SceneName;

        /// <summary>
        /// Load mode that was used (Single or Additive)
        /// </summary>
        public UnityEngine.SceneManagement.LoadSceneMode Mode;
    }

    #endregion
}

