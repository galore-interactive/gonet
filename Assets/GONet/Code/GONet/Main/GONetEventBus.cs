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
        public override ushort TargetClientAuthorityId { get; internal set; }
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

        internal static GONetEventEnvelope<T> Borrow(T eventTyped, ushort sourceAuthorityId, GONetParticipant gonetParticipant)
        {
            var envelope = pool.Borrow();

            envelope.Event = eventTyped;
            envelope.SourceAuthorityId = sourceAuthorityId;
            envelope.GONetParticipant = gonetParticipant;
            envelope.IsReliable = true;
            envelope.TargetClientAuthorityId = GONetMain.OwnerAuthorityId_Unset;

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

        private readonly Dictionary<Type, Dictionary<string, Func<object, ushort[], int>>> multiTargetBufferAccessorsByType = new();
        private readonly Dictionary<Type, Dictionary<string, Func<object, ushort, ushort[], int, int>>> spanValidatorsByType = new();
        private readonly Dictionary<Type, Dictionary<string, RpcMetadata>> rpcMetadata = new Dictionary<Type, Dictionary<string, RpcMetadata>>();
        /// <summary>
        /// TODO FIXME: consolidate this with <see cref="rpcMetadata"/>!
        /// </summary>
        private Dictionary<Type, Dictionary<string, RpcMetadata>> rpcMetadataByType => rpcMetadata;
        private readonly ArrayPool<ushort> targetAuthorityArrayPool = new ArrayPool<ushort>(10, 5, 16, 128);
        private const int MAX_RPC_TARGETS = 64;

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

        private static bool IsValidConnectedClient(ushort authorityId)
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

        public void RegisterRpcIdMapping(uint rpcId, string methodName)
        {
            methodNameByRpcId[rpcId] = methodName;
        }

        private string GetMethodNameFromRpcId(uint rpcId)
        {
            return methodNameByRpcId.TryGetValue(rpcId, out var name) ? name : null;
        }

        internal void InitializeRpcSystem()
        {
            Subscribe<RpcEvent>(HandleIncomingRpc);
            Subscribe<RpcResponseEvent>(HandleRpcResponse);
            Subscribe<RoutedRpcEvent>(HandleRoutedRpcFromClient);
        }

        internal void RegisterRpcHandler(uint rpcId, Func<GONetEventEnvelope<RpcEvent>, Task> handler)
        {
            rpcHandlers[rpcId] = handler;
        }

        private async void HandleIncomingRpc(GONetEventEnvelope<RpcEvent> envelope)
        {
            var rpcEvent = envelope.Event;

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

        private void HandleRpcResponse(GONetEventEnvelope<RpcResponseEvent> envelope)
        {
            // Don't process responses we generated for others
            if (envelope.TargetClientAuthorityId != GONetMain.MyAuthorityId && envelope.TargetClientAuthorityId != GONetMain.OwnerAuthorityId_Unset)
            {
                return;
            }

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

        private void HandleRoutedRpcFromClient(GONetEventEnvelope<RoutedRpcEvent> envelope)
        {
            if (!GONetMain.IsServer) return;

            var evt = envelope.Event;
            var sourceAuthority = envelope.SourceAuthorityId;

            // Find the instance
            var gnp = GONetMain.GetGONetParticipantById(evt.GONetId);
            if (gnp == null) return;

            // Find the component with the RPC
            GONetParticipantCompanionBehaviour component = null;
            foreach (var comp in gnp.GetComponents<GONetParticipantCompanionBehaviour>())
            {
                if (rpcMetadataByType.ContainsKey(comp.GetType()))
                {
                    component = comp;
                    break;
                }
            }
            if (component == null) return;

            var methodName = GetMethodNameFromRpcId(evt.RpcId);
            if (methodName == null) return;

            if (!rpcMetadataByType.TryGetValue(component.GetType(), out var metadata) ||
                !metadata.TryGetValue(methodName, out var rpcMeta)) return;

            // Validate and route
            var targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            try
            {
                Array.Copy(evt.TargetAuthorities, targetBuffer, evt.TargetCount);
                int validCount = ValidateTargetsInPlace(component, methodName, sourceAuthority, targetBuffer, evt.TargetCount, rpcMeta);

                // Route to validated targets
                for (int i = 0; i < validCount; i++)
                {
                    if (targetBuffer[i] != sourceAuthority) // Don't send back to sender
                    {
                        var rpcEvent = RpcEvent.Borrow();
                        rpcEvent.RpcId = evt.RpcId;
                        rpcEvent.GONetId = evt.GONetId;
                        rpcEvent.Data = evt.Data;
                        rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        rpcEvent.IsSingularRecipientOnly = true;
                        
                        Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: rpcMeta.IsReliable);
                    }
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

                // Then broadcast to all clients
                var rpcId = GetRpcId(instance.GetType(), methodName);
                var rpcEvent = RpcEvent.Borrow();
                rpcEvent.RpcId = rpcId;
                rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
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

                // Then broadcast to all clients
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
                Publish(rpcEvent, shouldPublishReliably: metadata.IsReliable);
            }
            else
            {
                GONetLog.Warning($"ClientRpc {methodName} can only be called from server");
            }
        }

        // HandleTargetRpc - 0 parameters
        private void HandleTargetRpc(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    var routedRpc = RoutedRpcEvent.Borrow();
                    routedRpc.RpcId = GetRpcId(instance.GetType(), methodName);
                    routedRpc.GONetId = instance.GONetParticipant.GONetId;
                    routedRpc.TargetCount = targetCount;
                    Array.Copy(targetBuffer, routedRpc.TargetAuthorities, targetCount);
                    routedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);

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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);

                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }

                    // Send to remote targets
                    if (validCount > 0)
                    {
                        for (int i = 0; i < validCount; i++)
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
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 1 parameters
        private void HandleTargetRpc<T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData1<T1> { Arg1 = arg1 };
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
                    // Publish auto-returns the event and its data

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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);

                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }

                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData1<T1> { Arg1 = arg1 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);

                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }

                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 2 parameters
        private void HandleTargetRpc<T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 3 parameters
        private void HandleTargetRpc<T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 4 parameters
        private void HandleTargetRpc<T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 5 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 6 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 7 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
            }
        }

        // HandleTargetRpc - 8 parameters
        private void HandleTargetRpc<T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            // Get array from pool
            ushort[] targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
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
                    // Serialize for routing
                    var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
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
                    // Publish auto-returns the event and its data
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
                    int validCount = ValidateTargetsInPlace(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, metadata);
                    // Execute locally if server is a target
                    for (int i = 0; i < validCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                            // Remove server from targets by swapping with last
                            targetBuffer[i] = targetBuffer[--validCount];
                            break;
                        }
                    }
                    // Send to remote targets
                    if (validCount > 0)
                    {
                        // Serialize once
                        var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
                        int bytesUsed;
                        bool needsReturn;
                        byte[] serializedOriginal = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);
                        for (int i = 0; i < validCount; i++)
                        {
                            // Copy the data for each event since Publish will auto-return
                            byte[] serializedCopy = SerializationUtils.BorrowByteArray(bytesUsed);
                            Buffer.BlockCopy(serializedOriginal, 0, serializedCopy, 0, bytesUsed);
                            var rpcEvent = RpcEvent.Borrow();
                            rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                            rpcEvent.Data = serializedCopy;
                            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                            rpcEvent.IsSingularRecipientOnly = true;
                            Publish(rpcEvent, targetClientAuthorityId: targetBuffer[i], shouldPublishReliably: metadata.IsReliable);
                            // Publish auto-returns the event and serializedCopy
                        }
                        // Return the original
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serializedOriginal);
                        }
                    }
                }
            }
            finally
            {
                targetAuthorityArrayPool.Return(targetBuffer);
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

        // 0 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }

            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }

            return await HandleServerRpcAsync<TResult>(instance, methodName, metadata);
        }

        // 1 parameter
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) ||
                !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }

            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }

            return await HandleServerRpcAsync<TResult, T1>(instance, methodName, metadata, arg1);
        }

        // CallRpcInternalAsync - 2 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2>(instance, methodName, metadata, arg1, arg2);
        }

        // CallRpcInternalAsync - 3 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3>(instance, methodName, metadata, arg1, arg2, arg3);
        }

        // CallRpcInternalAsync - 4 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3, T4>(instance, methodName, metadata, arg1, arg2, arg3, arg4);
        }

        // CallRpcInternalAsync - 5 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5);
        }

        // CallRpcInternalAsync - 6 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        // CallRpcInternalAsync - 7 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        // CallRpcInternalAsync - 8 parameters
        internal async Task<TResult> CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (!rpcMetadata.TryGetValue(instance.GetType(), out var typeMetadata) || !typeMetadata.TryGetValue(methodName, out var metadata))
            {
                GONetLog.Warning($"No RPC metadata found for {instance.GetType().Name}.{methodName}");
                return default(TResult);
            }
            if (metadata.Type != RpcType.ServerRpc)
            {
                GONetLog.Warning($"CallRpcAsync can only be used with ServerRpc methods. {methodName} is a {metadata.Type}");
                return default(TResult);
            }
            return await HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, metadata, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        // HandleServerRpcAsync - 0 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult>(instance, methodName, metadata.IsReliable);
            }
        }

        // HandleServerRpcAsync - 1 parameter
        private async Task<TResult> HandleServerRpcAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1>(instance, methodName, metadata.IsReliable, arg1);
            }
        }

        // HandleServerRpcAsync - 2 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2>(instance, methodName, metadata.IsReliable, arg1, arg2);
            }
        }

        // HandleServerRpcAsync - 3 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3);
            }
        }

        // HandleServerRpcAsync - 4 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3, T4>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4);
            }
        }

        // HandleServerRpcAsync - 5 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3, T4, T5>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5);
            }
        }

        // HandleServerRpcAsync - 6 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6);
            }
        }

        // HandleServerRpcAsync - 7 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }

        // HandleServerRpcAsync - 8 parameters
        private async Task<TResult> HandleServerRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (GONetMain.IsServer)
            {
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
                return await SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(instance, methodName, metadata.IsReliable, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
        }

        private async Task<TResult> SendRpcAsync<TResult>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();

            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 1 parameter
        private async Task<TResult> SendRpcAsync<TResult, T1>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData1<T1> { Arg1 = arg1 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 2 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 3 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 4 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3, T4>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 5 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3, T4, T5>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 6 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 7 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
        }

        // SendRpcAsync - 8 parameters
        private async Task<TResult> SendRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(GONetParticipantCompanionBehaviour instance, string methodName, bool isReliable, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            var rpcId = GetRpcId(instance.GetType(), methodName);
            var correlationId = GUID.Generate().AsInt64();
            var tcs = new TaskCompletionSource<TResult>();
            RegisterPendingResponse(correlationId, tcs);

            var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.RpcId = rpcId;
            rpcEvent.GONetId = instance.GONetParticipant.GONetId;
            rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
            rpcEvent.CorrelationId = correlationId;
            rpcEvent.Data = serialized;
            Publish(rpcEvent, shouldPublishReliably: isReliable);

            return await tcs.Task;
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
                // GONetLog.Debug("DREETS  pre borrow...eventEnvelope.EventUntyped.type: " + eventEnvelope.EventUntyped.GetType().FullName + " T: " + typeof(T).FullName);

                GONetEventEnvelope<T> envelopeTyped = GONetEventEnvelope<T>.Borrow((T)eventEnvelope.EventUntyped, eventEnvelope.SourceAuthorityId, eventEnvelope.GONetParticipant);
                envelopeTyped.TargetClientAuthorityId = eventEnvelope.TargetClientAuthorityId;

                // GONetLog.Debug("DREETS  POST borrow..envelopeTyped.EventUntyped.type: " + envelopeTyped.EventUntyped.GetType().FullName + " T: " + typeof(T).FullName);
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
                GONetEventEnvelope<T> envelopeTyped = GONetEventEnvelope<T>.Borrow((T)eventEnvelope.EventUntyped, eventEnvelope.SourceAuthorityId, eventEnvelope.GONetParticipant);

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
