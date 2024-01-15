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

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GONet.Utils;
using System.Collections.Concurrent;
using UnityEngine;
using ReliableNetcode;

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

        readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_SpecificOnly = new Dictionary<Type, List<EventHandlerAndFilterer>>();
        readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_IncludingChildren = new Dictionary<Type, List<EventHandlerAndFilterer>>();

        private readonly Dictionary<SyncEvent_GeneratedTypes, Type> eventEnumTypeToEventTypeMap;

        private GONetEventBus()
        {
            for (int i = 0; i < genericEnvelopes_publishCallDepthIndex.Length; ++i)
            {
                genericEnvelopes_publishCallDepthIndex[i] = new GONetEventEnvelope<IGONetEvent>();
                specificTypeHandlers_tmp_publishCallDepthIndex[i] = new HashSet<EventHandlerAndFilterer>();
                specificTypeHandlers_tmpList_publishCallDepthIndex[i] = new List<EventHandlerAndFilterer>(100);
            }

            eventEnumTypeToEventTypeMap = new Dictionary<SyncEvent_GeneratedTypes, Type>();
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

        private const int MAX_PUBLISH_CALL_DEPTH = 256;
        /// <summary>
        /// IMPORTANT: since we only allow calls to <see cref="Publish{T}(T, uint?)"/> from one thread (i.e., <see cref="GONetMain.mainUnityThread"/>), we are sure everything is serial calls and only one of these little temporary pass through guys is needed!  The calls to <see cref="GONetEventEnvelope{T}.Borrow(T, uint)"/> is called in the publish bit for each one individually to get the properly typed instance that is automatically returned to its pool after being processed
        /// </summary>
        readonly GONetEventEnvelope<IGONetEvent>[] genericEnvelopes_publishCallDepthIndex = new GONetEventEnvelope<IGONetEvent>[MAX_PUBLISH_CALL_DEPTH];
        readonly HashSet<EventHandlerAndFilterer>[] specificTypeHandlers_tmp_publishCallDepthIndex = new HashSet<EventHandlerAndFilterer>[MAX_PUBLISH_CALL_DEPTH];
        readonly List<EventHandlerAndFilterer>[] specificTypeHandlers_tmpList_publishCallDepthIndex = new List<EventHandlerAndFilterer>[MAX_PUBLISH_CALL_DEPTH];

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
            int exceptionsThrown = 0;

            ++genericEnvelope_publishCallDepth; // GONetLog.Debug("genericEnvelope_publishCallDepth incremented to: " + genericEnvelope_publishCallDepth);

            try
            {
                GONetMain.EnsureMainThread_IfPlaying();

                List<EventHandlerAndFilterer> handlersForType = LookupSpecificTypeHandlers_FULLY_CACHED(@event.GetType());
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
                                GONetLog.Error(string.Concat(EventType, @event.GetType().FullName, GenericEventType, typeof(T).FullName, Event, error.Message, StackTrace, error.StackTrace, Depth, genericEnvelope_publishCallDepth)); // NOTE: adding in the stack trace is important to see exactly where things went wrong...or else that info is lost
                            }
                        }
                    }
                }
                else
                {
                    //const string NO_HANDLERS = "Event received, but no handlers to process it.";
                    //GONetLog.Info(NO_HANDLERS);
                }

                if (@event is ISelfReturnEvent)
                {
                    ((ISelfReturnEvent)@event).Return();
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

        /// <summary>
        /// <para>Use this method to subscribe to events categorized as 'SyncEvent'. These events originate exclusively from GONetAutoMagicalSync fields when their values change</para>
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

            List<EventHandlerAndFilterer> handlersForType = GetTypeHandlers_SpecificOnly(eventType);
            var handlerAndPredicate = new EventHandlerAndFilterer(
                new HandlerWrapper<SyncEvent_ValueChangeProcessed>(handler).Handle,
                filter == null ? (EventFilterDelegate<IGONetEvent>)null : new FilterWrapper<SyncEvent_ValueChangeProcessed>(filter).Filter
            );

            return SubscribeInternal(eventType, handlersForType, handlerAndPredicate);
        }

        /// <summary>
        /// NOTE: Finds/Creates list of observers for specific event type and returns it.
        /// </summary>
        private List<EventHandlerAndFilterer> GetTypeHandlers_SpecificOnly(Type eventType)
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

        private Subscription<SyncEvent_ValueChangeProcessed> SubscribeInternal(Type eventType, List<EventHandlerAndFilterer> existingHandlersForSpecificType, EventHandlerAndFilterer newHandlerAndPredicate)
        {
            existingHandlersForSpecificType.Add(newHandlerAndPredicate);

            Update_handlersByEventType_IncludingChildren_Deep(eventType);

            ResortSubscribersByPriority();

            var subscription = new Subscription<SyncEvent_ValueChangeProcessed>(
                newHandlerAndPredicate,
                existingHandlersForSpecificType,
                () => Update_handlersByEventType_IncludingChildren_Deep(eventType));

            subscription.IsSubscriptionActive = true;

            return subscription;
        }

        /// <summary>
        /// <para>Use this method to subscribe to events that are not categorized as 'SyncEvent'. If you want to subscribe to SyncEvents use the <see cref="Subscribe(SyncEvent_GeneratedTypes, HandleEventDelegate{SyncEvent_ValueChangeProcessed}, EventFilterDelegate{SyncEvent_ValueChangeProcessed})"/> method.</para>
        /// <para>IMPORTANT: It is vitally important that <paramref name="handler"/> code does NOT keep a reference to the envelope or the event inside the envelope. These items are managed by an object pool for performance reasons.  If for some reason the handler needs to do operations against data inside the envelope or event after that method call is complete (e.g., in a method later on or in a coroutine or another thread) you have to either (a) copy data off of it into other variables or (b) make a copy and if you do that it is your responsibility to return it to the proper pool afterward. TODO FIXME: add more info as to location of proper pools!</para>
        /// <para>IMPORTANT: Only call this from the main Unity thread!</para>
        /// </summary>
        public Subscription<T> Subscribe<T>(HandleEventDelegate<T> handler, EventFilterDelegate<T> filter = null) where T : IGONetEvent
        {
            GONetMain.EnsureMainThread_IfPlaying();

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            List<EventHandlerAndFilterer> handlersForType = GetTypeHandlers_SpecificOnly<T>();
            var handlerAndPredicate = new EventHandlerAndFilterer(
                new HandlerWrapper<T>(handler).Handle,
                filter == null ? (EventFilterDelegate<IGONetEvent>)null : new FilterWrapper<T>(filter).Filter
            );

            return SubscribeInternal<T>(handlersForType, handlerAndPredicate);
        }

        /// <summary>
        /// NOTE: Finds/Creates list of observers for specific event type and returns it.
        /// </summary>
        private List<EventHandlerAndFilterer> GetTypeHandlers_SpecificOnly<T>() where T : IGONetEvent
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

        private Subscription<T> SubscribeInternal<T>(List<EventHandlerAndFilterer> existingHandlersForSpecificType, EventHandlerAndFilterer newHandlerAndPredicate) where T : IGONetEvent
        {
            Type eventType = typeof(T);

            existingHandlersForSpecificType.Add(newHandlerAndPredicate);

            Update_handlersByEventType_IncludingChildren_Deep(eventType);

            ResortSubscribersByPriority();

            var subscription = new Subscription<T>(
                newHandlerAndPredicate,
                existingHandlersForSpecificType,
                () => Update_handlersByEventType_IncludingChildren_Deep(eventType));

            subscription.IsSubscriptionActive = true;

            return subscription;
        }

        private void Update_handlersByEventType_IncludingChildren_Deep(Type eventType)
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

        /// <summary>
        /// IMPORTANT: Only call this from the main Unity thread!
        /// </summary>
        internal void SetSubscriptionPriority(EventHandlerAndFilterer subscriber, int priority)
        {
            GONetMain.EnsureMainThread_IfPlaying();

            subscriber.Priority = priority;

            ResortSubscribersByPriority();
        }

        private void ResortSubscribersByPriority()
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
        }

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
            var specificTypeHandlers_tmp = specificTypeHandlers_tmp_publishCallDepthIndex[genericEnvelope_publishCallDepth];
            specificTypeHandlers_tmp.Clear();

            List<EventHandlerAndFilterer> handlers = null;

            if (includeChildClasses)
            {
                Type eventTypeCurrent = eventType;
                while (eventTypeCurrent != typeof(object))
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
                if (handlersByEventType_IncludingChildren.TryGetValue(currentType, out handlers))
                {
                    int handlerCount = handlers.Count;
                    for (int iHandler = 0; iHandler < handlerCount; ++iHandler)
                    {
                        specificTypeHandlers_tmp.Add(handlers[iHandler]);
                    }
                }
            }

            List<EventHandlerAndFilterer> specificTypeHandlers_tmpList = specificTypeHandlers_tmpList_publishCallDepthIndex[genericEnvelope_publishCallDepth];
            specificTypeHandlers_tmpList.Clear();

            specificTypeHandlers_tmpList.AddRange(specificTypeHandlers_tmp);
            GCLessAlgorithms.QuickSort(specificTypeHandlers_tmpList, EventHandlerAndFilterer.SubscriptionPriorityComparer);

            return specificTypeHandlers_tmpList.Count > 0 ? specificTypeHandlers_tmpList : null;
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
