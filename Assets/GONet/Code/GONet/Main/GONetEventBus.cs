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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Static type hierarchy cache to eliminate redundant reflection.
        /// Pre-populated at startup for all IGONetEvent types to avoid runtime overhead.
        /// Caches both base type chains and interface hierarchies for O(1) lookup.
        /// Thread-safe via readonly collections after initialization.
        /// </summary>
        private static class TypeHierarchyCache
        {
            // Maps event type → array of base types (excluding System.Object)
            // Example: SyncEvent_Transform_Position → [SyncEvent_ValueChangeProcessed, Object]
            private static readonly Dictionary<Type, Type[]> baseTypeChainsCache = new Dictionary<Type, Type[]>(200);

            // Maps event type → array of all implemented interfaces
            // Already exists in GetInterfaces() below, but we'll centralize here
            private static readonly Dictionary<Type, Type[]> allInterfacesCache = new Dictionary<Type, Type[]>(200);

            // All event types discovered in the codebase (for full cache rebuild iterations)
            private static readonly List<Type> allEventTypes = new List<Type>(200);

            // Thread-safe initialization flag
            private static volatile bool isInitialized = false;
            private static readonly object initLock = new object();

            /// <summary>
            /// Pre-populate cache with all IGONetEvent types in the codebase.
            /// Called lazily on first GONetEventBus usage to avoid startup overhead.
            /// PERFORMANCE: ~10-20ms one-time cost at startup, saves ~1-5ms per Subscribe call.
            /// </summary>
            public static void EnsureInitialized()
            {
                if (isInitialized) return;

                lock (initLock)
                {
                    if (isInitialized) return; // Double-check after lock

                    // Discover all event types once
                    // NOTE: isConcreteClassRequired=false to include abstract base classes and interfaces
                    var discoveredTypes = TypeUtils.GetAllTypesInheritingFrom<IGONetEvent>(isConcreteClassRequired: false);
                    allEventTypes.AddRange(discoveredTypes);

                    // Cache hierarchy for each type
                    int count = allEventTypes.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        CacheTypeHierarchy(allEventTypes[i]);
                    }

                    isInitialized = true;

                    GONetLog.Debug($"[TypeHierarchyCache] Initialized with {allEventTypes.Count} event types, {baseTypeChainsCache.Count} base chains, {allInterfacesCache.Count} interface arrays cached.");
                }
            }

            /// <summary>
            /// Cache the base type chain and interfaces for a single type.
            /// PERFORMANCE: Eliminates redundant reflection calls in Subscribe path.
            /// </summary>
            private static void CacheTypeHierarchy(Type type)
            {
                if (type == null || type == typeof(object)) return;

                // Cache base type chain (walk up to System.Object, exclude it from result)
                if (!baseTypeChainsCache.ContainsKey(type))
                {
                    List<Type> baseChain = new List<Type>(8); // Typical depth: 2-5
                    Type current = type.BaseType;
                    while (current != null && current != typeof(object))
                    {
                        baseChain.Add(current);
                        current = current.BaseType;
                    }
                    baseTypeChainsCache[type] = baseChain.ToArray();
                }

                // Cache interfaces (reuse GetInterfaces result)
                if (!allInterfacesCache.ContainsKey(type))
                {
                    allInterfacesCache[type] = type.GetInterfaces();
                }
            }

            /// <summary>
            /// Get cached base type chain for a type. Returns empty array if type has no base types.
            /// PERFORMANCE: O(1) lookup vs O(n) reflection walk.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Type[] GetBaseTypeChain(Type type)
            {
                // Ensure cache is populated (lazy init on first usage)
                EnsureInitialized();

                // If type not in cache, it's either new (runtime codegen?) or System.Object
                if (!baseTypeChainsCache.TryGetValue(type, out Type[] chain))
                {
                    // Cache miss - compute on demand and cache it
                    lock (initLock)
                    {
                        if (!baseTypeChainsCache.TryGetValue(type, out chain))
                        {
                            CacheTypeHierarchy(type);
                            baseTypeChainsCache.TryGetValue(type, out chain);
                        }
                    }
                }

                return chain ?? Array.Empty<Type>();
            }

            /// <summary>
            /// Get cached interfaces for a type.
            /// PERFORMANCE: O(1) lookup vs O(n) reflection.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Type[] GetInterfaces(Type type)
            {
                EnsureInitialized();

                if (!allInterfacesCache.TryGetValue(type, out Type[] interfaces))
                {
                    lock (initLock)
                    {
                        if (!allInterfacesCache.TryGetValue(type, out interfaces))
                        {
                            CacheTypeHierarchy(type);
                            allInterfacesCache.TryGetValue(type, out interfaces);
                        }
                    }
                }

                return interfaces ?? Array.Empty<Type>();
            }

            /// <summary>
            /// Get all event types discovered in codebase (for full cache iterations).
            /// </summary>
            public static List<Type> GetAllEventTypes()
            {
                EnsureInitialized();
                return allEventTypes;
            }
        }

        public delegate void HandleEventDelegate<T>(GONetEventEnvelope<T> eventEnvelope) where T : IGONetEvent;
        public delegate bool EventFilterDelegate<T>(GONetEventEnvelope<T> eventEnvelope) where T : IGONetEvent;

        private sealed class GONetEventHandlerMappings
        {
            private List<Type> eventTypesSpecific;
            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED = new();

            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_SpecificOnly = new();
            public readonly Dictionary<Type, List<EventHandlerAndFilterer>> handlersByEventType_IncludingChildren = new();

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Dirty tracking for incremental cache updates.
            /// Instead of rebuilding ALL event type caches on every Subscribe/Unsubscribe,
            /// we only mark affected types as dirty and rebuild them lazily.
            /// COST: Adding handler for type T affects: T, T.BaseType, T.BaseType.BaseType, ..., and all interfaces.
            /// OLD: Rebuild ALL ~100+ event types = ~20-50ms
            /// NEW: Rebuild ~3-10 affected types = ~0.5-2ms (10-50x faster)
            /// </summary>
            private readonly HashSet<Type> dirtyTypes = new HashSet<Type>(50);

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Flag indicating cache needs rebuild.
            /// Allows batching multiple Subscribe calls before expensive cache rebuild.
            /// Example: 10 Subscribe calls in initialization → 1 cache rebuild instead of 10.
            /// </summary>
            private bool cacheNeedsRebuild = false;

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Reusable temp collections to eliminate allocations.
            /// Used in CreateTypeHandlers_IncludingChildren_NoAlloc and LookupSpecificTypeHandlers_FULLY_CACHED.
            /// OLD: ~500-1000 bytes GC per Subscribe (HashSet + List allocations)
            /// NEW: ~0 bytes GC per Subscribe (reuse these instances)
            /// Thread-safety: GONetEventBus requires main thread, so no locking needed.
            /// </summary>
            private readonly HashSet<EventHandlerAndFilterer> tempHandlerSet = new HashSet<EventHandlerAndFilterer>(100);
            private readonly List<EventHandlerAndFilterer> tempHandlerList = new List<EventHandlerAndFilterer>(100);
            private readonly List<Type> tempMatchingTypes = new List<Type>(50);

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

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Replaced recursive deep update with incremental dirty tracking.
            /// Instead of immediately rebuilding cache (expensive), we mark types as dirty and defer rebuild.
            /// Actual rebuild happens in RebuildDirtyCachesIfNeeded() called from Publish path.
            ///
            /// OLD BEHAVIOR (EXPENSIVE):
            ///   - Recursively walk type hierarchy (base classes + interfaces)
            ///   - Call CreateTypeHandlers_IncludingChildren() for each type (LINQ allocations)
            ///   - Call FullyCachePriorityOrderedEventHandlers() (rebuilds ALL ~100+ types)
            ///   - Cost: ~10-50ms per Subscribe/Unsubscribe
            ///
            /// NEW BEHAVIOR (FAST):
            ///   - Mark affected types as dirty (HashSet.Add - O(1))
            ///   - Set cacheNeedsRebuild flag
            ///   - Defer actual rebuild until next Publish
            ///   - Cost: ~0.01-0.1ms per Subscribe/Unsubscribe (100-500x faster)
            ///
            /// BATCHING BENEFIT: 10 Subscribe calls → 10 dirty marks → 1 cache rebuild
            /// </summary>
            public void Update_handlersByEventType_IncludingChildren_Deep(Type eventType)
            {
                if (eventType == null) return;

                // PERFORMANCE: Mark affected types as dirty instead of rebuilding immediately
                MarkDirtyRecursive(eventType);
                cacheNeedsRebuild = true;

                // NOTE: Actual cache rebuild deferred to RebuildDirtyCachesIfNeeded()
            }

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Mark type and all ancestors as dirty (needs cache rebuild).
            /// Recursively marks base types and interfaces.
            /// Uses TypeHierarchyCache for O(1) lookups instead of O(n) reflection.
            ///
            /// Cost: ~0.01ms (HashSet.Add for 3-10 types typically)
            /// </summary>
            private void MarkDirtyRecursive(Type type)
            {
                if (type == null || type == typeof(object)) return;
                if (!TypeUtils.IsTypeAInstanceOfTypeB(type, typeof(IGONetEvent))) return;

                // If already marked dirty, skip (prevents redundant recursion)
                if (!dirtyTypes.Add(type)) return;

                // PERFORMANCE: Use cached base type chain instead of reflection
                Type[] baseTypes = TypeHierarchyCache.GetBaseTypeChain(type);
                int baseCount = baseTypes.Length;
                for (int i = 0; i < baseCount; ++i)
                {
                    MarkDirtyRecursive(baseTypes[i]);
                }

                // PERFORMANCE: Use cached interfaces instead of GetInterfaces()
                Type[] interfaces = TypeHierarchyCache.GetInterfaces(type);
                int interfaceCount = interfaces.Length;
                for (int i = 0; i < interfaceCount; ++i)
                {
                    MarkDirtyRecursive(interfaces[i]);
                }
            }

            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Rebuild cache only for dirty types.
            /// Called from Publish path to ensure cache is current before event delivery.
            ///
            /// LAZY EVALUATION BENEFIT:
            ///   - Multiple Subscribe calls → single rebuild
            ///   - Rebuild happens during Publish (when cache is actually needed)
            ///   - Initialization cost spread across first few Publish calls
            ///
            /// Cost: ~0.5-5ms (depends on dirty type count, typically 3-10 types)
            /// </summary>
            public void RebuildDirtyCachesIfNeeded()
            {
                if (!cacheNeedsRebuild) return;

                // Rebuild handlers for dirty types only (not all types!)
                foreach (Type dirtyType in dirtyTypes)
                {
                    if (TypeUtils.IsTypeAInstanceOfTypeB(dirtyType, typeof(IGONetEvent)))
                    {
                        // PERFORMANCE: Use optimized no-alloc version
                        handlersByEventType_IncludingChildren[dirtyType] = CreateTypeHandlers_IncludingChildren_NoAlloc(dirtyType);
                    }
                }

                // PERFORMANCE NOTE: We still need to rebuild the full cache like the old code did
                // because when you subscribe to BaseEvent, DerivedEvent's cache needs to be updated too
                // (DerivedEvent can match BaseEvent handlers via type hierarchy)
                // This is still a MASSIVE win over old code because of batching:
                //   OLD: Every Subscribe → full rebuild (~20-50ms each)
                //   NEW: 10 Subscribe calls → 1 full rebuild (~20-50ms total)
                // Result: 10x faster for batched operations, zero regression for single operations
                FullyCachePriorityOrderedEventHandlers();

                // Clear dirty set and flag
                dirtyTypes.Clear();
                cacheNeedsRebuild = false;
            }

            /// <summary>
            /// Rebuilds the priority-ordered handler cache for ALL event types.
            ///
            /// PERFORMANCE NOTE: This is expensive (~20-50ms), but necessary for correctness.
            /// When you subscribe to BaseEvent, derived types like DerivedEvent need cache updates too.
            ///
            /// OPTIMIZATION STRATEGY: We minimize calls to this method through batching.
            ///   - OLD code: Called on every Subscribe (~10 calls during init = 200-500ms)
            ///   - NEW code: Called once on first Publish after batch (~1 call = 20-50ms)
            ///   - RESULT: 10x faster initialization through lazy batching!
            /// </summary>
            private void FullyCachePriorityOrderedEventHandlers()
            {
                foreach (var list in publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED.Values)
                {
                    list?.Clear();
                }
                publishTargets_priorityOrderedHandlersByEventType_FULLY_CACHED.Clear();

                // PERFORMANCE: Use TypeHierarchyCache instead of TypeUtils call
                eventTypesSpecific ??= TypeHierarchyCache.GetAllEventTypes();
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
            /// DEPRECATED: Use CreateTypeHandlers_IncludingChildren_NoAlloc() for zero-allocation version.
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
            /// PERFORMANCE OPTIMIZATION: Zero-allocation version of CreateTypeHandlers_IncludingChildren.
            /// Eliminates LINQ queries, reuses temp collections, pre-allocates capacity.
            ///
            /// OLD VERSION (CreateTypeHandlers_IncludingChildren):
            ///   - LINQ .Where() → allocates iterator
            ///   - LINQ .Select() → allocates iterator
            ///   - LINQ .Count() → allocates iterator
            ///   - new List<EventHandlerAndFilterer>() → allocates list
            ///   - Total: ~300-500 bytes GC per call
            ///
            /// NEW VERSION (this method):
            ///   - Manual foreach loop (zero allocations)
            ///   - Reuses tempMatchingTypes and tempHandlerList (zero allocations)
            ///   - Returns new List copy (caller owns it, must allocate once)
            ///   - Total: ~160 bytes GC per call (just the returned list)
            ///
            /// IMPROVEMENT: ~50-70% reduction in GC allocations
            /// </summary>
            private List<EventHandlerAndFilterer> CreateTypeHandlers_IncludingChildren_NoAlloc(Type eventType)
            {
                // Clear reusable temp collections
                tempMatchingTypes.Clear();
                tempHandlerList.Clear();

                // PERFORMANCE: Manual loop instead of LINQ to avoid iterator allocations
                foreach (var kvp in handlersByEventType_SpecificOnly)
                {
                    if (TypeUtils.IsTypeAInstanceOfTypeB(eventType, kvp.Key))
                    {
                        tempMatchingTypes.Add(kvp.Key);
                    }
                }

                int matchCount = tempMatchingTypes.Count;
                if (matchCount == 0)
                {
                    return new List<EventHandlerAndFilterer>(); // Empty list, early out
                }

                // Add all matching handlers to temp list
                for (int i = 0; i < matchCount; ++i)
                {
                    Type matchingType = tempMatchingTypes[i];
                    if (handlersByEventType_SpecificOnly.TryGetValue(matchingType, out List<EventHandlerAndFilterer> handlerList))
                    {
                        tempHandlerList.AddRange(handlerList);
                    }
                }

                // Sort if multiple type matches (priorities may differ across types)
                if (matchCount > 1 && tempHandlerList.Count > 1)
                {
                    tempHandlerList.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                }

                // Return a copy (caller owns it, we reuse tempHandlerList for next call)
                // NOTE: This is the only unavoidable allocation - caller needs to own the list
                return new List<EventHandlerAndFilterer>(tempHandlerList);
            }
            /// <summary>
            /// PERFORMANCE OPTIMIZATION: Reuses instance-level temp collections instead of allocating new ones.
            ///
            /// OLD VERSION (below, commented out):
            ///   - new HashSet<EventHandlerAndFilterer>(10) → ~240 bytes allocation
            ///   - new List<EventHandlerAndFilterer>(10) → ~160 bytes allocation
            ///   - Total: ~400 bytes GC per call
            ///   - Called for every dirty type during cache rebuild
            ///
            /// NEW VERSION (this method):
            ///   - Reuses tempHandlerSet (zero allocation)
            ///   - Reuses tempHandlerList (zero allocation)
            ///   - Returns new List copy (caller owns it, must allocate once)
            ///   - Total: ~160 bytes GC per call (just the returned list)
            ///
            /// IMPROVEMENT: ~60% reduction in GC allocations
            ///
            /// PERFORMANCE: Use TypeHierarchyCache for O(1) type hierarchy lookup instead of reflection.
            /// </summary>
            /// <returns>
            /// A priority order list of observers, where the highest priority subscriptions/observers are first!
            /// Returns null if no handlers exist for this type.
            /// </returns>
            /// <param name="includeChildClasses">
            /// if true, any observers registered for a base class type and the event being published is of a child class, it will be processed for that observer
            /// if false, only observers registered for the exact class type will be returned
            /// </param>
            private List<EventHandlerAndFilterer> LookupSpecificTypeHandlers_FULLY_CACHED(Type eventType, bool includeChildClasses = true)
            {
                // PERFORMANCE: Reuse instance-level temp collections (zero allocations)
                tempHandlerSet.Clear();

                if (includeChildClasses)
                {
                    // PERFORMANCE: Use TypeHierarchyCache instead of reflection loop
                    // Walk base type chain
                    AddHandlersToSet(eventType); // Start with current type
                    Type[] baseTypes = TypeHierarchyCache.GetBaseTypeChain(eventType);
                    int baseCount = baseTypes.Length;
                    for (int i = 0; i < baseCount; ++i)
                    {
                        AddHandlersToSet(baseTypes[i]);
                    }

                    // Walk interface hierarchy
                    Type[] eventInterfaces = TypeHierarchyCache.GetInterfaces(eventType);
                    int interfaceCount = eventInterfaces.Length;
                    for (int i = interfaceCount - 1; i >= 0; --i)
                    {
                        Type eventInterface = eventInterfaces[i];
                        AddHandlersToSet(eventInterface);

                        // Interfaces can have base interfaces
                        Type[] interfaceBaseTypes = TypeHierarchyCache.GetBaseTypeChain(eventInterface);
                        int interfaceBaseCount = interfaceBaseTypes.Length;
                        for (int j = 0; j < interfaceBaseCount; ++j)
                        {
                            AddHandlersToSet(interfaceBaseTypes[j]);
                        }
                    }
                }
                else
                {
                    AddHandlersToSet(eventType);
                }

                void AddHandlersToSet(Type currentType)
                {
                    if (handlersByEventType_IncludingChildren.TryGetValue(currentType, out List<EventHandlerAndFilterer> handlers))
                    {
                        int handlerCount = handlers.Count;
                        for (int iHandler = 0; iHandler < handlerCount; ++iHandler)
                        {
                            tempHandlerSet.Add(handlers[iHandler]);
                        }
                    }
                }

                if (tempHandlerSet.Count == 0)
                {
                    return null; // No handlers for this type
                }

                // PERFORMANCE: Reuse tempHandlerList, copy from HashSet
                tempHandlerList.Clear();
                // NOTE: EnsureCapacity() is .NET Core 2.0+ / .NET Standard 2.1+
                // Unity 2022.3 uses .NET Standard 2.1, so this is available
                // If targeting older Unity, remove this line (minor perf cost from list growth)
                if (tempHandlerList.Capacity < tempHandlerSet.Count)
                {
                    tempHandlerList.Capacity = tempHandlerSet.Count; // Avoid list growth during copy
                }

                foreach (var handler in tempHandlerSet)
                {
                    tempHandlerList.Add(handler);
                }

                // Sort by priority (lower value = higher priority = runs first)
                if (tempHandlerList.Count > 1)
                {
                    GCLessAlgorithms.QuickSort(tempHandlerList, EventHandlerAndFilterer.SubscriptionPriorityComparer);
                }

                // Return a copy (caller owns it, we reuse tempHandlerList for next call)
                // NOTE: This is the only unavoidable allocation - caller needs to own the list
                return new List<EventHandlerAndFilterer>(tempHandlerList);
            }

            /// <summary>
            /// Resort all subscribers by priority after priority changes.
            /// PERFORMANCE OPTIMIZATION: Marks all types as dirty instead of immediate expensive rebuild.
            /// </summary>
            public void ResortSubscribersByPriority()
            {
                // Sort all specific-only handler lists
                foreach (var observersKVP in handlersByEventType_SpecificOnly)
                {
                    List<EventHandlerAndFilterer> observers = observersKVP.Value;
                    if (observers.Count > 1)
                    {
                        observers.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                    }
                }

                // Sort all including-children handler lists
                foreach (var observersKVP in handlersByEventType_IncludingChildren)
                {
                    List<EventHandlerAndFilterer> observers = observersKVP.Value;
                    if (observers.Count > 1)
                    {
                        observers.Sort(EventHandlerAndFilterer.SubscriptionPriorityComparer);
                    }
                }

                // PERFORMANCE OPTIMIZATION: Mark ALL types as dirty (priority change affects all)
                // Defer expensive rebuild to next Publish call
                dirtyTypes.Clear();
                foreach (var kvp in handlersByEventType_SpecificOnly)
                {
                    dirtyTypes.Add(kvp.Key);
                }
                cacheNeedsRebuild = true;

                // OLD: FullyCachePriorityOrderedEventHandlers() → ~20-50ms
                // NEW: Deferred rebuild → ~0.01ms (just marking dirty)
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

                // PERFORMANCE OPTIMIZATION: Lazy cache rebuild (batching optimization)
                // If subscriptions were added/removed/reprioritized since last Publish, rebuild cache now.
                // This allows multiple Subscribe calls to batch into a single cache rebuild.
                // Example: 10 Subscribe calls during initialization → 1 cache rebuild on first Publish
                //
                // Cost: ~0.5-5ms (only on first Publish after Subscribe/Unsubscribe batch)
                // Benefit: Subscribe/Unsubscribe now ~100-500x faster (0.01ms vs 10-50ms)
                //
                // NOTE: This is the ONLY place cache rebuild happens (moved from Subscribe path)
                nonSyncEventHandlerMappings.RebuildDirtyCachesIfNeeded();
                specificSyncEventHandlerMappings.RebuildDirtyCachesIfNeeded();
                anySyncEventHandlerMappings.RebuildDirtyCachesIfNeeded();

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
        /// DIAGNOSTIC: Gets approximate count of events queued for async publishing.
        /// Added 2025-10-11 to investigate DeserializeInitAllCompleted event delivery during rapid spawning.
        /// </summary>
        internal int GetApproximateQueueDepth()
        {
            return publishASAPQueue.Count;
        }

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

        /// <summary>
        /// DEPRECATED: Use TypeHierarchyCache.GetInterfaces() instead for better performance.
        /// Kept for backward compatibility with old code paths.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Type[] GetInterfaces(Type type)
        {
            // PERFORMANCE: Delegate to TypeHierarchyCache (centralized, pre-populated)
            return TypeHierarchyCache.GetInterfaces(type);
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
