/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GONet.Utils;

namespace GONet
{
    #region support classes

    public abstract class GONetEventEnvelope
    {
        public virtual ushort SourceAuthorityId { get; set; }

        public virtual bool IsSourceRemote { get; }

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

        public override ushort SourceAuthorityId { get; set; }

        public override bool IsSourceRemote => SourceAuthorityId != GONetMain.MyAuthorityId;

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

            return envelope;
        }

        internal static void Return(GONetEventEnvelope<T> borrowed)
        {
            pool.Return(borrowed);
        }

        internal void Init(T @event, ushort sourceAuthorityId)
        {
            Event = @event;
            SourceAuthorityId = sourceAuthorityId;

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
                    GONetParticipant = GONetMain.GetGONetParticipantById(iHaveRelatedGONetId.GONetId);
                }
            }
            else
            {
                GONetParticipant = GONetMain.GetGONetParticipantById(syncEvent.GONetId);
            }
        }
    }

    public interface IHasPriority
    {
        int Priority { get; }
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

        private GONetEventBus() { }

        /// <summary>
        /// IMPORTANT: since we only allow calls to <see cref="Publish{T}(T, uint?)"/> from one thread (i.e., <see cref="GONetMain.mainUnityThread"/>), we are sure everything is serial calls and only one of these little temporary pass through guys is needed!  The calls to <see cref="GONetEventEnvelope{T}.Borrow(T, uint)"/> is called in the publish bit for each one individually to get the properly typed instance that is automatically returned to its pool after being processed
        /// </summary>
        readonly GONetEventEnvelope<IGONetEvent> genericEnvelope = new GONetEventEnvelope<IGONetEvent>();

        /// <summary>
        /// IMPORTANT: Only call this from the main Unity thread!
        /// </summary>
        public void Publish<T>(T @event, ushort? remoteSourceAuthorityId = default) where T : IGONetEvent
        {
            if (!GONetMain.IsUnityMainThread)
            {
                throw new InvalidOperationException(GONetMain.REQUIRED_CALL_UNITY_MAIN_THREAD);
            }

            List<EventHandlerAndFilterer> handlersForType = LookupSpecificTypeHandlers_FULLY_CACHED(@event.GetType());
            if (handlersForType != null)
            {
                int handlerCount = handlersForType.Count;
                ushort sourceAuthorityId = remoteSourceAuthorityId.HasValue ? remoteSourceAuthorityId.Value : GONetMain.MyAuthorityId;
                genericEnvelope.Init(@event, sourceAuthorityId);
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
                            const string EventType = "(GONetEventBus handler error) Event Type: ";
                            const string GenericEventType = "\n(GONetEventBus handler error) Event Published as generic Type: ";
                            const string Event = "\n(GONetEventBus handler error) Error Event: ";
                            const string StackTrace = "\n(GONetEventBus handler error)  Error Stack Trace: ";
                            GONetLog.Error(string.Concat(EventType, @event.GetType().FullName, GenericEventType, typeof(T).FullName, Event, error.Message, StackTrace, error.StackTrace)); // NOTE: adding in the stack trace is important to see exactly where things went wrong...or else that info is lost
                        }
                    }
                }
            }
            else
            {
                //const string NO_HANDLERS = "Event received, but no handlers to process it.";
                //GONetLog.Info(NO_HANDLERS);
            }

            try
            {
                if (@event is ISelfReturnEvent)
                {
                    ((ISelfReturnEvent)@event).Return();
                }
            }
            catch (Exception e)
            {
                GONetLog.Warning(e.Message);
            }
        }

        /// <summary>
        /// <para>IMPORTANT: It is vitally important that <paramref name="handler"/> code does NOT keep a reference to the envelope or the event inside the envelope.  These items are managed by an object pool for performance reasons.  If for some reason the handler needs to do operations against data inside the envelope or event after that method call is complete (e.g., in a method later on or in a coroutine or another thread) you have to either (a) copy data off of it into other variables or (b) make a copy and if you do that it is your responsibility to return it to the proper pool afterward.  TODO FIXME: add more info as to location of proper pools!</para>
        /// <para>IMPORTANT: Only call this from the main Unity thread!</para>
        /// </summary>
        public Subscription<T> Subscribe<T>(HandleEventDelegate<T> handler, EventFilterDelegate<T> filter = null) where T : IGONetEvent
        {
            if (!GONetMain.IsUnityMainThread)
            {
                throw new InvalidOperationException(GONetMain.REQUIRED_CALL_UNITY_MAIN_THREAD);
            }

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
            if (!GONetMain.IsUnityMainThread)
            {
                throw new InvalidOperationException(GONetMain.REQUIRED_CALL_UNITY_MAIN_THREAD);
            }

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
            List<EventHandlerAndFilterer> handlers = null;

            if (includeChildClasses)
            {
                Type eventTypeCurrent = eventType;
                while (eventTypeCurrent != typeof(object))
                {
                    if (handlersByEventType_IncludingChildren.TryGetValue(eventTypeCurrent, out handlers))
                    {
                        return handlers;
                    }
                    eventTypeCurrent = eventTypeCurrent.BaseType; // keep going up the class hierarchy until we find the first hit...that will contain all relevant observers...even for base classes up hierarchy based on our calls to Update_observersByEventType_IncludingChildren_Deep() earlier on during subscribes
                }

                Type eventInterface;
                Type[] eventInterfaces = GetInterfaces(eventType);
                int length = eventInterfaces.Length;
                for (int i = 0; i < length; ++i)
                {
                    eventInterface = eventInterfaces[i];
                    while (eventInterface != null)
                    {
                        if (handlersByEventType_IncludingChildren.TryGetValue(eventInterface, out handlers))
                        {
                            return handlers;
                        }
                        eventInterface = eventInterface.BaseType; // keep going up the class hierarchy until we find the first hit...that will contain all relevant observers...even for base classes up hierarchy based on our calls to Update_observersByEventType_IncludingChildren_Deep() earlier on during subscribes
                    }
                }
            }
            else
            {
                if (handlersByEventType_SpecificOnly.TryGetValue(eventType, out handlers))
                {
                    return handlers;
                }
            }

            return null;

            // since the filterPredicate cannot safely be compared for equality against another, don't cache the observable mapped to it and return a new observable each time // TODO for better storage/lookup efficiency's sake look into a way to compare filters
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

        public class EventHandlerAndFilterer : IHasPriority
        {
            public static readonly PriorityComparer SubscriptionPriorityComparer = new PriorityComparer();

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

        public class PriorityComparer : IComparer<IHasPriority>
        {
            public int Compare(IHasPriority a, IHasPriority b)
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
                GONetEventEnvelope<T> envelopeTyped = GONetEventEnvelope<T>.Borrow((T)eventEnvelope.EventUntyped, eventEnvelope.SourceAuthorityId, eventEnvelope.GONetParticipant);

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
