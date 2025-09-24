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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static UnityEngine.Networking.UnityWebRequest;

namespace GONet
{
    #region support classes (e.g., envelopes)

    public abstract class GONetEventEnvelope
    {
        /// <summary>
        /// The value of <see cref="GONetMain.MyAuthorityId"/> from the machine initially publishing the event in this envelope.
        /// It could be from a remote machine (i.e., <see cref="IsSourceRemote"/> will be true) or from this local machine (i.e., <see cref="IsFromMe"/> will be true).
        /// </summary>
        public virtual ushort SourceAuthorityId { get; set; }

        /// <summary>
        /// This ONLY should be used when the <see cref="SourceAuthorityId"/> is the server, in other words, when <see cref="IsSourceRemote"/> is false! This array contains the target remote client Ids that the server is going to transmit this event to.
        /// </summary>
        public virtual ushort TargetClientAuthorityId { get; internal set; }

        /// <summary>
        /// Tells GONet if this event should be transmitted Reliably or Unreliably
        /// </summary>
        public virtual bool IsReliable { get; protected set; }

        /// <summary>
        /// Indicates whether or not the event in this envelope initiated from a remote machine (as opposed to being from this/mine local machine).
        /// This will always return the opposite of <see cref="IsFromMe"/>.
        /// </summary>
        public virtual bool IsSourceRemote { get; }

        /// <summary>
        /// Indicates whether or not the event in this envelope initiated from this machine.
        /// This will always return the opposite of <see cref="IsSourceRemote"/>.
        /// </summary>
        public virtual bool IsFromMe { get; }

        internal IGONetEvent EventUntyped { get; set; }

        /// <summary>
        /// <para>Some event types have a single related <see cref="GONet.GONetParticipant"/> instance.</para>
        /// <para>In those cases and those cases only, this will not be null (e.g., child classes of <see cref="SyncEvent_ValueChangeProcessed"/>).</para>
        /// </summary>
        public GONetParticipant GONetParticipant { get; protected set; }
    }

    public sealed class GONetEventEnvelope<T> : GONetEventEnvelope where T : IGONetEvent
    {
        static readonly ObjectPool<GONetEventEnvelope<T>> pool = new ObjectPool<GONetEventEnvelope<T>>(50, 5);

        /// <summary>
        /// The value of <see cref="GONetMain.MyAuthorityId"/> from the machine initially publishing the event in this envelope.
        /// It could be from a remote machine (i.e., <see cref="IsSourceRemote"/> will be true) or from this local machine (i.e., <see cref="IsFromMe"/> will be true).
        /// </summary>
        public override ushort SourceAuthorityId { get; set; }
        private ushort _targetClientAuthorityId;
        public override ushort TargetClientAuthorityId 
        {
            get => _targetClientAuthorityId;
            internal set
            {
                var before = _targetClientAuthorityId;
                _targetClientAuthorityId = value;
                //GONetLog.Debug($"[SNAFUERY] _targetClientAuthorityId: {_targetClientAuthorityId}, before: {before}");
            }
        }
        public override bool IsReliable { get; protected set; }

        /// <summary>
        /// Indicates whether or not the event in this envelope initiated from a remote machine (as opposed to being from this/mine local machine).
        /// This will always return the opposite of <see cref="IsFromMe"/>.
        /// </summary>
        public override bool IsSourceRemote => SourceAuthorityId != GONetMain.MyAuthorityId;

        /// <summary>
        /// Indicates whether or not the event in this envelope initiated from this machine.
        /// This will always return the opposite of <see cref="IsSourceRemote"/>.
        /// </summary>
        public override bool IsFromMe => SourceAuthorityId == GONetMain.MyAuthorityId;

        public bool IsSingularRecipientOnly => eventTyped is ITransientEvent ? ((ITransientEvent)eventTyped).IsSingularRecipientOnly : false;

        T eventTyped;

        public T Event
        {
            get => eventTyped;
            set => EventUntyped = eventTyped = value;
        }

        internal static GONetEventEnvelope<T> Borrow(T eventTyped, ushort sourceAuthorityId, GONetParticipant gonetParticipant, 
            ushort targetClientAuthorityId = GONetMain.OwnerAuthorityId_Unset, bool isReliable = true)
        {
            var envelope = pool.Borrow();

            envelope.Event = eventTyped;
            envelope.SourceAuthorityId = sourceAuthorityId;
            envelope.GONetParticipant = gonetParticipant;
            envelope.IsReliable = isReliable;
            envelope.TargetClientAuthorityId = targetClientAuthorityId;

            return envelope;
        }

        internal static void Return(GONetEventEnvelope<T> borrowed)
        {
            pool.Return(borrowed);
        }

        internal void Init(T @event, ushort sourceAuthorityId, ushort targetClientAuthorityId, bool isReliable)
        {
            Event = @event;
            SourceAuthorityId = sourceAuthorityId;
            TargetClientAuthorityId = targetClientAuthorityId;
            IsReliable = isReliable;

            SyncEvent_ValueChangeProcessed syncEvent = @event as SyncEvent_ValueChangeProcessed;
            if (syncEvent == null)
            {
                IHaveRelatedGONetId iHaveRelatedGONetId = @event as IHaveRelatedGONetId;
                if (iHaveRelatedGONetId == null)
                {
                    GONetParticipant = null;
                }
                else
                {
                    AttemptSetGNP(iHaveRelatedGONetId.GONetId);
                }
            }
            else
            {
                AttemptSetGNP(syncEvent.GONetId);
            }
        }

        private void AttemptSetGNP(uint gonetId)
        {
            GONetParticipant = GONetMain.GetGONetParticipantById(gonetId);
            /* this has been deemed unuseful:
            if ((object)GONetParticipant == null)
            {
                uint gonetIdAtInstantiation = GONetMain.GetGONetIdAtInstantiation(gonetId);
                GONetParticipant = GONetMain.GetGONetParticipantById(gonetIdAtInstantiation);

                GONetLog.Debug("Did not find GNP by id: " + gonetId + " Next attempt to find GNP by id: " + gonetIdAtInstantiation + " did it succeed? " + ((object)GONetParticipant != null));
            }
            */
        }
    }

    #endregion

    /// <summary>
    /// <para>Main class in GONet that provides publish/subscribe model for events (both GONet types and user created custom types).</para>
    /// <para>The GONet event architecture supports the out-of-the-box feature of Record+Replay.</para>
    /// <para>For convenience, you can get access to the/an instance of this class (i.e., <see cref="GONetEventBus.Instance"/>) via <see cref="GONetMain.EventBus"/>.</para>
    /// </summary>
    public sealed partial class GONetEventBus
    {
        public static readonly GONetEventBus Instance = new GONetEventBus();

        #region Deferred RPC Processing - High Performance Object Pooled System

        /// <summary>
        /// Pooled structure for deferred RPC information to avoid allocations
        /// </summary>
        private sealed class DeferredRpcInfo : ISelfReturnEvent
        {
            public RpcEvent RpcEvent;
            public PersistentRpcEvent PersistentRpcEvent;
            public uint SourceAuthorityId;
            public uint TargetClientAuthorityId;
            public float DeferredAtTime;
            public int RetryCount;
            public bool IsPersistent;
            public GONetParticipant SourceGONetParticipant;

            private static readonly ConcurrentBag<DeferredRpcInfo> pool = new ConcurrentBag<DeferredRpcInfo>();
            private static readonly object poolLock = new object();

            public static DeferredRpcInfo Borrow()
            {
                if (pool.TryTake(out var item))
                {
                    return item;
                }
                return new DeferredRpcInfo();
            }

            public void Return()
            {
                // Clear references to prevent memory leaks
                RpcEvent = null;
                PersistentRpcEvent = null;
                SourceGONetParticipant = null;
                DeferredAtTime = 0;
                RetryCount = 0;
                IsPersistent = false;
                SourceAuthorityId = 0;
                TargetClientAuthorityId = 0;

                pool.Add(this);
            }
        }

        // High-performance collections for deferred RPC tracking
        private static readonly List<DeferredRpcInfo> deferredRpcs = new List<DeferredRpcInfo>(32);
        private static readonly Dictionary<uint, List<DeferredRpcInfo>> deferredRpcsByGoNetId = new Dictionary<uint, List<DeferredRpcInfo>>(16);

        // Configuration constants
        private const float RPC_DEFER_TIMEOUT = 1.0f; // 1 second timeout
        private const int MAX_RETRY_COUNT = 60; // ~60 frames at 60fps = 1 second

        // Performance tracking
        private static int totalDeferredRpcs = 0;
        private static int successfulDeferredRpcs = 0;
        private static int timedOutDeferredRpcs = 0;

        /// <summary>
        /// Defers an RPC for later processing when the target GONetParticipant is not yet available
        /// High-performance method using object pooling
        /// </summary>
        private static void DeferRpcForLater(RpcEvent rpcEvent, uint sourceAuthorityId, uint targetClientAuthorityId, GONetParticipant sourceGONetParticipant, bool isPersistent = false, PersistentRpcEvent persistentRpcEvent = null)
        {
            var deferredInfo = DeferredRpcInfo.Borrow();
            deferredInfo.RpcEvent = rpcEvent;
            deferredInfo.PersistentRpcEvent = persistentRpcEvent;
            deferredInfo.SourceAuthorityId = sourceAuthorityId;
            deferredInfo.TargetClientAuthorityId = targetClientAuthorityId;
            deferredInfo.DeferredAtTime = UnityEngine.Time.time;
            deferredInfo.RetryCount = 0;
            deferredInfo.IsPersistent = isPersistent;
            deferredInfo.SourceGONetParticipant = sourceGONetParticipant;

            uint targetGoNetId = isPersistent ? persistentRpcEvent.GONetId : rpcEvent.GONetId;

            deferredRpcs.Add(deferredInfo);

            // Index by GONet ID for faster lookup and immediate processing when participant becomes available
            if (!deferredRpcsByGoNetId.ContainsKey(targetGoNetId))
            {
                deferredRpcsByGoNetId[targetGoNetId] = new List<DeferredRpcInfo>(4); // Pre-allocate small capacity
            }
            deferredRpcsByGoNetId[targetGoNetId].Add(deferredInfo);

            totalDeferredRpcs++;
            GONetLog.Debug($"Deferred RPC {(isPersistent ? "(persistent)" : "")} 0x{(isPersistent ? persistentRpcEvent.RpcId : rpcEvent.RpcId):X8} for GONet ID {targetGoNetId} - participant not found yet (total deferred: {deferredRpcs.Count})");
        }

        /// <summary>
        /// High-performance deferred RPC processing - called from Unity main thread
        /// Processes all deferred RPCs and retries or times them out as needed
        /// </summary>
        public static void ProcessDeferredRpcs()
        {
            if (deferredRpcs.Count == 0) return;

            float currentTime = UnityEngine.Time.time;

            // Process deferred RPCs in reverse order for efficient removal
            for (int i = deferredRpcs.Count - 1; i >= 0; i--)
            {
                var deferred = deferredRpcs[i];
                uint targetGoNetId = deferred.IsPersistent ? deferred.PersistentRpcEvent.GONetId : deferred.RpcEvent.GONetId;

                // Check timeout first for early exit
                if (currentTime - deferred.DeferredAtTime > RPC_DEFER_TIMEOUT)
                {
                    GONetLog.Warning($"RPC {(deferred.IsPersistent ? "(persistent)" : "")} 0x{(deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId):X8} for GONet ID {targetGoNetId} timed out after {RPC_DEFER_TIMEOUT}s");
                    timedOutDeferredRpcs++;
                    RemoveDeferredRpc(i, deferred, targetGoNetId);
                    continue;
                }

                // Check if GONetParticipant is now available
                var gnp = GONetMain.GetGONetParticipantById(targetGoNetId);
                if (gnp != null)
                {
                    GONetLog.Debug($"GONet ID {targetGoNetId} now available - processing deferred RPC {(deferred.IsPersistent ? "(persistent)" : "")} 0x{(deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId):X8}");

                    // Process the RPC now - create new envelope and process immediately
                    successfulDeferredRpcs++;
                    if (deferred.IsPersistent)
                    {
                        var envelope = GONetEventEnvelope<PersistentRpcEvent>.Borrow(deferred.PersistentRpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                        envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;

                        // Process immediately on main thread to maintain order
                        Instance.HandlePersistentRpcForMe_Immediate(envelope);
                    }
                    else
                    {
                        var envelope = GONetEventEnvelope<RpcEvent>.Borrow(deferred.RpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                        envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;

                        // Process immediately on main thread to maintain order
                        Instance.HandleRpcForMe_Immediate(envelope);
                    }

                    RemoveDeferredRpc(i, deferred, targetGoNetId);
                }
                else
                {
                    // Increment retry count
                    deferred.RetryCount++;

                    if (deferred.RetryCount > MAX_RETRY_COUNT)
                    {
                        GONetLog.Warning($"RPC {(deferred.IsPersistent ? "(persistent)" : "")} 0x{(deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId):X8} for GONet ID {targetGoNetId} exceeded max retries ({MAX_RETRY_COUNT})");
                        timedOutDeferredRpcs++;
                        RemoveDeferredRpc(i, deferred, targetGoNetId);
                    }
                }
            }
        }

        /// <summary>
        /// High-performance removal of deferred RPC with proper cleanup and pooling
        /// </summary>
        private static void RemoveDeferredRpc(int index, DeferredRpcInfo deferred, uint targetGoNetId)
        {
            deferredRpcs.RemoveAt(index);

            // Remove from lookup dictionary
            if (deferredRpcsByGoNetId.TryGetValue(targetGoNetId, out var list))
            {
                list.Remove(deferred);
                if (list.Count == 0)
                {
                    deferredRpcsByGoNetId.Remove(targetGoNetId);
                }
            }

            // Return to object pool
            deferred.Return();
        }

        /// <summary>
        /// Called when a new GONetParticipant is registered to immediately process any deferred RPCs
        /// This provides the fastest path to processing once the participant becomes available
        /// </summary>
        public static void OnGONetParticipantRegistered(uint gonetId)
        {
            if (deferredRpcsByGoNetId.TryGetValue(gonetId, out var deferredList))
            {
                GONetLog.Debug($"GONet ID {gonetId} registered - immediately processing {deferredList.Count} deferred RPCs");

                // Process all deferred RPCs for this ID immediately
                var gnp = GONetMain.GetGONetParticipantById(gonetId);
                if (gnp != null)
                {
                    // Create copy of list since we'll be modifying the original
                    var listCopy = new List<DeferredRpcInfo>(deferredList);

                    foreach (var deferred in listCopy)
                    {
                        successfulDeferredRpcs++;
                        if (deferred.IsPersistent)
                        {
                            var envelope = GONetEventEnvelope<PersistentRpcEvent>.Borrow(deferred.PersistentRpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                            envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;
                            Instance.HandlePersistentRpcForMe_Immediate(envelope);
                        }
                        else
                        {
                            var envelope = GONetEventEnvelope<RpcEvent>.Borrow(deferred.RpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                            envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;
                            Instance.HandleRpcForMe_Immediate(envelope);
                        }
                    }

                    // Clear all deferred RPCs for this GONet ID
                    foreach (var deferred in listCopy)
                    {
                        deferredRpcs.Remove(deferred);
                        deferred.Return();
                    }
                    deferredRpcsByGoNetId.Remove(gonetId);
                }
            }
        }

        /// <summary>
        /// Debug information about deferred RPC system performance
        /// </summary>
        public static string GetDeferredRpcStats()
        {
            return $"Deferred RPCs - Total: {totalDeferredRpcs}, Successful: {successfulDeferredRpcs}, Timed Out: {timedOutDeferredRpcs}, Currently Pending: {deferredRpcs.Count}";
        }

        #endregion

        public delegate void HandleEventDelegate<T>(GONetEventEnvelope<T> eventEnvelope) where T : IGONetEvent;
        public delegate bool EventFilterDelegate<T>(GONetEventEnvelope<T> eventEnvelope) where T : IGONetEvent;

        private sealed class GONetEventHandlerMappings
        {
            private List<Type> eventTypesSpecific;
            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED = new();

            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_SpecificOnly = new();
            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_IncludingChildren = new();

            /// <summary>
            /// NOTE: Finds/Creates list of observers for specific event type and returns it.
            /// </summary>
            public List<EventHandlerAndFilterer> GetTypeHandlers_SpecificOnly(Type eventType)
            {
                List<EventHandlerAndFilterer> handlers = null;
                if (!handlersByEventType_SpecificOnly.TryGetValue(eventType, out handlers))
                {
                    handlers = new List<EventHandlerAndFilterer>();
                    handlersByEventType_SpecificOnly[eventType] = handlers;
                }

                return handlers;

                // since the filterPredicate cannot safely be compared for equality against another, don't cache the observable mapped to it and return a new observable each time // TODO for better storage/lookup efficiency's sake look into a way to compare filters
            }

            /// <summary>
            /// NOTE: Finds/Creates list of observers for specific event type and returns it.
            /// </summary>
            public List<EventHandlerAndFilterer> GetTypeHandlers_SpecificOnly<T>() where T : IGONetEvent
            {
                List<EventHandlerAndFilterer> handlers = null;
                Type eventType = typeof(T);

                if (!handlersByEventType_SpecificOnly.TryGetValue(eventType, out handlers))
                {
                    handlers = new List<EventHandlerAndFilterer>();
                    handlersByEventType_SpecificOnly[eventType] = handlers;
                }

                return handlers;

                // since the filterPredicate cannot safely be compared for equality against another, don't cache the observable mapped to it and return a new observable each time // TODO for better storage/lookup efficiency's sake look into a way to compare filters
            }

            public void Update_handlersByEventType_IncludingChildren_Deep(Type eventType)
            {
                if (eventType != null)
                {
                    if (TypeUtils.IsTypeAInstanceOfTypeB(eventType, typeof(IGONetEvent)))
                    {
                        handlersByEventType_IncludingChildren[eventType] = CreateTypeHandlers_IncludingChildren(eventType); // NOTE: yes, this replaces whatever was there previously
                        Update_handlersByEventType_IncludingChildren_Deep(eventType.BaseType);
                    }

                    Type[] interfaces = GetInterfaces(eventType);
                    int length = interfaces.Length;
                    for (int i = 0; i < length; ++i)
                    {
                        Update_handlersByEventType_IncludingChildren_Deep(interfaces[i]);
                    }

                    FullyCachePriorityOrderedEventHandlers();
                }
            }

            /// <summary>
            /// WARNING: This is a VERY CPU INTENSE/EXPENSIVE method!!!
            /// Call this only during times when using more CPU is acceptable (i.e., during initialization etc...)
            /// </summary>
            private void FullyCachePriorityOrderedEventHandlers()
            {
                foreach (var list in publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED.Values)
                {
                    list?.Clear();
                }
                publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED.Clear();

                /*
                foreach (var handlersKVP in handlersByEventType_SpecificOnly)
                {
                    if (handlersKVP.Value.Count > 0)
                    {
                        Type eventTypeSpecific = handlersKVP.Key;
                        publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED[eventTypeSpecific] = 
                            LookupSpecificTypeHandlers_FULLY_CACHED(eventTypeSpecific);
                    }
                }
                */

                eventTypesSpecific ??= TypeUtils.GetAllTypesInheritingFrom<IGONetEvent>(true);
                int count = eventTypesSpecific.Count;
                for (int i = 0; i < count; ++i)
                {
                    Type eventTypeSpecific = eventTypesSpecific[i];
                    List<EventHandlerAndFilterer> fullyCached = LookupSpecificTypeHandlers_FULLY_CACHED(eventTypeSpecific);
                    if (fullyCached != null)
                    {
                        publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED[eventTypeSpecific] = fullyCached;
                    }
                }
            }

            /// <summary>
            /// NOTE: Creates list of observers for specific event type and any children types and returns it.
            /// PRE: Ensure <see cref="handlersByEventType_SpecificOnly"/> is updated for <paramref name="eventType"/> so this can do its job.
            /// </summary>
            private List<EventHandlerAndFilterer> CreateTypeHandlers_IncludingChildren(Type eventType)
            {
                var handlers = new List<EventHandlerAndFilterer>();

                // TODO review this functionality and make a more performant version.  This usage of linq, new and various methods for this kills me!
                var matchingHandlerLists = handlersByEventType_SpecificOnly.Where(kvp => TypeUtils.IsTypeAInstanceOfTypeB(eventType, kvp.Key)).Select(kvp => kvp.Value);
                int matchCount = matchingHandlerLists != null ? matchingHandlerLists.Count() : 0;
                if (matchCount > 0)
                {
                    foreach (List<EventHandlerAndFilterer> matchingHandlerList in matchingHandlerLists)
                    {
                        handlers.AddRange(matchingHandlerList);
                    }

                    if (matchCount > 1)
                    {
                        // if there are more than one matches, then we cannot rely on the already ordered subscription priority stuffs and we have to consider priorities across these multiple lists!
                        handlers.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                    }
                }

                return handlers;
            }
            //readonly HashSet<EventHandlerAndFilterer> specificTypeHandlers_tmp = new(1000);
            //readonly List<EventHandlerAndFilterer> specificTypeHandlers_tmpList = new(1000);
            /// <summary>
            /// IMPORTANT: This method is no longer "fully cached" as advertised.  TODO FIXME: get things back to where it is fully cached and additional lists building does not occur.
            /// 
            /// TODO inline this method for performance...it is called all the time with <see cref="Publish{T}(T)"/>!
            /// if no specific observable streams exist for the type - null
            /// otherwise - list of specific observable streams for the type
            /// </summary>
            /// <returns>
            /// A priority order list of observers, where the highest priority subscriptions/observers are first!
            /// </returns>
            /// <param name="includeChildClasses">
            /// if true, any observers registered for a base class type (<typeparam name="T"/>) and the event being published is of a child class, it will be processed for that observer
            /// if false, only observers registered for the exact class type (<typeparam name="T"/>) will be returned
            /// </param>
            private List<EventHandlerAndFilterer> LookupSpecificTypeHandlers_FULLY_CACHED(Type eventType, bool includeChildClasses = true)
            {
                HashSet<EventHandlerAndFilterer> specificTypeHandlers_tmp = new(10);
                //specificTypeHandlers_tmp.Clear();

                if (includeChildClasses)
                {
                    Type eventTypeCurrent = eventType;
                    while (eventTypeCurrent != typeof(object) && eventTypeCurrent != null)
                    {
                        AddHandlersToList(eventTypeCurrent);
                        eventTypeCurrent = eventTypeCurrent.BaseType;
                    }

                    Type[] eventInterfaces = GetInterfaces(eventType);
                    int length = eventInterfaces.Length;
                    for (int i = length - 1; i >= 0; --i)
                    {
                        Type eventInterface = eventInterfaces[i];
                        while (eventInterface != null)
                        {
                            AddHandlersToList(eventInterface);
                            eventInterface = eventInterface.BaseType;
                        }
                    }
                }
                else
                {
                    AddHandlersToList(eventType);
                }

                void AddHandlersToList(Type currentType)
                {
                    if (handlersByEventType_IncludingChildren.TryGetValue(currentType, out List<EventHandlerAndFilterer> handlers))
                    {
                        int handlerCount = handlers.Count;
                        for (int iHandler = 0; iHandler < handlerCount; ++iHandler)
                        {
                            specificTypeHandlers_tmp.Add(handlers[iHandler]);
                        }
                    }
                }

                if (specificTypeHandlers_tmp.Count == 0)
                {
                    return null;
                }

                List<EventHandlerAndFilterer> specificTypeHandlers_tmpList = new(10);
                //specificTypeHandlers_tmpList.Clear();
                using (var en = specificTypeHandlers_tmp.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        specificTypeHandlers_tmpList.Add(en.Current);
                    }
                }

                GCLessAlgorithms.QuickSort(specificTypeHandlers_tmpList, EventHandlerAndFilterer.SubscriptionPriorityComparer);

                return specificTypeHandlers_tmpList;
            }

            public void ResortSubscribersByPriority()
            {
                foreach (var observersKVP in handlersByEventType_SpecificOnly)
                {
                    List<EventHandlerAndFilterer> observers = observersKVP.Value;
                    observers.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                }

                foreach (var observersKVP in handlersByEventType_IncludingChildren)
                {
                    List<EventHandlerAndFilterer> observers = observersKVP.Value;
                    observers.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                }

                FullyCachePriorityOrderedEventHandlers();
            }
        }

        private readonly GONetEventHandlerMappings nonSyncEventHandlerMappings = new();
        private readonly GONetEventHandlerMappings specificSyncEventHandlerMappings = new();
        private readonly GONetEventHandlerMappings anySyncEventHandlerMappings = new();

        private readonly Dictionary<SyncEvent_GeneratedTypes, Type> eventEnumTypeToEventTypeMap = new();

        private GONetEventBus()
        {
            for (int i = 0; i < genericEnvelopes_publishCallDepthIndex.Length; ++i)
            {
                genericEnvelopes_publishCallDepthIndex[i] = new GONetEventEnvelope<IGONetEvent>();
            }

            InitializeEventMap();
        }

        private void InitializeEventMap()
        {
            foreach (SyncEvent_GeneratedTypes eventType in Enum.GetValues(typeof(SyncEvent_GeneratedTypes)))
            {
                Type @type = Type.GetType("GONet." + eventType.ToString());
                eventEnumTypeToEventTypeMap[eventType] = @type;

                if (@type == null)
                {
                    GONetLog.Warning("Not valid type for: " + eventType.ToString());
                }
            }
        }

        #region RPC Support

        private readonly Dictionary<uint, Func<GONetEventEnvelope<RpcEvent>, Task>> rpcHandlers = new();
        private readonly ConcurrentDictionary<long, object> pendingResponses = new(); // TaskCompletionSource<T>

        private readonly Dictionary<Type, Dictionary<string, Func<object, ushort>>> targetPropertyAccessorsByType = new();

        public void RegisterTargetPropertyAccessors(Type type, Dictionary<string, Func<object, ushort>> accessors)
        {
            targetPropertyAccessorsByType[type] = accessors;
        }

        [ThreadStatic]
        private static GONetRpcContext? currentRpcContext;

        /// <summary>
        /// Gets the current RPC context. Only valid during RPC method execution.
        /// Returns null if not currently executing an RPC.
        /// </summary>
        public static GONetRpcContext? CurrentRpcContext => currentRpcContext;

        /// <summary>
        /// Gets the current RPC context, throwing if not available.
        /// Use this when you know you're in an RPC context.
        /// </summary>
        public static GONetRpcContext GetCurrentRpcContext()
        {
            if (!currentRpcContext.HasValue)
                throw new InvalidOperationException("No RPC context available. This can only be accessed during RPC method execution.");
            return currentRpcContext.Value;
        }

        internal static void SetCurrentRpcContext(GONetRpcContext? context)
        {
            currentRpcContext = context;
        }

        /// <summary>
        /// Thread-static validation context. Each thread processing RPCs has its own validation context
        /// to avoid conflicts when multiple RPCs are validated simultaneously.
        /// </summary>
        [ThreadStatic]
        private static RpcValidationContext currentValidationContext;

        /// <summary>
        /// Sets the validation context for the current thread. Used internally by validation framework.
        /// </summary>
        internal void SetValidationContext(RpcValidationContext context)
        {
            currentValidationContext = context;
        }

        /// <summary>
        /// Clears the validation context for the current thread. Used internally by validation framework.
        /// </summary>
        internal void ClearValidationContext()
        {
            currentValidationContext = default;
        }

        /// <summary>
        /// Gets the current validation context. Only valid during RPC validation.
        /// Thread-safe access to the current thread's validation context.
        /// </summary>
        internal RpcValidationContext? GetValidationContext()
        {
            return currentValidationContext.TargetAuthorityIds != null ? currentValidationContext : null;
        }

        /// <summary>
        /// Attempts to get a cached validation result for read-only scenarios.
        /// Only caches results that don't modify data and have simple authority-based validation.
        /// </summary>
        /// <param name="cacheKey">Unique key identifying this validation scenario</param>
        /// <param name="targetCount">Number of targets being validated</param>
        /// <param name="cachedResult">The cached result if found and valid</param>
        /// <returns>True if a valid cached result was found</returns>
        private bool TryGetCachedValidationResult(string cacheKey, int targetCount, out RpcValidationResult cachedResult)
        {
            cachedResult = default;

            if (validationResultCache.TryGetValue(cacheKey, out var cached) && cached.IsValid)
            {
                // Create a new result using the cached data
                cachedResult = RpcValidationResult.CreatePreAllocated(targetCount);

                // Copy cached results
                for (int i = 0; i < Math.Min(targetCount, cached.AllowedTargets.Length); i++)
                {
                    cachedResult.AllowedTargets[i] = cached.AllowedTargets[i];
                }

                cachedResult.DenialReason = cached.DenialReason;
                cachedResult.WasModified = cached.WasModified;

                return true;
            }

            // Cleanup cache periodically
            PerformCacheCleanupIfNeeded();
            return false;
        }

        /// <summary>
        /// Caches a validation result for future reuse (only for read-only, non-modifying validations)
        /// </summary>
        /// <param name="cacheKey">Unique key for this validation scenario</param>
        /// <param name="result">The validation result to cache</param>
        private void CacheValidationResult(string cacheKey, RpcValidationResult result)
        {
            // Only cache non-modifying results to avoid stale data issues
            if (result.WasModified) return;

            // Don't cache if we're at capacity
            if (validationResultCache.Count >= MAX_CACHED_RESULTS) return;

            var cached = new CachedValidationResult
            {
                AllowedTargets = new bool[result.TargetCount],
                DenialReason = result.DenialReason,
                WasModified = result.WasModified,
                CachedAtTicks = DateTime.UtcNow.Ticks
            };

            // Copy the results
            for (int i = 0; i < result.TargetCount; i++)
            {
                cached.AllowedTargets[i] = result.AllowedTargets[i];
            }

            validationResultCache.TryAdd(cacheKey, cached);
        }

        /// <summary>
        /// Removes expired entries from the validation result cache
        /// </summary>
        private void PerformCacheCleanupIfNeeded()
        {
            long currentTicks = DateTime.UtcNow.Ticks;
            if (currentTicks - lastCacheCleanup < CACHE_CLEANUP_INTERVAL_TICKS) return;

            lock (cacheCleanupLock)
            {
                // Double-check after acquiring lock
                if (currentTicks - lastCacheCleanup < CACHE_CLEANUP_INTERVAL_TICKS) return;

                var expiredKeys = new List<string>();
                foreach (var kvp in validationResultCache)
                {
                    if (!kvp.Value.IsValid)
                        expiredKeys.Add(kvp.Key);
                }

                foreach (var key in expiredKeys)
                {
                    validationResultCache.TryRemove(key, out _);
                }

                lastCacheCleanup = currentTicks;
            }
        }

        /// <summary>
        /// Invokes a validator delegate based on its parameter count.
        /// Uses caching to eliminate reflection overhead for repeated calls.
        /// </summary>
        /// <param name="validatorObj">The validator delegate object</param>
        /// <param name="paramCount">Number of parameters the RPC method has</param>
        /// <param name="instance">The component instance to validate on</param>
        /// <param name="sourceAuthority">Authority ID of the RPC sender</param>
        /// <param name="targets">Array of target authority IDs</param>
        /// <param name="targetCount">Number of valid targets in the array</param>
        /// <param name="data">Serialized RPC parameter data (null for 0-param methods)</param>
        /// <returns>Validation result indicating which targets are allowed/denied</returns>
        private RpcValidationResult InvokeValidator(object validatorObj, int paramCount, object instance, ushort sourceAuthority, ushort[] targets, int targetCount, byte[] data)
        {
            try
            {
                // Create cache key for this validator
                string cacheKey = $"{instance.GetType().Name}_{validatorObj.GetHashCode()}";

                // Try to get compiled validator from cache
                var compiledValidator = compiledValidatorCache.GetOrAdd(cacheKey, key =>
                {
                    var compiled = new CompiledValidator { ParameterCount = paramCount };

                    if (paramCount == 0)
                    {
                        compiled.Validator0Param = (Func<object, ushort, ushort[], int, RpcValidationResult>)validatorObj;
                    }
                    else if (paramCount <= 8)
                    {
                        compiled.ValidatorNParam = (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)validatorObj;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Validator with {paramCount} parameters is not supported. Maximum is 8 parameters.");
                    }

                    return compiled;
                });

                // Use cached compiled validator for fast invocation
                if (compiledValidator.ParameterCount == 0)
                {
                    return compiledValidator.Validator0Param(instance, sourceAuthority, targets, targetCount);
                }
                else
                {
                    return compiledValidator.ValidatorNParam(instance, sourceAuthority, targets, targetCount, data);
                }
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Error invoking validator for {instance.GetType().Name}: {ex}");
                // Return default allow-all result on error to maintain RPC functionality
                var result = RpcValidationResult.CreatePreAllocated(targetCount);
                result.AllowAll();
                return result;
            }
        }


        private readonly Dictionary<Type, Dictionary<string, Func<object, ushort[], int>>> multiTargetBufferAccessorsByType = new();
        private readonly Dictionary<Type, Dictionary<string, Func<object, ushort, ushort[], int, int>>> spanValidatorsByType = new();
        private readonly Dictionary<Type, Dictionary<string, RpcMetadata>> rpcMetadata = new();
        /// <summary>
        /// TODO FIXME: consolidate this with <see cref="rpcMetadata"/>!
        /// </summary>
        private Dictionary<Type, Dictionary<string, RpcMetadata>> rpcMetadataByType => rpcMetadata;
        private readonly ArrayPool<ushort> targetAuthorityArrayPool = new(10, 5, 16, 128);
        internal const int MAX_RPC_TARGETS = 64;

        private readonly Dictionary<ulong, RpcValidationResult> storedValidationReports = new();
        private readonly Queue<(ulong id, long timestamp)> validationReportQueue = new();
        private ulong nextValidationReportId = 1;
        private const int MAX_STORED_REPORTS = 1000;
        private const long REPORT_RETENTION_TICKS = 60 * 60; // 1 minute at 60 ticks/sec

        // Thread-safe concurrent collections for validation data accessed from multiple threads during RPC processing
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> enhancedValidatorsByType = new();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, int>> validatorParameterCounts = new();

        // Performance optimization: Cache compiled validators to eliminate reflection overhead
        private readonly ConcurrentDictionary<string, CompiledValidator> compiledValidatorCache = new();

        // Result caching for read-only validation scenarios (e.g., authority checks)
        private readonly ConcurrentDictionary<string, CachedValidationResult> validationResultCache = new();
        private readonly object cacheCleanupLock = new object();
        private long lastCacheCleanup = 0;
        private const long CACHE_CLEANUP_INTERVAL_TICKS = 30 * TimeSpan.TicksPerSecond; // 30 seconds in DateTime.Ticks (100ns units)
        private const int MAX_CACHED_RESULTS = 1000;

        /// <summary>
        /// Cached validator information for fast lookup without reflection
        /// </summary>
        private struct CompiledValidator
        {
            public Func<object, ushort, ushort[], int, RpcValidationResult> Validator0Param;
            public Func<object, ushort, ushort[], int, byte[], RpcValidationResult> ValidatorNParam;
            public int ParameterCount;
        }

        /// <summary>
        /// Cached validation result for repeated scenarios with identical parameters
        /// </summary>
        private struct CachedValidationResult
        {
            public bool[] AllowedTargets;
            public string DenialReason;
            public bool WasModified;
            public long CachedAtTicks;
            public bool IsValid => (DateTime.UtcNow.Ticks - CachedAtTicks) < CACHE_CLEANUP_INTERVAL_TICKS;
        }

        // For tracking pending delivery reports
        private readonly Dictionary<long, TaskCompletionSource<RpcDeliveryReport>> pendingDeliveryReports = new();
        private readonly Dictionary<uint, Type> componentTypeByRpcId = new();


        /// <summary>
        /// Registers enhanced validation delegates for a component type.
        /// Thread-safe registration of parameter-specific validators.
        /// </summary>
        /// <param name="type">Component type to register validators for</param>
        /// <param name="validators">Dictionary of method name to validator delegate</param>
        /// <param name="parameterCounts">Dictionary of method name to parameter count</param>
        public void RegisterEnhancedValidators(Type type, Dictionary<string, object> validators, Dictionary<string, int> parameterCounts)
        {
            // Convert to concurrent dictionaries for thread-safe access during RPC processing
            var concurrentValidators = new ConcurrentDictionary<string, object>(validators);
            var concurrentParamCounts = new ConcurrentDictionary<string, int>(parameterCounts);

            enhancedValidatorsByType.AddOrUpdate(type, concurrentValidators, (key, existing) => concurrentValidators);
            validatorParameterCounts.AddOrUpdate(type, concurrentParamCounts, (key, existing) => concurrentParamCounts);
        }

        // Send delivery report back to caller
        private void SendDeliveryReport(ushort targetAuthority, RpcDeliveryReport report, RpcMetadata metadata, long correlationId = 0)
        {
            var reportEvent = RpcDeliveryReportEvent.Borrow();
            reportEvent.Report = report;
            reportEvent.CorrelationId = correlationId;
            reportEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

            Publish(reportEvent, targetClientAuthorityId: targetAuthority, shouldPublishReliably: true);
        }

        // Handle incoming delivery reports
        private void Client_HandleDeliveryReport(GONetEventEnvelope<RpcDeliveryReportEvent> envelope)
        {
            RpcDeliveryReportEvent reportEvent = envelope.Event;

            // Check both places for the correlation
            if (pendingDeliveryReports.TryGetValue(reportEvent.CorrelationId, out var tcs))
            {
                tcs.TrySetResult(reportEvent.Report);
                pendingDeliveryReports.Remove(reportEvent.CorrelationId);
            }
            else if (pendingResponses.TryRemove(reportEvent.CorrelationId, out var handler) &&
                     handler is DeliveryReportHandler reportHandler)
            {
                reportHandler.HandleDeliveryReport(reportEvent);
            }
        }

        // Method to await delivery report
        internal Task<RpcDeliveryReport> WaitForDeliveryReport(long correlationId)
        {
            if (pendingDeliveryReports.TryGetValue(correlationId, out var tcs))
            {
                return tcs.Task;
            }
            return Task.FromResult(new RpcDeliveryReport { FailureReason = "No pending report" });
        }

        private ulong StoreValidationReport(RpcValidationResult result)
        {
            ulong id = nextValidationReportId++;

            // Store the report
            storedValidationReports[id] = result;
            validationReportQueue.Enqueue((id, GONetMain.Time.ElapsedTicks));

            // Clean up old reports
            while (validationReportQueue.Count > MAX_STORED_REPORTS ||
                   (validationReportQueue.Count > 0 &&
                    GONetMain.Time.ElapsedTicks - validationReportQueue.Peek().timestamp > REPORT_RETENTION_TICKS))
            {
                var old = validationReportQueue.Dequeue();
                storedValidationReports.Remove(old.id);
            }

            return id;
        }

        internal bool TryGetStoredRpcValidationReport(ulong reportId, out RpcValidationResult report)
        {
            return storedValidationReports.TryGetValue(reportId, out report);
        }

        public void RegisterMultiTargetPropertyAccessors(Type type, Dictionary<string, Func<object, ushort[], int>> accessors)
        {
            multiTargetBufferAccessorsByType[type] = accessors;
        }

        public void RegisterSingleTargetValidators(Type type, Dictionary<string, Func<object, ushort, ushort[], int, int>> validators)
        {
            spanValidatorsByType[type] = validators;
        }

        public void RegisterMultiTargetValidators(Type type, Dictionary<string, Func<object, ushort, ushort[], int, int>> validators)
        {
            if (!spanValidatorsByType.TryGetValue(type, out var existing))
            {
                spanValidatorsByType[type] = validators;
            }
            else
            {
                foreach (var kvp in validators)
                {
                    existing[kvp.Key] = kvp.Value;
                }
            }
        }

        private bool IsValidConnectedClient(ushort authorityId)
        {
            if (GONetMain.IsServer)
            {
                foreach (var client in GONetMain.gonetServer.remoteClients)
                {
                    if (client.ConnectionToClient.OwnerAuthorityId == authorityId)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #region Execute RPC locally helper methods
        // ExecuteRpcLocally - 0 parameters
        private void ExecuteRpcLocally(GONetParticipantCompanionBehaviour instance, string methodName)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch0(instance, methodName);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 1 parameters
        private void ExecuteRpcLocally<T1>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch1(instance, methodName, arg1);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 2 parameters
        private void ExecuteRpcLocally<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch2(instance, methodName, arg1, arg2);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 3 parameters
        private void ExecuteRpcLocally<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch3(instance, methodName, arg1, arg2, arg3);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 4 parameters
        private void ExecuteRpcLocally<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch4(instance, methodName, arg1, arg2, arg3, arg4);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 5 parameters
        private void ExecuteRpcLocally<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch5(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 6 parameters
        private void ExecuteRpcLocally<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch6(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 7 parameters
        private void ExecuteRpcLocally<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch7(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }

        // ExecuteRpcLocally - 8 parameters
        private void ExecuteRpcLocally<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
            {
                var context = new GONetRpcContext(GONetMain.MyAuthorityId, true, instance.GONetParticipant.GONetId);
                SetCurrentRpcContext(context);
                try
                {
                    dispatcher.Dispatch8(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                }
                finally
                {
                    SetCurrentRpcContext(null);
                }
            }
        }
        #endregion

        private readonly Dictionary<uint, string> methodNameByRpcId = new();

        public void RegisterRpcIdMapping(uint rpcId, string methodName, Type componentType)
        {
            methodNameByRpcId[rpcId] = methodName;
            componentTypeByRpcId[rpcId] = componentType;
        }

        private string GetMethodNameFromRpcId(uint rpcId)
        {
            return methodNameByRpcId.TryGetValue(rpcId, out var name) ? name : null;
        }

        internal void InitializeRpcSystem()
        {
            // Transient RPC events
            Subscribe<RpcEvent>(HandleRpcForMe, (envelope) =>
                envelope.TargetClientAuthorityId == GONetMain.MyAuthorityId ||
                envelope.TargetClientAuthorityId == GONetMain.OwnerAuthorityId_Unset
            );

            Subscribe<RpcResponseEvent>(HandleRpcResponseForMe, (envelope) =>
                envelope.TargetClientAuthorityId == GONetMain.MyAuthorityId ||
                envelope.TargetClientAuthorityId == GONetMain.OwnerAuthorityId_Unset
            );

            Subscribe<RoutedRpcEvent>(Server_HandleRoutedRpcFromClient, (e) =>
                e.IsSourceRemote &&
                e.SourceAuthorityId != GONetMain.OwnerAuthorityId_Server);

            Subscribe<RpcDeliveryReportEvent>(Client_HandleDeliveryReport, (e) =>
                e.IsSourceRemote &&
                e.SourceAuthorityId == GONetMain.OwnerAuthorityId_Server);

            // Persistent RPC events - handle exactly like transient but these persist for late-joiners
            Subscribe<PersistentRpcEvent>(HandlePersistentRpcForMe, (envelope) =>
                envelope.TargetClientAuthorityId == GONetMain.MyAuthorityId ||
                envelope.TargetClientAuthorityId == GONetMain.OwnerAuthorityId_Unset
            );

            Subscribe<PersistentRoutedRpcEvent>(Server_HandlePersistentRoutedRpcFromClient, (e) =>
                e.IsSourceRemote &&
                e.SourceAuthorityId != GONetMain.OwnerAuthorityId_Server);
        }

        internal void RegisterRpcHandler(uint rpcId, Func<GONetEventEnvelope<RpcEvent>, Task> handler)
        {
            rpcHandlers[rpcId] = handler;
        }

        private async void HandleRpcForMe(GONetEventEnvelope<RpcEvent> envelope)
        {
            var rpcEvent = envelope.Event;

            // Check if GONetParticipant exists - if not, defer processing
            var targetParticipant = GONetMain.GetGONetParticipantById(rpcEvent.GONetId);
            bool participantExists = targetParticipant != null;

            if (!participantExists)
            {
                // Defer processing until GONetParticipant becomes available
                DeferRpcForLater(rpcEvent, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: false);
                return;
            }

            if (!rpcHandlers.TryGetValue(rpcEvent.RpcId, out var handler))
            {
                GONetLog.Warning($"No handler registered for RPC ID: 0x{rpcEvent.RpcId:X8}");
                return;
            }

            // Set context for this RPC execution
            currentRpcContext = new GONetRpcContext(envelope);

            try
            {
                await handler(envelope);
                // The handler will send response if needed
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Error executing RPC handler for ID 0x{rpcEvent.RpcId:X8}: {ex.Message}");

                if (rpcEvent.CorrelationId != 0)
                {
                    var errorResponse = RpcResponseEvent.Borrow();
                    errorResponse.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    errorResponse.CorrelationId = rpcEvent.CorrelationId;
                    errorResponse.Success = false;
                    errorResponse.ErrorMessage = ex.Message;
                    
                    Publish(errorResponse, targetClientAuthorityId: envelope.SourceAuthorityId);
                }
            }
            finally
            {
                // Clear context after execution
                currentRpcContext = null;
            }
        }

        /// <summary>
        /// Immediate synchronous version of HandleRpcForMe for deferred processing
        /// Processes RPCs immediately on main thread to maintain timing and order
        /// </summary>
        private void HandleRpcForMe_Immediate(GONetEventEnvelope<RpcEvent> envelope)
        {
            HandleRpcForMe(envelope);
        }

        private void HandleRpcResponseForMe(GONetEventEnvelope<RpcResponseEvent> envelope)
        {
            var response = envelope.Event;

            // Find the pending request
            if (!pendingResponses.TryRemove(response.CorrelationId, out var tcsObj))
            {
                GONetLog.Warning($"Received RPC response for unknown correlation ID: {response.CorrelationId}");
                return;
            }

            // The tcsObj should be a TaskCompletionSource<T> where T is the actual return type
            // We'll handle deserialization in the specific typed methods
            if (tcsObj is IResponseHandler responseHandler)
            {
                responseHandler.HandleResponse(response);
            }
        }

        // Interface for handling typed responses
        internal interface IResponseHandler
        {
            void HandleResponse(RpcResponseEvent response);
        }

        // Generic response handler implementation
        internal class ResponseHandler<T> : IResponseHandler
        {
            private readonly TaskCompletionSource<T> tcs;

            public ResponseHandler(TaskCompletionSource<T> tcs)
            {
                this.tcs = tcs;
            }

            public void HandleResponse(RpcResponseEvent response)
            {
                if (response.Success)
                {
                    try
                    {
                        var result = SerializationUtils.DeserializeFromBytes<T>(response.Data);
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
                else
                {
                    tcs.TrySetException(new Exception(response.ErrorMessage ?? "RPC failed"));
                }
            }
        }

        private void Server_HandleRoutedRpcFromClient(GONetEventEnvelope<RoutedRpcEvent> envelope)
        {
            var evt = envelope.Event;
            var sourceAuthority = envelope.SourceAuthorityId;

            // Use RpcId to find the exact component type and method
            var methodName = GetMethodNameFromRpcId(evt.RpcId);
            if (methodName == null) return;

            // Find which component type this RpcId belongs to
            Type componentType = componentTypeByRpcId[evt.RpcId];
            if (componentType == null) return;

            // Find the instance
            var gnp = GONetMain.GetGONetParticipantById(evt.GONetId);
            if (gnp == null)
            {
                // Convert RoutedRpcEvent to RpcEvent for deferred processing
                var rpcEvent = RpcEvent.Borrow();
                rpcEvent.RpcId = evt.RpcId;
                rpcEvent.GONetId = evt.GONetId;
                rpcEvent.Data = evt.Data;
                rpcEvent.OccurredAtElapsedTicks = evt.OccurredAtElapsedTicks;
                rpcEvent.CorrelationId = evt.CorrelationId;
                // Note: RoutedRpcEvent doesn't have IsSingularRecipientOnly property

                // Defer processing until GONetParticipant becomes available
                DeferRpcForLater(rpcEvent, sourceAuthority, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: false);
                return;
            }

            // Get the specific component
            var component = gnp.GetComponent(componentType) as GONetParticipantCompanionBehaviour;
            if (component == null) return;

            // Now get the metadata for this specific component and method
            if (!rpcMetadataByType.TryGetValue(componentType, out var metadata) ||
                !metadata.TryGetValue(methodName, out var rpcMeta)) return;

            // Validate and route using enhanced validation system
            var targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            try
            {
                Array.Copy(evt.TargetAuthorities, targetBuffer, evt.TargetCount);

                // Use enhanced validation system for profanity filtering and other validation
                RpcValidationResult validationResult;
                if (enhancedValidatorsByType.TryGetValue(componentType, out var validators) &&
                    validators.TryGetValue(methodName, out var validatorObj) &&
                    validatorParameterCounts.TryGetValue(componentType, out var paramCounts) &&
                    paramCounts.TryGetValue(methodName, out var paramCount))
                {
                    // Invoke enhanced validator (with profanity filtering)
                    validationResult = InvokeValidator(validatorObj, paramCount, component, sourceAuthority, targetBuffer, evt.TargetCount, evt.Data);
                }
                else
                {
                    // Fallback to basic connection validation
                    validationResult = RpcValidationResult.CreatePreAllocated(evt.TargetCount);
                    for (int i = 0; i < evt.TargetCount; i++)
                    {
                        validationResult.AllowedTargets[i] = (targetBuffer[i] == GONetMain.MyAuthorityId || IsValidConnectedClient(targetBuffer[i]));
                    }
                }

                // Use modified data if validation changed the content
                byte[] dataToUse = validationResult.ModifiedData ?? evt.Data;

                // Route to validated targets
                for (int i = 0; i < validationResult.TargetCount; i++)
                {
                    if (validationResult.AllowedTargets[i])
                    {
                        var rpcEvent = RpcEvent.Borrow();
                        rpcEvent.RpcId = evt.RpcId;
                        rpcEvent.GONetId = evt.GONetId;
                        rpcEvent.Data = dataToUse;
                        rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        rpcEvent.IsSingularRecipientOnly = true;

                        Publish(
                            rpcEvent,
                            targetClientAuthorityId: targetBuffer[i],
                            shouldPublishReliably: rpcMeta.IsReliable);
                    }
                }

                // Return validation result arrays to pool
                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        /// <summary>
        /// Handles persistent RPC events - identical logic to HandleRpcForMe but for persistent events.
        /// These events are processed when they arrive from late-joining client delivery.
        /// </summary>
        private async void HandlePersistentRpcForMe(GONetEventEnvelope<PersistentRpcEvent> envelope)
        {
            var rpcEvent = envelope.Event;

            // Check if GONetParticipant exists - if not, defer processing
            var targetParticipant = GONetMain.GetGONetParticipantById(rpcEvent.GONetId);
            bool participantExists = targetParticipant != null;

            GONetLog.Debug($"This just in: persistent RPC ID: 0x{rpcEvent.RpcId:X8}, for gonetId: {rpcEvent.GONetId}, exists yet? {participantExists}");

            if (!participantExists)
            {
                // Defer processing until GONetParticipant becomes available
                DeferRpcForLater(null, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: true, persistentRpcEvent: rpcEvent);
                return;
            }

            if (!rpcHandlers.TryGetValue(rpcEvent.RpcId, out var handler))
            {
                GONetLog.Warning($"No handler registered for persistent RPC ID: 0x{rpcEvent.RpcId:X8}");
                return;
            }

            // Set context for this RPC execution - convert to standard RpcEvent envelope format
            var transientEvent = RpcEvent.Borrow();
            transientEvent.RpcId = rpcEvent.RpcId;
            transientEvent.GONetId = rpcEvent.GONetId;
            transientEvent.Data = rpcEvent.Data;
            transientEvent.OccurredAtElapsedTicks = rpcEvent.OccurredAtElapsedTicks;
            transientEvent.CorrelationId = rpcEvent.CorrelationId;
            transientEvent.IsSingularRecipientOnly = rpcEvent.IsSingularRecipientOnly;

            var transientEnvelope = GONetEventEnvelope<RpcEvent>.Borrow(transientEvent, envelope.SourceAuthorityId, envelope.GONetParticipant, rpcEvent.SourceAuthorityId);
            currentRpcContext = new GONetRpcContext(transientEnvelope);

            try
            {
                await handler(transientEnvelope);
                // The handler will send response if needed
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Error executing persistent RPC handler for ID 0x{rpcEvent.RpcId:X8}: {ex.Message}");

                // Note: Persistent RPCs typically don't have responses since they're replayed events
                // But we'll handle it just in case
                if (rpcEvent.CorrelationId != 0)
                {
                    var errorResponse = RpcResponseEvent.Borrow();
                    errorResponse.CorrelationId = rpcEvent.CorrelationId;
                    errorResponse.Success = false;
                    errorResponse.ErrorMessage = ex.Message;
                    errorResponse.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(errorResponse,
                        targetClientAuthorityId: rpcEvent.SourceAuthorityId,
                        shouldPublishReliably: true);
                }
            }
            finally
            {
                currentRpcContext = null;
                transientEvent.Return();
                GONetEventEnvelope<RpcEvent>.Return(transientEnvelope);
            }
        }

        /// <summary>
        /// Immediate synchronous version of HandlePersistentRpcForMe for deferred processing
        /// Processes persistent RPCs immediately on main thread to maintain timing and order
        /// </summary>
        private void HandlePersistentRpcForMe_Immediate(GONetEventEnvelope<PersistentRpcEvent> envelope)
        {
            HandlePersistentRpcForMe(envelope);
        }

        /// <summary>
        /// Handles persistent routed RPC events - identical logic to Server_HandleRoutedRpcFromClient but for persistent events.
        /// These events are processed when they arrive from late-joining client delivery.
        /// </summary>
        private void Server_HandlePersistentRoutedRpcFromClient(GONetEventEnvelope<PersistentRoutedRpcEvent> envelope)
        {
            var evt = envelope.Event;
            var sourceAuthority = envelope.SourceAuthorityId;

            // Use RpcId to find the exact component type and method
            var methodName = GetMethodNameFromRpcId(evt.RpcId);
            if (methodName == null) return;

            // Find which component type this RpcId belongs to
            Type componentType = componentTypeByRpcId[evt.RpcId];
            if (componentType == null) return;

            // Find the instance
            var gnp = GONetMain.GetGONetParticipantById(evt.GONetId);
            if (gnp == null)
            {
                // Convert PersistentRoutedRpcEvent to PersistentRpcEvent for deferred processing
                var persistentRpcEvent = new PersistentRpcEvent();
                persistentRpcEvent.RpcId = evt.RpcId;
                persistentRpcEvent.GONetId = evt.GONetId;
                persistentRpcEvent.Data = evt.Data;
                persistentRpcEvent.OccurredAtElapsedTicks = evt.OccurredAtElapsedTicks;
                persistentRpcEvent.CorrelationId = evt.CorrelationId;
                // Note: PersistentRoutedRpcEvent doesn't have IsSingularRecipientOnly property

                // Defer processing until GONetParticipant becomes available
                DeferRpcForLater(null, sourceAuthority, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: true, persistentRpcEvent: persistentRpcEvent);
                return;
            }

            // Get the specific component
            var component = gnp.GetComponent(componentType) as GONetParticipantCompanionBehaviour;
            if (component == null) return;

            // Now get the metadata for this specific component and method
            if (!rpcMetadataByType.TryGetValue(componentType, out var metadata) ||
                !metadata.TryGetValue(methodName, out var rpcMeta)) return;

            // For persistent events, we need to re-evaluate targeting for the current client state
            // The stored TargetAuthorities may not include late-joining clients
            var targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            try
            {
                // Copy original targets for reference
                Array.Copy(evt.TargetAuthorities, targetBuffer, evt.TargetCount);

                // NOTE: For persistent TargetRPCs, the original target list may exclude late-joining clients
                // This is a fundamental limitation that should be documented and handled at the design level
                // For now, we process with the original targets as stored

                // Use enhanced validation system for profanity filtering and other validation
                RpcValidationResult validationResult;
                if (enhancedValidatorsByType.TryGetValue(componentType, out var validators) &&
                    validators.TryGetValue(methodName, out var validatorObj) &&
                    validatorParameterCounts.TryGetValue(componentType, out var paramCounts) &&
                    paramCounts.TryGetValue(methodName, out var paramCount))
                {
                    // Invoke enhanced validator (with profanity filtering)
                    validationResult = InvokeValidator(validatorObj, paramCount, component, sourceAuthority, targetBuffer, evt.TargetCount, evt.Data);
                }
                else
                {
                    // Default validation - allow all original targets
                    validationResult = RpcValidationResult.CreatePreAllocated(evt.TargetCount);

                    for (int i = 0; i < evt.TargetCount; i++)
                    {
                        validationResult.AllowedTargets[i] = true;
                    }
                }

                // Send to validated targets using transient events (persistent replay doesn't need to persist again)
                byte[] dataToUse = validationResult.ModifiedData ?? evt.Data;

                for (int i = 0; i < evt.TargetCount; i++)
                {
                    if (i < validationResult.AllowedTargets.Length && validationResult.AllowedTargets[i])
                    {
                        var rpcEvent = RpcEvent.Borrow();
                        rpcEvent.RpcId = evt.RpcId;
                        rpcEvent.GONetId = evt.GONetId;
                        rpcEvent.Data = dataToUse;
                        rpcEvent.OccurredAtElapsedTicks = evt.OccurredAtElapsedTicks; // Use original timestamp
                        rpcEvent.IsSingularRecipientOnly = true;

                        Publish(
                            rpcEvent,
                            targetClientAuthorityId: targetBuffer[i],
                            shouldPublishReliably: rpcMeta.IsReliable);
                    }
                }

                // Return validation result arrays to pool
                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // For the generated code to register pending responses with proper typing
        internal void RegisterPendingResponse<T>(long correlationId, TaskCompletionSource<T> tcs)
        {
            pendingResponses[correlationId] = tcs;

            // Set up timeout
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30)); // 30 second timeout

                if (pendingResponses.TryRemove(correlationId, out var pendingTcs))
                {
                    (pendingTcs as TaskCompletionSource<T>)?.TrySetException(
                        new TimeoutException($"RPC request timed out after 30 seconds (correlation: {correlationId})"));
                }
            });
        }

        public void RegisterRpcMetadata(Type componentType, Dictionary<string, RpcMetadata> metadata)
        {
            rpcMetadata[componentType] = metadata;
            rpcMetadataByType[componentType] = metadata;
        }

        // HandleServerRpc - 0 parameters
        private void HandleServerRpc(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch0(instance, methodName);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc0(instance, methodName, metadata.IsReliable);
            }
        }

        // HandleServerRpc - 1 parameter
        private void HandleServerRpc<T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch1(instance, methodName, arg1);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc1(instance, methodName, metadata.IsReliable, arg1);
            }
        }

        // HandleServerRpc - 2 parameters
        private void HandleServerRpc<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch2(instance, methodName, arg1, arg2);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc2(instance, methodName, metadata.IsReliable, arg1, arg2);
            }
        }

        // HandleServerRpc - 3 parameters
        private void HandleServerRpc<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch3(instance, methodName, arg1, arg2, arg3);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc3(instance, methodName, metadata.IsReliable, arg1, arg2, arg3);
            }
        }

        // HandleServerRpc - 4 parameters
        private void HandleServerRpc<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch4(instance, methodName, arg1, arg2, arg3, arg4);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc4(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4);
            }
        }

        // HandleServerRpc - 5 parameters
        private void HandleServerRpc<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch5(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc5(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5);
            }
        }

        // HandleServerRpc - 6 parameters
        private void HandleServerRpc<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch6(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc6(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6);
            }
        }

        // HandleServerRpc - 7 parameters
        private void HandleServerRpc<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch7(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc7(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }

        // HandleServerRpc - 8 parameters
        private void HandleServerRpc<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (GONetMain.IsServer)
            {
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch8(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }
            }
            else
            {
                SendRpc8(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
        }

        // HandleClientRpc - 0 parameters
        private void HandleClientRpc(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch0(instance, methodName);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 1 parameter
        private void HandleClientRpc<T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch1(instance, methodName, arg1);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData1<T1> { Arg1 = arg1 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 2 parameters
        private void HandleClientRpc<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch2(instance, methodName, arg1, arg2);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 3 parameters
        private void HandleClientRpc<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch3(instance, methodName, arg1, arg2, arg3);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 4 parameters
        private void HandleClientRpc<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch4(instance, methodName, arg1, arg2, arg3, arg4);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 5 parameters
        private void HandleClientRpc<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch5(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 6 parameters
        private void HandleClientRpc<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch6(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 7 parameters
        private void HandleClientRpc<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch7(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleClientRpc - 8 parameters
        private void HandleClientRpc<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (GONetMain.IsServer)
            {
                // Execute locally on server
                if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                {
                    var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                    SetCurrentRpcContext(context);
                    try
                    {
                        dispatcher.Dispatch8(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }
                    finally
                    {
                        SetCurrentRpcContext(null);
                    }
                }

                // Then broadcast to all clients - use persistent or transient event based on metadata
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                int bytesUsed;
                bool needsReturn;
                byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                if (IsSuitableForPersistence(metadata))
                {
                    var persistentEvent = new PersistentRpcEvent();
                    persistentEvent.RpcId = rpcId;
                    persistentEvent.GONetId = instance.GONetParticipant.GONetId;
                    persistentEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    persistentEvent.Data = serialized;
                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    Publish(persistentEvent, shouldPublishReliably: metadata.IsReliable);
                }
                else
                {
                    var transientEvent = RpcEvent.Borrow();
                    transientEvent.RpcId = rpcId;
                    transientEvent.GONetId = instance.GONetParticipant.GONetId;
                    transientEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    transientEvent.Data = serialized;

                    Publish(transientEvent, shouldPublishReliably: metadata.IsReliable);
                }
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        private RpcValidationResult Server_DefaultValidation(ushort[] targetBuffer, int targetCount)
        {
            var result = RpcValidationResult.CreatePreAllocated(targetCount);
            result.AllowAll(); // Default allows all targets
            return result;
        }

        // HandleTargetRpc - 0 parameters
        private void HandleTargetRpc(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;

            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Enum-based targeting
                    switch (metadata.Target)
                    {
                        case RpcTarget.Owner:
                            targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                            targetCount = 1;
                            break;

                        case RpcTarget.All:
                            targetBuffer[0] = GONetMain.MyAuthorityId;
                            targetCount = 1;
                            if (GONetMain.IsServer)
                            {
                                foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                {
                                    if (targetCount < MAX_RPC_TARGETS)
                                    {
                                        targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                    }
                                }
                            }
                            break;

                        case RpcTarget.Others:
                            var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                            if (GONetMain.IsServer)
                            {
                                if (GONetMain.MyAuthorityId != ownerId)
                                {
                                    targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                }
                                foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                {
                                    ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                    if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                    {
                                        targetBuffer[targetCount++] = clientAuthorityId;
                                    }
                                }
                            }
                            break;

                        case RpcTarget.SpecificAuthority:
                        case RpcTarget.MultipleAuthorities:
                            GONetLog.Error($"TargetRpc {methodName} with {metadata.Target} requires TargetPropertyName or parameters");
                            return;
                    }
                }

                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName);
                            break;
                        }
                    }
                    return;
                }

                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Enhanced validation (no data to pass for 0-param)
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, null);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }

                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }

                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);

                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };

                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }

                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            break;
                        }
                    }

                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName);
                    }

                    // Send to allowed targets (no data for 0-param)
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 1 parameters
        private void HandleTargetRpc<T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;

            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;

                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;

                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;

                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }

                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData1<T1> { Arg1 = arg1 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1);
                            break;
                        }
                    }
                    return;
                }

                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData1<T1> { Arg1 = arg1 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }

                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }

                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);

                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };

                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }

                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;

                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }

                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1);
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }

                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 2 parameters
        private void HandleTargetRpc<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    var routedRpc = RoutedRpcEvent.Borrow();
                    routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                    routedRpc.GONetId = instance.GONetParticipant.GONetId;
                    routedRpc.TargetCount = targetCount;
                    Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                    routedRpc.Data = serialized;
                    routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 3 parameters
        private void HandleTargetRpc<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 4 parameters
        private void HandleTargetRpc<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 5 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 6 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 7 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // HandleTargetRpc - 8 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            ushort[] deniedBuffer = null;
            int targetCount = 0;
            try
            {
                // Determine targets based on metadata
                if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
                {
                    if (metadata.IsMultipleTargets)
                    {
                        // Property returns multiple targets
                        if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetCount = accessor(instance, targetBuffer);
                        }
                        else
                        {
                            GONetLog.Error($"No multi-target accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                    else
                    {
                        // Property returns single target
                        if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                            accessors.TryGetValue(methodName, out var accessor))
                        {
                            targetBuffer[0] = accessor(instance);
                            targetCount = 1;
                        }
                        else
                        {
                            GONetLog.Error($"No accessor found for {methodName} on {instance.GetType().Name}");
                            return;
                        }
                    }
                }
                else
                {
                    // Check for parameter-based targeting first
                    if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
                    {
                        targetBuffer[0] = (ushort)(object)arg1;
                        targetCount = 1;
                    }
                    else if (metadata.Target == RpcTarget.MultipleAuthorities)
                    {
                        if (typeof(T1) == typeof(List<ushort>))
                        {
                            var list = (List<ushort>)(object)arg1;
                            targetCount = Math.Min(list.Count, MAX_RPC_TARGETS);
                            for (int i = 0; i < targetCount; i++)
                            {
                                targetBuffer[i] = list[i];
                            }
                        }
                        else if (typeof(T1) == typeof(ushort[]))
                        {
                            var array = (ushort[])(object)arg1;
                            targetCount = Math.Min(array.Length, MAX_RPC_TARGETS);
                            Array.Copy(array, targetBuffer, targetCount);
                        }
                        else
                        {
                            GONetLog.Error($"TargetRpc {methodName} with MultipleAuthorities requires List<ushort> or ushort[] as first parameter");
                            return;
                        }
                    }
                    else
                    {
                        // Enum-based targeting
                        switch (metadata.Target)
                        {
                            case RpcTarget.Owner:
                                targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                                targetCount = 1;
                                break;
                            case RpcTarget.All:
                                targetBuffer[0] = GONetMain.MyAuthorityId;
                                targetCount = 1;
                                if (GONetMain.IsServer)
                                {
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        if (targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.Others:
                                var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                                if (GONetMain.IsServer)
                                {
                                    if (GONetMain.MyAuthorityId != ownerId)
                                    {
                                        targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                                    }
                                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                    {
                                        ushort clientAuthorityId = client.ConnectionToClient.OwnerAuthorityId;
                                        if (clientAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                        {
                                            targetBuffer[targetCount++] = clientAuthorityId;
                                        }
                                    }
                                }
                                break;
                            case RpcTarget.SpecificAuthority:
                                GONetLog.Error($"TargetRpc {methodName} with SpecificAuthority requires TargetPropertyName or ushort first parameter");
                                return;
                        }
                    }
                }
                // Client sends to server for routing
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    if (IsSuitableForPersistence(metadata))
                    {
                        var persistentRoutedRpc = new PersistentRoutedRpcEvent();
                        persistentRoutedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        persistentRoutedRpc.GONetId = instance.GONetParticipant.GONetId;
                        persistentRoutedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, persistentRoutedRpc.TargetAuthorities, targetCount);
                        persistentRoutedRpc.Data = serialized;
                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;
                        Publish(persistentRoutedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    else
                    {
                        var routedRpc = RoutedRpcEvent.Borrow();
                        routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                        routedRpc.GONetId = instance.GONetParticipant.GONetId;
                        routedRpc.TargetCount = targetCount;
                        Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                        routedRpc.Data = serialized;
                        routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }
                    // Execute locally if we're a target
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                            break;
                        }
                    }
                    return;
                }
                // Server validates and routes
                if (GONetMain.IsServer)
                {
                    // Serialize once for validation
                    var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    // Enhanced validation
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serializedOriginal);
                    }
                    else
                    {
                        // Default validation - just check if connected
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }
                    // Store the full report if there was validation
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }
                    // Send delivery report if requested
                    if (metadata.ExpectsDeliveryReport)
                    {
                        deniedBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
                        var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                        var report = new RpcDeliveryReport
                        {
                            DeliveredTo = allowedTargets,
                            FailedDelivery = deniedTargets,
                            FailureReason = validationResult.DenialReason,
                            WasModified = validationResult.ModifiedData != null,
                            ValidationReportId = reportId
                        };
                        // Send report back to caller
                        SendDeliveryReport(GONetMain.MyAuthorityId, report, metadata);
                    }
                    // Use modified data if provided
                    byte[] dataToSend = validationResult.ModifiedData ?? serializedOriginal;
                    // Execute locally if server is a target
                    bool serverIsTarget = false;
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            serverIsTarget = true;
                            // Don't need to remove from bool array, just skip it later
                            break;
                        }
                    }
                    if (serverIsTarget)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }
                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(dataToSend, 0, serializedCopy, 0, bytesUsed);

                            if (IsSuitableForPersistence(metadata))
                            {
                                var persistentRpcEvent = new PersistentRpcEvent();
                                persistentRpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                persistentRpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                persistentRpcEvent.Data = serializedCopy;
                                persistentRpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                persistentRpcEvent.IsSingularRecipientOnly = true;
                                persistentRpcEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                                persistentRpcEvent.OriginalTarget = metadata.Target;
                                Publish(persistentRpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                            else
                            {
                                var rpcEvent = RpcEvent.Borrow();
                                rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                                rpcEvent.Data = serializedCopy;
                                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                                rpcEvent.IsSingularRecipientOnly = true;
                                Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            }
                        }
                    }
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serializedOriginal);
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
                if (deniedBuffer != null)
                    targetAuthorityArrayPool.Return(deniedBuffer);
            }
        }

        // Validation that modifies array in-place, returns new valid count
        private int ValidateTargetsInPlace(GONetParticipantCompanionBehaviour instance, string methodName,
            ushort sourceAuthority, ushort[] targets, int targetCount, RpcMetadata metadata)
        {
            if (!string.IsNullOrEmpty(metadata.ValidationMethodName))
            {
                // Use generated validator
                if (spanValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                    validators.TryGetValue(methodName, out var validator))
                {
                    return validator(instance, sourceAuthority, targets, targetCount);
                }
            }

            // Default validation - compact array in place
            int writeIndex = 0;
            for (int i = 0; i < targetCount; i++)
            {
                if (targets[i] == GONetMain.MyAuthorityId || IsValidConnectedClient(targets[i]))
                {
                    targets[writeIndex++] = targets[i];
                }
            }
            return writeIndex;
        }

        private void SendRpc0(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        private void SendRpc1<T1>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData1<T1> { Arg1 = arg1 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc2
        private void SendRpc2<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc3
        private void SendRpc3<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc4
        private void SendRpc4<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc5
        private void SendRpc5<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc6
        private void SendRpc6<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc7
        private void SendRpc7<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // SendRpc8
        private void SendRpc8<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);
        }

        // 0 parameters
        internal void CallRpcInternal(GONetParticipantCompanionBehaviour instance, string methodName)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata);
                    break;
            }
        }

        // 1 parameter
        internal void CallRpcInternal<T1>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1);
                    break;
            }
        }

        // 2 parameters
        internal void CallRpcInternal<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2);
                    break;
            }
        }

        // 3 parameters
        internal void CallRpcInternal<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3);
                    break;
            }
        }

        // 4 parameters
        internal void CallRpcInternal<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4);
                    break;
            }
        }

        // 5 parameters
        internal void CallRpcInternal<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
                    break;
            }
        }

        // 6 parameters
        internal void CallRpcInternal<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
                    break;
            }
        }

        // 7 parameters
        internal void CallRpcInternal<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    break;
            }
        }

        // 8 parameters
        internal void CallRpcInternal<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return;
            }

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    HandleServerRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    break;
                case RpcType.ClientRpc:
                    HandleClientRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    break;
                case RpcType.TargetRpc:
                    HandleTargetRpc(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    break;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (bool flowControl, TResult value) CallRpcInternalAsync_PreValidation<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, out RpcMetadata metadata)
        {
            metadata = default;

            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return (flowControl: false, value: default(TResult));
            }

            // Allow ServerRpc and TargetRpc with delivery reports
            bool isValidAsync = metadata.Type == RpcType.ServerRpc ||
                                (metadata.Type == RpcType.TargetRpc && metadata.ExpectsDeliveryReport);

            if (!isValidAsync)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods or TargetRpc methods that return Task<RpcDeliveryReport>. {methodName} is a {metadata.Type}");
                return (flowControl: false, value: default(TResult));
            }

            return (flowControl: true, value: default);
        }

        // 0 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult>(instance, methodName, metadata);
        }

        // 1 parameter
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1>(instance, methodName, metadata, arg1);
        }

        // CallRpcInternalAsync - 2 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2>(instance, methodName, metadata, arg1, arg2);
        }

        // CallRpcInternalAsync - 3 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3>(instance, methodName, metadata, arg1, arg2, arg3);
        }

        // CallRpcInternalAsync - 4 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3, T4>(instance, methodName, metadata, arg1, arg2, arg3, arg4);
        }

        // CallRpcInternalAsync - 5 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3, T4, T5>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
        }

        // CallRpcInternalAsync - 6 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        // CallRpcInternalAsync - 7 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        // CallRpcInternalAsync - 8 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            RpcMetadata metadata;
            (bool flowControl, TResult value) = CallRpcInternalAsync_PreValidation<TResult>(instance, methodName, out metadata);
            if (!flowControl)
            {
                return value;
            }

            return await HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        // HandleRpcAsync - 0 parameters
        private async Task<TResult> HandleRpcAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync0<TResult>(instance, methodName);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult>(instance, methodName, metadata.IsReliable);
                    }

                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync0<TResult>(instance, methodName);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }

                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult>(instance, methodName, metadata);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }

                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 1 parameters
        private async Task<TResult> HandleRpcAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync1<TResult, T1>(instance, methodName, arg1);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1>(instance, methodName, metadata.IsReliable, arg1);
                    }

                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync1<TResult, T1>(instance, methodName, arg1);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }

                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1>(instance, methodName, metadata, arg1);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }

                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 2 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync2<TResult, T1, T2>(instance, methodName, arg1, arg2);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2>(instance, methodName, metadata.IsReliable, arg1, arg2);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync2<TResult, T1, T2>(instance, methodName, arg1, arg2);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2>(instance, methodName, metadata, arg1, arg2);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 3 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync3<TResult, T1, T2, T3>(instance, methodName, arg1, arg2, arg3);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync3<TResult, T1, T2, T3>(instance, methodName, arg1, arg2, arg3);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3>(instance, methodName, metadata, arg1, arg2, arg3);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 4 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync4<TResult, T1, T2, T3, T4>(instance, methodName, arg1, arg2, arg3, arg4);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync4<TResult, T1, T2, T3, T4>(instance, methodName, arg1, arg2, arg3, arg4);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4>(instance, methodName, metadata, arg1, arg2, arg3, arg4);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 5 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync5<TResult, T1, T2, T3, T4, T5>(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync5<TResult, T1, T2, T3, T4, T5>(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 6 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync6<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync6<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 7 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync7<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync7<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleRpcAsync - 8 parameters
        /// <summary>
        /// Handles asynchronous RPC execution based on the RPC type, executing locally or sending to appropriate targets.
        /// </summary>
        private async Task<TResult> HandleRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server executes ServerRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync8<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        GONetLog.Warning($"No dispatcher found for {instance.GetType().Name}.{methodName}");
                        return default(TResult);
                    }
                    else
                    {
                        // Client sends ServerRpc to server and waits for response
                        return await SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }
                case RpcType.ClientRpc:
                    if (GONetMain.IsServer)
                    {
                        // Server sends rpc to all clients
                        // This shouldn't return a value typically
                        SendRpcToDirectRemotes(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                        return default(TResult);
                    }
                    else
                    {
                        // Client executes ClientRpc locally
                        if (rpcDispatchers.TryGetValue(instance.GetType(), out var dispatcher))
                        {
                            var context = new GONetRpcContext(GONetMain.MyAuthorityId, metadata.IsReliable, instance.GONetParticipant.GONetId);
                            SetCurrentRpcContext(context);
                            try
                            {
                                return await dispatcher.DispatchAsync8<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                            }
                            finally
                            {
                                SetCurrentRpcContext(null);
                            }
                        }
                        return default(TResult);
                    }
                case RpcType.TargetRpc:
                    if (typeof(TResult) == typeof(RpcDeliveryReport))
                    {
                        // TargetRpc with delivery report goes through special handling
                        return await HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }
                    else
                    {
                        // Regular async TargetRpc (if such a thing exists)
                        GONetLog.Warning($"Async TargetRpc without delivery report not supported: {methodName}");
                        return default(TResult);
                    }
                default:
                    GONetLog.Error($"Unknown RPC type: {metadata.Type} for {methodName}");
                    return default(TResult);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 0 parameters
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }

            // Determine targets
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;

            try
            {
                // Get targets from property or metadata
                targetCount = DetermineTargets(instance, methodName, metadata, targetBuffer);

                if (targetCount == 0)
                {
                    // No targets, return empty report
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }

                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();

                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    // Client: Send to server for routing and validation
                    var tcs = new TaskCompletionSource<RpcDeliveryReport>();
                    pendingDeliveryReports[correlationId] = tcs;

                    // Create routed event
                    var routedRpc = RoutedRpcEvent.Borrow();
                    routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                    routedRpc.GONetId = instance.GONetParticipant.GONetId;
                    routedRpc.TargetCount = targetCount;
                    Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                    routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    routedRpc.CorrelationId = correlationId;

                    // Send to server
                    Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server,
                        shouldPublishReliably: metadata.IsReliable);

                    // Set timeout
                    _ = Task.Delay(5000).ContinueWith(t =>
                    {
                        if (pendingDeliveryReports.Remove(correlationId, out var pending))
                        {
                            pending.TrySetResult(new RpcDeliveryReport
                            {
                                FailureReason = "Timeout waiting for delivery report"
                            });
                        }
                    });

                    // Wait for delivery report
                    var report = await tcs.Task;
                    return (TResult)(object)report;
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report

                    // Validate targets
                    RpcValidationResult validationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, null);
                    }
                    else
                    {
                        validationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }

                    // Store validation report if significant
                    ulong reportId = 0;
                    var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || validationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(validationResult);
                    }

                    // Route to allowed targets
                    if (validationResult.TargetCount > 0)
                    {
                        for (int i = 0; i < validationResult.TargetCount; i++)
                        {
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;

                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i],
                                shouldPublishReliably: metadata.IsReliable);
                        }
                    }

                    // Create delivery report
                    var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);

                    var deliveryReport = new RpcDeliveryReport
                    {
                        DeliveredTo = allowedTargets,
                        FailedDelivery = deniedTargets,
                        FailureReason = validationResult.DenialReason,
                        WasModified = validationResult.ModifiedData != null,
                        ValidationReportId = reportId
                    };

                    // Server returns the report directly
                    return (TResult)(object)deliveryReport;
                }

                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 1 parameter
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData1<T1> { Arg1 = arg1 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData1<T1> { Arg1 = arg1 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 2 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 3 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 4 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 5 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 6 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 7 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpcWithDeliveryReportAsync - 8 parameters
        /// <summary>
        /// Handles asynchronous TargetRpc calls expecting a delivery report.
        /// </summary>
        private async Task<TResult> HandleTargetRpcWithDeliveryReportAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            // This should only be called for Task<RpcDeliveryReport>
            if (typeof(TResult) != typeof(RpcDeliveryReport))
            {
                GONetLog.Error($"HandleTargetRpcWithDeliveryReportAsync called for wrong return type: {typeof(TResult).Name}");
                return default(TResult);
            }
            // Determine targets (may need arg1 for parameter-based targeting)
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            int targetCount = 0;
            try
            {
                // Get targets - if SpecificAuthority/MultipleAuthorities, arg1 might be the target(s)
                targetCount = DetermineTargetsWithArg(instance, methodName, metadata, targetBuffer, arg1);
                if (targetCount == 0)
                {
                    return (TResult)(object)new RpcDeliveryReport
                    {
                        FailureReason = "No targets determined",
                        DeliveredTo = Array.Empty<ushort>(),
                        FailedDelivery = Array.Empty<ushort>()
                    };
                }
                // Create correlation for delivery report
                var correlationId = GUID.Generate().AsInt64();
                if (GONetMain.IsClient && !GONetMain.IsServer)
                {
                    var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                    
                    // Serialize the arguments
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                    
                    return await SendToServerWithDataDoReporting<TResult>(instance, methodName, metadata, targetBuffer, targetCount, correlationId, serialized);
                }
                else if (GONetMain.IsServer)
                {
                    // Server: Validate, route, and generate delivery report
                    // Serialize for validation
                    var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                    int bytesUsed;
                    bool needsReturn;
                    byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                    return (TResult)(object)Server_SendToValidatedTargetsWithDataDoReporting(instance, methodName, metadata, targetBuffer, targetCount, serialized, bytesUsed, needsReturn);
                }
                return default(TResult);
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        /// <summary>
        /// Reusable method to handle server-side logi
        /// NOTE: <paramref name="targetBuffer"/> MIGHT include the server itself as a target!
        /// </summary>
        /// <summary>
        /// Reusable method to handle server-side logic
        /// NOTE: <paramref name="targetBuffer"/> MIGHT include the server itself as a target!
        /// </summary>
        private RpcDeliveryReport Server_SendToValidatedTargetsWithDataDoReporting(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount,
            byte[] serialized, int bytesUsed, bool needsReturn)
        {
            // Validate targets using parameter-specific delegates
            RpcValidationResult validationResult;
            if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                validators.TryGetValue(methodName, out var validatorObj) &&
                validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                paramCounts.TryGetValue(methodName, out var paramCount))
            {
                // Invoke validator based on parameter count
                validationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, serialized);
            }
            else
            {
                // Default validation - allow all
                validationResult = RpcValidationResult.CreatePreAllocated(targetCount);
                validationResult.AllowAll();
            }

            // Convert bool array to allowed/denied lists
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            // Store validation report if significant
            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.ModifiedData != null)
            {
                // Store the full validation result for later retrieval
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                // Use the validated/modified data if available
                byte[] dataToUse = validationResult.ModifiedData ?? serialized;

                // 1. Check if server is a target and execute ONCE locally
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    // Execute locally ONCE
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = dataToUse;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    // Publish with self as target - this executes local handlers
                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);

                    // Remove server from allowed list for remote sending
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                // 2. Send directly to remote clients WITHOUT triggering local handlers
                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = dataToUse;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    // Send to all remote targets efficiently
                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return(); // Return event to pool manually
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                // Return the bool array to the pool
                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            // Create delivery report
            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.ModifiedData != null,
                ValidationReportId = reportId
            };
        }

        // Reusable method to handle client-side logic
        private async Task<TResult> SendToServerWithDataDoReporting<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, ushort[] targetBuffer, int targetCount, long correlationId, byte[] data)
        {
            // Client: Send to server for routing and validation
            var tcs = new TaskCompletionSource<RpcDeliveryReport>();
            pendingDeliveryReports[correlationId] = tcs;
            // Create routed event
            var routedRpc = RoutedRpcEvent.Borrow();
            routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
            routedRpc.GONetId = instance.GONetParticipant.GONetId;
            routedRpc.TargetCount = targetCount;
            Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
            routedRpc.Data = data;
            routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            routedRpc.CorrelationId = correlationId;

            // Send to server
            Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server,
                shouldPublishReliably: metadata.IsReliable);

            // Set timeout
            _ = Task.Delay(5000).ContinueWith(t =>
            {
                if (pendingDeliveryReports.Remove(correlationId, out var pending))
                {
                    pending.TrySetResult(new RpcDeliveryReport
                    {
                        FailureReason = "Timeout waiting for delivery report"
                    });
                }
            });
            // Wait for delivery report
            var report = await tcs.Task;
            return (TResult)(object)report;
        }

        // Helper method to determine targets (extracted from HandleTargetRpc logic)
        private int DetermineTargets(GONetParticipantCompanionBehaviour instance, string methodName,
            RpcMetadata metadata, ushort[] targetBuffer)
        {
            int targetCount = 0;

            if (!string.IsNullOrEmpty(metadata.TargetPropertyName))
            {
                if (metadata.IsMultipleTargets)
                {
                    if (multiTargetBufferAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                        accessors.TryGetValue(methodName, out var accessor))
                    {
                        targetCount = accessor(instance, targetBuffer);
                    }
                }
                else
                {
                    if (targetPropertyAccessorsByType.TryGetValue(instance.GetType(), out var accessors) &&
                        accessors.TryGetValue(methodName, out var accessor))
                    {
                        targetBuffer[0] = accessor(instance);
                        targetCount = 1;
                    }
                }
            }
            else
            {
                // Handle enum-based targeting
                switch (metadata.Target)
                {
                    case RpcTarget.Owner:
                        targetBuffer[0] = instance.GONetParticipant.OwnerAuthorityId;
                        targetCount = 1;
                        break;
                    case RpcTarget.All:
                        // Include all connected authorities
                        targetBuffer[0] = GONetMain.MyAuthorityId;
                        targetCount = 1;
                        if (GONetMain.IsServer)
                        {
                            foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                            {
                                if (targetCount < MAX_RPC_TARGETS)
                                {
                                    targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                }
                            }
                        }
                        break;
                    case RpcTarget.Others:
                        // All except owner
                        var ownerId = instance.GONetParticipant.OwnerAuthorityId;
                        if (GONetMain.IsServer)
                        {
                            if (GONetMain.MyAuthorityId != ownerId)
                            {
                                targetBuffer[targetCount++] = GONetMain.MyAuthorityId;
                            }
                            foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                            {
                                if (client.ConnectionToClient.OwnerAuthorityId != ownerId && targetCount < MAX_RPC_TARGETS)
                                {
                                    targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                }
                            }
                        }
                        break;
                }
            }

            return targetCount;
        }

        // Helper method that handles parameter-based targeting
        private int DetermineTargetsWithArg<T1>(GONetParticipantCompanionBehaviour instance, string methodName,
            RpcMetadata metadata, ushort[] targetBuffer, T1 arg1)
        {
            // Check if arg1 is the target(s) for parameter-based targeting
            if (metadata.Target == RpcTarget.SpecificAuthority && typeof(T1) == typeof(ushort))
            {
                targetBuffer[0] = (ushort)(object)arg1;
                return 1;
            }
            else if (metadata.Target == RpcTarget.MultipleAuthorities)
            {
                if (typeof(T1) == typeof(List<ushort>))
                {
                    var list = (List<ushort>)(object)arg1;
                    int count = Math.Min(list.Count, MAX_RPC_TARGETS);
                    for (int i = 0; i < count; i++)
                    {
                        targetBuffer[i] = list[i];
                    }
                    return count;
                }
                else if (typeof(T1) == typeof(ushort[]))
                {
                    var array = (ushort[])(object)arg1;
                    int count = Math.Min(array.Length, MAX_RPC_TARGETS);
                    Array.Copy(array, targetBuffer, count);
                    return count;
                }
            }

            // Fall back to regular target determination
            return DetermineTargets(instance, methodName, metadata, targetBuffer);
        }

        // SendRpcToDirectRemotesAsync - 0 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();

            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            SendRpcToDirectRemotes(instance, methodName, isReliable, correlationId);

            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 1 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, correlationId);

            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 2 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 3 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 4 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, arg4, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 5 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, arg4, arg5, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 6 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, arg4, arg5, arg6, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 7 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotesAsync - 8 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private async Task<TResult> SendRpcToDirectRemotesAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            long correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);
            // Call the void version with correlation ID
            SendRpcToDirectRemotes(instance, methodName, isReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, correlationId);
            return await tcs.Task;
        }

        // SendRpcToDirectRemotes - 0 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, long correlationId = 0)
        {
            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;

            Publish(rpcEvent, shouldPublishReliably: isReliable);
            // Note: Publish auto-returns the event and data, so no manual cleanup needed
        }

        // SendRpcToDirectRemotes - 1 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, long correlationId = 0)
        {
            // Serialize the argument
            var data = new RpcData1<T1> { Arg1 = arg1 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 2 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 3 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 4 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 5 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 6 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 7 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        // SendRpcToDirectRemotes - 8 parameters
        /// <summary>
        /// on the server, this will send to ALL clients
        /// on a client, this will send to JUST the server
        /// </summary>
        private void SendRpcToDirectRemotes<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, long correlationId = 0)
        {
            // Serialize the arguments
            var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
            SendRpcToDirectRemotesWithData(instance, methodName, isReliable, correlationId, serialized);
        }

        private void SendRpcToDirectRemotesWithData(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, long correlationId, byte[] dataSerialized)
        {
            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = dataSerialized;

            Publish(rpcEvent, shouldPublishReliably: isReliable);
            // Note: Publish auto-returns the event and data, so no manual cleanup needed
        }

        private readonly Dictionary<Type, IRpcDispatcher> rpcDispatchers = new Dictionary<Type, IRpcDispatcher>();

        public void RegisterRpcDispatcher(Type componentType, IRpcDispatcher dispatcher)
        {
            rpcDispatchers[componentType] = dispatcher;
        }
        public static unsafe uint GetRpcId(Type componentType, string methodName)
        {
            string typeName = componentType.FullName;

            // FNV-1a hash
            const uint fnvPrime = 16777619;
            uint hash = 2166136261;

            // Hash type name
            fixed (char* ptr = typeName)
            {
                char* p = ptr;
                while (*p != 0)
                {
                    hash ^= *p++;
                    hash *= fnvPrime;
                }
            }

            // Hash separator
            hash ^= '.';
            hash *= fnvPrime;

            // Hash method name
            fixed (char* ptr = methodName)
            {
                char* p = ptr;
                while (*p != 0)
                {
                    hash ^= *p++;
                    hash *= fnvPrime;
                }
            }

            return hash;
        }
        #endregion

        private const int MAX_PUBLISH_CALL_DEPTH = 256;
        /// <summary>
        /// IMPORTANT: since we only allow calls to <see cref="Publish{T}(T, uint?)"/> from one thread (i.e., <see cref="GONetMain.mainUnityThread"/>), 
        /// we are sure everything is serial calls and only one of these little temporary pass through guys is needed!  
        /// The calls to <see cref="GONetEventEnvelope{T}.Borrow(T, uint)"/> is called in the publish bit for each one individually to get the 
        /// properly typed instance that is automatically returned to its pool after being processed
        /// </summary>
        readonly GONetEventEnvelope<IGONetEvent>[] genericEnvelopes_publishCallDepthIndex = new GONetEventEnvelope<IGONetEvent>[MAX_PUBLISH_CALL_DEPTH];

        int genericEnvelope_publishCallDepth;

        /// <summary>
        /// This publishes the <paramref name="event"/> to all machines connected to the network session this is in including this
        /// machine/process (i.e., sends to self to activate subscriptions on this machine as well as all others too).
        /// 
        /// IMPORTANT: Only call this from the main Unity thread!  If you need to call from a non main Unity thread, use/call <see cref="PublishASAP{T}(T)"/> instead.
        /// </summary>
        /// <returns>0 if all went well, otherwise the number of failures/exceptions occurred during individual subscription/handler processing</returns>
        public int Publish<T>(T @event, ushort remoteSourceAuthorityId = default, ushort targetClientAuthorityId = GONetMain.OwnerAuthorityId_Unset, bool shouldPublishReliably = true) where T : IGONetEvent
        {
            Type publishedAsGenericType = typeof(T);
            Type eventTypeActual = @event.GetType();
            //GONetLog.Debug($"[DREETS] publish event type: {@event.GetType().Name} T: {typeof(T).Name}");

            int exceptionsThrown = 0;

            if (genericEnvelope_publishCallDepth >= MAX_PUBLISH_CALL_DEPTH)
            {
                throw new Exception("MAX_PUBLISH_CALL_DEPTH reached/exceeded!"); // GONetLog.Debug("genericEnvelope_publishCallDepth incremented to: " + genericEnvelope_publishCallDepth);
            }

            ++genericEnvelope_publishCallDepth;
            try
            {
                GONetMain.EnsureMainThread_IfPlaying();

                List<EventHandlerAndFilterer> handlersForType = null;
                if (@event is SyncEvent_ValueChangeProcessed)
                {
                    anySyncEventHandlerMappings.publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED
                            .TryGetValue(eventTypeActual, out handlersForType);

                    exceptionsThrown += Handle<T>(@event, remoteSourceAuthorityId, targetClientAuthorityId, shouldPublishReliably, exceptionsThrown, handlersForType);

                    //-----------------------------------------------------

                    specificSyncEventHandlerMappings.publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED
                        .TryGetValue(eventTypeActual, out handlersForType);

                    exceptionsThrown += Handle<T>(@event, remoteSourceAuthorityId, targetClientAuthorityId, shouldPublishReliably, exceptionsThrown, handlersForType);
                }

                if (!TypeUtils.IsTypeAInstanceOfTypeB(publishedAsGenericType, typeof(SyncEvent_ValueChangeProcessed)))
                {
                    nonSyncEventHandlerMappings.publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED
                        .TryGetValue(eventTypeActual, out handlersForType);

                    exceptionsThrown += Handle<T>(@event, remoteSourceAuthorityId, targetClientAuthorityId, shouldPublishReliably, exceptionsThrown, handlersForType);
                }

                if (@event is ISelfReturnEvent selfReturnEvent)
                {
                    selfReturnEvent.Return();
                }
            }
            catch (Exception e)
            {
                ++exceptionsThrown;

                const string Event = "(GONetEventBus Publish error) Error Event: ";
                const string StackTrace = "\n(GONetEventBus Publish error)  Error Stack Trace: ";
                const string Depth = "\n(GONetEventBus Publish error) genericEnvelope_publishCallDepth: ";
                GONetLog.Warning(string.Concat(Event, e.Message, StackTrace, e.StackTrace, Depth, genericEnvelope_publishCallDepth));
            }
            finally
            {
                --genericEnvelope_publishCallDepth; // GONetLog.Debug("genericEnvelope_publishCallDepth decremented to: " + genericEnvelope_publishCallDepth);
            }

            return exceptionsThrown;

            int Handle<T>(T @event, ushort remoteSourceAuthorityId, ushort targetClientAuthorityId, bool shouldPublishReliably, int exceptionsThrown, List<EventHandlerAndFilterer> handlersForType) where T : IGONetEvent
            {
                if (handlersForType != null)
                {
                    int handlerCount = handlersForType.Count;
                    ushort sourceAuthorityId = remoteSourceAuthorityId == default ? GONetMain.MyAuthorityId : remoteSourceAuthorityId;

                    GONetEventEnvelope<IGONetEvent> genericEnvelope = genericEnvelopes_publishCallDepthIndex[genericEnvelope_publishCallDepth];
                    genericEnvelope.Init(@event, sourceAuthorityId, targetClientAuthorityId, shouldPublishReliably);

                    //GONetLog.Debug($"[SNAFUERY] targetClientAuthorityId: {targetClientAuthorityId}, genericEnvelope.TargetClientAuthorityId: {genericEnvelope.TargetClientAuthorityId}");

                    for (int i = 0; i < handlerCount; ++i)
                    {
                        EventHandlerAndFilterer handlerForType = handlersForType[i];
                        if (handlerForType.Filterer == null || handlerForType.Filterer(genericEnvelope))
                        {
                            try // try-catch to disallow a single handler blowing things up for the rest of them!
                            {
                                handlerForType.Handler(genericEnvelope);
                            }
                            catch (Exception error)
                            {
                                ++exceptionsThrown;

                                const string EventType = "(GONetEventBus handler error) Event Type: ";
                                const string GenericEventType = "\n(GONetEventBus handler error) Event Published as generic Type: ";
                                const string Event = "\n(GONetEventBus handler error) Error Event: ";
                                const string StackTrace = "\n(GONetEventBus handler error)  Error Stack Trace: ";
                                const string Depth = "\n(GONetEventBus handler error) genericEnvelope_publishCallDepth: ";
                                GONetLog.Error(string.Concat(EventType, eventTypeActual.FullName, GenericEventType, publishedAsGenericType.FullName, Event, error.Message, StackTrace, error.StackTrace, Depth, genericEnvelope_publishCallDepth)); // NOTE: adding in the stack trace is important to see exactly where things went wrong...or else that info is lost
                            }
                        }
                    }
                }
                else
                {
                    //const string NO_HANDLERS = "Event received, but no handlers to process it.";
                    //GONetLog.Info(NO_HANDLERS);
                }

                return exceptionsThrown;
            }
        }

        private readonly ConcurrentQueue<IGONetEvent> publishASAPQueue = new ConcurrentQueue<IGONetEvent>();

        /// <summary>
        /// Unlike <see cref="Publish{T}(T, ushort?)"/>, you can call this on any thread and if its not <see cref="GONetMain.IsUnityMainThread"/>
        /// it will be published as soon as the main thread notices it (later this frame or next frame).
        /// </summary>
        public void PublishASAP<T>(T @event) where T : IGONetEvent
        {
            if (GONetMain.IsUnityMainThread)
            {
                Publish(@event);
            }
            else
            {
                publishASAPQueue.Enqueue(@event);
            }
        }

        internal void PublishQueuedEventsForMainThread()
        {
            GONetMain.EnsureMainThread_IfPlaying();

            int count = publishASAPQueue.Count;
            int processedCount = 0;
            IGONetEvent @event;
            while (processedCount < count && publishASAPQueue.TryDequeue(out @event))
            {
                Publish(@event);
                ++processedCount;
            }
        }

        public Subscription<SyncEvent_ValueChangeProcessed> SubscribeAnySyncEvents(HandleEventDelegate<SyncEvent_ValueChangeProcessed> handler, EventFilterDelegate<SyncEvent_ValueChangeProcessed> filter = null)
        {
            GONetMain.EnsureMainThread_IfPlaying();

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(SyncEvent_ValueChangeProcessed);

            return SubscribeInternal(eventType, anySyncEventHandlerMappings, handler, filter);
        }

        /// <summary>
        /// <para>
        /// Use this method to subscribe to events categorized as 'SyncEvent' for a specific sync event type. 
        /// These events originate exclusively from fields decorated with <see cref="GONetAutoMagicalSyncAttribute"/> when their values change.
        /// </para>
        /// <para>IMPORTANT: It is vitally important that <paramref name="handler"/> code does NOT keep a reference to the envelope or the event inside the envelope.  These items are managed by an object pool for performance reasons.  If for some reason the handler needs to do operations against data inside the envelope or event after that method call is complete (e.g., in a method later on or in a coroutine or another thread) you have to either (a) copy data off of it into other variables or (b) make a copy and if you do that it is your responsibility to return it to the proper pool afterward.  TODO FIXME: add more info as to location of proper pools!</para>
        /// <para>IMPORTANT: Only call this from the main Unity thread!</para>
        /// </summary>
        public Subscription<SyncEvent_ValueChangeProcessed> Subscribe(SyncEvent_GeneratedTypes eventEnumType, HandleEventDelegate<SyncEvent_ValueChangeProcessed> handler, EventFilterDelegate<SyncEvent_ValueChangeProcessed> filter = null)
        {
            GONetMain.EnsureMainThread_IfPlaying();

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = eventEnumTypeToEventTypeMap[eventEnumType];

            return SubscribeInternal(eventType, specificSyncEventHandlerMappings, handler, filter);
        }

        /// <summary>
        /// PRE: Must be called from main unity thread.
        /// </summary>
        private Subscription<SyncEvent_ValueChangeProcessed> SubscribeInternal(Type eventType, GONetEventHandlerMappings syncEventHandlerMappings, HandleEventDelegate<SyncEvent_ValueChangeProcessed> handler, EventFilterDelegate<SyncEvent_ValueChangeProcessed> filter = null)
        {
            List<EventHandlerAndFilterer> existingHandlersForSpecificType = syncEventHandlerMappings.GetTypeHandlers_SpecificOnly(eventType);
            EventHandlerAndFilterer newHandlerAndPredicate = new EventHandlerAndFilterer(
                new HandlerWrapper<SyncEvent_ValueChangeProcessed>(handler).Handle,
                filter == null ? (EventFilterDelegate<IGONetEvent>)null : new FilterWrapper<SyncEvent_ValueChangeProcessed>(filter).Filter
            );

            existingHandlersForSpecificType.Add(newHandlerAndPredicate);

            syncEventHandlerMappings.Update_handlersByEventType_IncludingChildren_Deep(eventType);

            syncEventHandlerMappings.ResortSubscribersByPriority();

            var subscription = new Subscription<SyncEvent_ValueChangeProcessed>(
                newHandlerAndPredicate,
                existingHandlersForSpecificType,
                () => syncEventHandlerMappings.Update_handlersByEventType_IncludingChildren_Deep(eventType));

            subscription.IsSubscriptionActive = true;
            /* WIP
            ushort sourceAuthorityId = remoteSourceAuthorityId == default ? GONetMain.MyAuthorityId : remoteSourceAuthorityId;
            disruptor.HandleEventsWith(new HandleWhich(newHandlerAndPredicate));
            */
            return subscription;
        }

        /// <summary>
        /// <para>
        /// Use this method to subscribe to events that are NOT categorized as 'SyncEvent'. If you want to subscribe to SyncEvents use the 
        /// <see cref="Subscribe(SyncEvent_GeneratedTypes, HandleEventDelegate{SyncEvent_ValueChangeProcessed}, EventFilterDelegate{SyncEvent_ValueChangeProcessed})"/> method.
        /// </para>
        /// <para>
        /// IMPORTANT: If the type T argument is of a lower type in the hierarchy than <see cref="SyncEvent_ValueChangeProcessed"/> (e.g., <see cref="IGONetEvent"/>), 
        ///            the <paramref name="handler"/> will NOT be notified of any sync events that occur if they inherit from that class.  As mentioned above, there
        ///            is a separate Subscribe method for those type of events.
        /// </para>
        /// <para>
        /// IMPORTANT: It is vitally important that <paramref name="handler"/> code does NOT keep a reference to the envelope 
        ///            or the event inside the envelope. These items are managed by an object pool for performance reasons.  
        ///            If for some reason the handler needs to do operations against data inside the envelope or event after 
        ///            that method call is complete (e.g., in a method later on or in a coroutine or another thread) you have 
        ///            to either (a) copy data off of it into other variables or (b) make a copy and if you do that it is your 
        ///            responsibility to return it to the proper pool afterward. TODO FIXME: add more info as to location of proper pools!
        /// </para>
        /// <para>IMPORTANT: Only call this from the main Unity thread!</para>
        /// </summary>
        public Subscription<T> Subscribe<T>(HandleEventDelegate<T> handler, EventFilterDelegate<T> filter = null) where T : IGONetEvent
        {
            GONetMain.EnsureMainThread_IfPlaying();

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (TypeUtils.IsTypeAInstanceOfTypeB(typeof(T), typeof(SyncEvent_ValueChangeProcessed)))
            {
                throw new ArgumentOutOfRangeException(nameof(handler), $"Cannot use this method for subscribing to sync events (i.e., that inherit from {nameof(SyncEvent_ValueChangeProcessed)}).  Use Subscribe(SyncEvent_GeneratedTypes, HandleEventDelegate<SyncEvent_ValueChangeProcessed>, EventFilterDelegate<SyncEvent_ValueChangeProcessed>) instead.");
            }

            return SubscribeInternal<T>(nonSyncEventHandlerMappings, handler, filter);
        }

        /// <summary>
        /// PRE: Must be called from main unity thread.
        /// </summary>
        private Subscription<T> SubscribeInternal<T>(GONetEventHandlerMappings eventHandlerMappings, HandleEventDelegate<T> handler, EventFilterDelegate<T> filter = null) where T : IGONetEvent
        {
            Type eventType = typeof(T);


            List<EventHandlerAndFilterer> existingHandlersForSpecificType = eventHandlerMappings.GetTypeHandlers_SpecificOnly<T>();
            EventHandlerAndFilterer newHandlerAndPredicate = new EventHandlerAndFilterer(
                new HandlerWrapper<T>(handler).Handle,
                filter == null ? (EventFilterDelegate<IGONetEvent>)null : new FilterWrapper<T>(filter).Filter
            );


            existingHandlersForSpecificType.Add(newHandlerAndPredicate);

            eventHandlerMappings.Update_handlersByEventType_IncludingChildren_Deep(eventType);

            eventHandlerMappings.ResortSubscribersByPriority();

            var subscription = new Subscription<T>(
                newHandlerAndPredicate,
                existingHandlersForSpecificType,
                () => eventHandlerMappings.Update_handlersByEventType_IncludingChildren_Deep(eventType));

            subscription.IsSubscriptionActive = true;

            return subscription;
        }

        /// <summary>
        /// IMPORTANT: Only call this from the main Unity thread!
        /// </summary>
        internal void SetSubscriptionPriority(EventHandlerAndFilterer subscriber, int priority)
        {
            GONetMain.EnsureMainThread_IfPlaying();

            subscriber.Priority = priority;

            anySyncEventHandlerMappings.ResortSubscribersByPriority();
            nonSyncEventHandlerMappings.ResortSubscribersByPriority();
            specificSyncEventHandlerMappings.ResortSubscribersByPriority();
        }

        static readonly Dictionary<Type, Type[]> interfacesByType = new Dictionary<Type, Type[]>(100);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type[] GetInterfaces(Type type)
        {
            Type[] interfaces;
            if (!interfacesByType.TryGetValue(type, out interfaces))
            {
                interfacesByType[type] = interfaces = type.GetInterfaces();
            }
            return interfaces;
        }

        #region RPC Persistence Support

        /// <summary>
        /// Determines if an RPC is suitable for persistence based on its metadata and targeting.
        /// Only certain combinations of RPC types and targets should persist for late-joining clients.
        /// </summary>
        private static bool IsSuitableForPersistence(RpcMetadata metadata)
        {
            if (!metadata.IsPersistent) return false;

            switch (metadata.Type)
            {
                case RpcType.ServerRpc:
                    // Server RPCs don't need persistence - server is always present
                    return false;

                case RpcType.ClientRpc:
                    // Client RPCs are always suitable - they go to all clients
                    return true;

                case RpcType.TargetRpc:
                    // Only certain TargetRpc configurations are suitable for persistence
                    return IsTargetRpcSuitableForPersistence(metadata);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a TargetRpc is suitable for persistence based on its targeting configuration.
        /// </summary>
        private static bool IsTargetRpcSuitableForPersistence(RpcMetadata metadata)
        {
            switch (metadata.Target)
            {
                case RpcTarget.All:
                case RpcTarget.Others:
                    // These targets are suitable - they represent categories, not individuals
                    return true;

                case RpcTarget.Owner:
                    // Owner targeting can be suitable for some use cases
                    // (e.g., setting initial state for object owners)
                    return true;

                case RpcTarget.SpecificAuthority:
                case RpcTarget.MultipleAuthorities:
                    // Individual authority targeting is not suitable for persistence
                    // because those specific clients may no longer exist
                    return false;

                default:
                    // Property-based targeting - validate the property name
                    return IsPropertyTargetSuitableForPersistence(metadata.TargetPropertyName);
            }
        }

        /// <summary>
        /// Determines if a property-based target is suitable for persistence.
        /// Static/category-based properties are suitable, individual ID properties are not.
        /// </summary>
        private static bool IsPropertyTargetSuitableForPersistence(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return false;

            // Convert to lowercase for case-insensitive comparison
            string lowerName = propertyName.ToLowerInvariant();

            // Category-based properties that are suitable for persistence
            if (lowerName.Contains("team") ||
                lowerName.Contains("group") ||
                lowerName.Contains("channel") ||
                lowerName.Contains("room") ||
                lowerName.Contains("admin") ||
                lowerName.Contains("moderator") ||
                lowerName.Contains("guild") ||
                lowerName.Contains("clan") ||
                lowerName.Contains("faction"))
            {
                return true;
            }

            // Individual-based properties that are NOT suitable for persistence
            if (lowerName.Contains("specific") ||
                lowerName.Contains("current") ||
                lowerName.Contains("target") ||
                lowerName.Contains("selected") ||
                lowerName.EndsWith("id") ||
                lowerName.EndsWith("ids"))
            {
                return false;
            }

            // Default to allowing property-based targeting for persistence
            // (conservative approach - better to persist unnecessarily than miss important state)
            return true;
        }

        #endregion

        public class EventHandlerAndFilterer
        {
            public static readonly EventHandlerAndFilterer_PriorityComparer SubscriptionPriorityComparer = new EventHandlerAndFilterer_PriorityComparer();

            internal HandleEventDelegate<IGONetEvent> Handler;

            internal EventFilterDelegate<IGONetEvent> Filterer;

            int subscriptionPriority = 0;
            public int Priority { get => subscriptionPriority; internal set => subscriptionPriority = value; }

            internal EventHandlerAndFilterer(HandleEventDelegate<IGONetEvent> handler, EventFilterDelegate<IGONetEvent> filterer)
            {
                Handler = handler;
                Filterer = filterer;
            }
        }

        public class EventHandlerAndFilterer_PriorityComparer : IComparer<EventHandlerAndFilterer>
        {
            public int Compare(EventHandlerAndFilterer a, EventHandlerAndFilterer b)
            {
                return a.Priority.CompareTo(b.Priority);
            }
        }

        /// <summary>
        /// This wrapper crap is here just due to c# generic system impl and casting issues.
        /// </summary>
        private class HandlerWrapper<T> where T : IGONetEvent
        {
            HandleEventDelegate<T> wrappedHandler;

            internal HandlerWrapper(HandleEventDelegate<T> wrappedHandler)
            {
                this.wrappedHandler = wrappedHandler ?? throw new ArgumentNullException(nameof(wrappedHandler));
            }

            public void Handle(GONetEventEnvelope eventEnvelope)
            {
                //GONetLog.Debug("DREETS  pre borrow...eventEnvelope.EventUntyped.type: " + eventEnvelope.EventUntyped.GetType().FullName + " T: " + typeof(T).FullName);

                GONetEventEnvelope<T> envelopeTyped = GONetEventEnvelope<T>.Borrow(
                    (T)eventEnvelope.EventUntyped, 
                    eventEnvelope.SourceAuthorityId, 
                    eventEnvelope.GONetParticipant,
                    targetClientAuthorityId: eventEnvelope.TargetClientAuthorityId);
                
                //GONetLog.Debug($"[SNAFUERY] eventEnvelope.TargetClientAuthorityId: {eventEnvelope.TargetClientAuthorityId}, envelopeTyped.TargetClientAuthorityId: {envelopeTyped.TargetClientAuthorityId}");
                
                //GONetLog.Debug("DREETS  POST borrow..envelopeTyped.EventUntyped.type: " + envelopeTyped.EventUntyped.GetType().FullName + " T: " + typeof(T).FullName);
                wrappedHandler(envelopeTyped);

                GONetEventEnvelope<T>.Return(envelopeTyped);
            }
        }

        /// <summary>
        /// This wrapper crap is here just due to c# generic system impl and casting issues.
        /// </summary>
        private class FilterWrapper<T> where T : IGONetEvent
        {
            EventFilterDelegate<T> wrappedFilter;

            internal FilterWrapper(EventFilterDelegate<T> wrappedFilter)
            {
                this.wrappedFilter = wrappedFilter ?? throw new ArgumentNullException(nameof(wrappedFilter));
            }

            public bool Filter(GONetEventEnvelope<IGONetEvent> eventEnvelope)
            {
                GONetEventEnvelope<T> envelopeTyped = GONetEventEnvelope<T>.Borrow(
                    (T)eventEnvelope.EventUntyped, 
                    eventEnvelope.SourceAuthorityId, 
                    eventEnvelope.GONetParticipant,
                    targetClientAuthorityId: eventEnvelope.TargetClientAuthorityId);

                bool filterResult = wrappedFilter(envelopeTyped);

                GONetEventEnvelope<T>.Return(envelopeTyped);

                return filterResult;
            }
        }
    }

    /// <summary>
    /// Represents a subscription to something in <see cref="GONetEventBus"/>.
    /// </summary>
    public class Subscription<T> : Disposer where T : IGONetEvent
    {
        GONetEventBus.EventHandlerAndFilterer subscriber;
        List<GONetEventBus.EventHandlerAndFilterer> subscribersForType;

        /// <summary>
        /// The lower the number, the higher up in the order of event processing when considering other subscribers to the same event type as in <see cref="subscriber"/>.
        /// </summary>
        public int SubscriptionPriority { get; private set; }

        public bool IsSubscriptionActive { get; internal set; }

        internal Subscription(GONetEventBus.EventHandlerAndFilterer subscriber, List<GONetEventBus.EventHandlerAndFilterer> subscribersForType, Action additionalUnsubscribeAction = null)
        {
            this.subscriber = subscriber;
            this.subscribersForType = subscribersForType;

            dispose = () => {
                this.subscribersForType.Remove(this.subscriber);
                additionalUnsubscribeAction?.Invoke();
            };

            SubscriptionPriority = 0;
        }

        /// <summary>
        /// How soon does the subscriber want to receive events as they come in (compared to all the rest of the potential subscribers to the same event).
        /// The lower the number, the sooner in the overall order of receipt.
        /// </summary>
        /// <param name="priority"></param>
        public void SetSubscriptionPriority(short priority)
        {
            GONetEventBus.Instance.SetSubscriptionPriority(subscriber, priority);
        }

        /// <summary>
        /// This GONet INTERNAL method is the same as <see cref="SetSubscriptionPriority(short)"/> with the exception that the range of values is greater, which allows GONet internal to subscribe and control internal handler priorities such that end-user priorities subscribing to the same message(s) will not cause handler processing order issues/conflicts.
        /// </summary>
        /// <param name="priority"></param>
        internal void SetSubscriptionPriority_INTERNAL(int priority)
        {
            GONetEventBus.Instance.SetSubscriptionPriority(subscriber, priority);
        }

        /// <returns>true if <see cref="IsSubscriptionActive"/> was true when called and was unsuscribed, false otherwise</returns>
        public bool Unsubscribe()
        {
            if (IsSubscriptionActive)
            {
                Dispose();
                return true;
            }
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();

            IsSubscriptionActive = false;
        }
    }

    public class Disposer : IDisposable
    {
        protected Action dispose;

        internal Disposer() { }

        internal Disposer(Action dispose)
        {
            this.dispose = dispose;
        }

        public virtual void Dispose()
        {
            dispose();
        }
    }
}
