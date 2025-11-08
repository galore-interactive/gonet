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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GONet
{
    /// <summary>
    /// RPC-specific functionality for GONetEventBus extracted for better code organization.
    /// This partial class contains all RPC processing, validation, deferred execution, and persistence logic.
    /// </summary>
    public sealed partial class GONetEventBus
    {
        #region Component Not Ready Exception

        /// <summary>
        /// Exception thrown by generated RPC handlers when the target component doesn't exist yet.
        /// This triggers automatic deferral of the RPC until the component is added.
        ///
        /// NOTE ON DESIGN: Yes, we're using exceptions for flow control here. This is generally an anti-pattern,
        /// but it's justified in this case because:
        /// 1. This is truly exceptional - only happens during late-joiner init with runtime-added components
        /// 2. The frequency is low (connection/scene-load, not hot gameplay loop)
        /// 3. Handlers are already async, so exception overhead is relatively small
        /// 4. Alternatives (sentinel values, shared flags, type registries) are more complex and error-prone
        /// 5. Self-documenting - clearly communicates the deferred RPC pattern
        /// </summary>
        public class RpcComponentNotReadyException : Exception
        {
            public uint GONetId { get; }
            public uint RpcId { get; }

            public RpcComponentNotReadyException(uint gonetId, uint rpcId)
                : base($"Component not ready for RPC 0x{rpcId:X8} on GONetId {gonetId}")
            {
                GONetId = gonetId;
                RpcId = rpcId;
            }
        }

        #endregion

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
            public int LastAttemptedFrame; // Track last frame we attempted this RPC to prevent same-frame retries

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
                LastAttemptedFrame = 0;
                SourceAuthorityId = 0;
                TargetClientAuthorityId = 0;

                pool.Add(this);
            }
        }

        // High-performance collections for deferred RPC tracking
        private static readonly List<DeferredRpcInfo> deferredRpcs = new List<DeferredRpcInfo>(32);
        private static readonly Dictionary<uint, List<DeferredRpcInfo>> deferredRpcsByGoNetId = new Dictionary<uint, List<DeferredRpcInfo>>(16);

        // Configuration constants
        private const float RPC_DEFER_TIMEOUT = 10.0f; // 10 second timeout (generous for late-joiners)
        private const int MAX_RETRY_COUNT = 600; // ~10 seconds at 60fps

        // Performance tracking
        private static int totalDeferredRpcs = 0;
        private static int successfulDeferredRpcs = 0;
        private static int timedOutDeferredRpcs = 0;

        /// <summary>
        /// Public API for generated RPC handlers to defer an RPC when the component doesn't exist yet.
        /// This handles runtime-added components (GONetRuntimeComponentInitializer) that receive RPCs before they're initialized.
        /// </summary>
        public static void DeferRpcFromGeneratedHandler(GONetEventEnvelope<RpcEvent> envelope)
        {
            DeferRpcForLater(envelope.Event, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant, isPersistent: false);
        }

        /// <summary>
        /// Called by GONetParticipantCompanionBehaviour when it becomes ready to receive RPCs.
        /// Does nothing now - ProcessDeferredRpcs() running every frame will automatically retry.
        /// Kept for API compatibility.
        /// </summary>
        public static void OnComponentReadyToReceiveRpcs(GONetParticipant participant)
        {
            // Intentionally empty - ProcessDeferredRpcs() handles all retry logic now
            // This prevents same-frame retry spam that was causing performance issues
        }

        /// <summary>
        /// Clears all deferred RPCs for a specific GONetId when the participant is destroyed.
        /// CRITICAL for preventing infinite defer loops when GONetIds are reused across scene changes.
        /// Without this, persistent RPCs from Scene A targeting GONetId X will try to deliver to
        /// a completely different participant in Scene B that reused GONetId X.
        /// </summary>
        internal static void ClearDeferredRpcsForGONetId(uint gonetId)
        {
            if (gonetId == 0) return;

            // Remove from per-GONetId lookup
            if (deferredRpcsByGoNetId.TryGetValue(gonetId, out List<DeferredRpcInfo> rpcsForId))
            {
                int count = rpcsForId.Count;
                if (count > 0)
                {
                    GONetLog.Debug($"[RPC] Clearing {count} deferred RPC(s) for destroyed participant GONetId {gonetId} to prevent infinite defer loops");

                    // Remove from main list
                    for (int i = deferredRpcs.Count - 1; i >= 0; --i)
                    {
                        var deferred = deferredRpcs[i];
                        uint targetGoNetId = deferred.IsPersistent ? deferred.PersistentRpcEvent.GONetId : deferred.RpcEvent.GONetId;

                        if (targetGoNetId == gonetId)
                        {
                            deferred.Return(); // Return to pool
                            deferredRpcs.RemoveAt(i);
                        }
                    }

                    // Clear the per-GONetId list
                    rpcsForId.Clear();
                }
                deferredRpcsByGoNetId.Remove(gonetId);
            }
        }

        /// <summary>
        /// Defers an RPC for later processing when the target GONetParticipant or component is not yet available.
        /// ProcessDeferredRpcs() will retry automatically every frame until success or timeout.
        /// </summary>
        private static void DeferRpcForLater(RpcEvent rpcEvent, uint sourceAuthorityId, uint targetClientAuthorityId, GONetParticipant sourceGONetParticipant, bool isPersistent = false, PersistentRpcEvent persistentRpcEvent = null)
        {
            uint targetGoNetId = isPersistent ? persistentRpcEvent.GONetId : rpcEvent.GONetId;

            var deferredInfo = DeferredRpcInfo.Borrow();
            deferredInfo.RpcEvent = rpcEvent;
            deferredInfo.PersistentRpcEvent = persistentRpcEvent;
            deferredInfo.SourceAuthorityId = sourceAuthorityId;
            deferredInfo.TargetClientAuthorityId = targetClientAuthorityId;
            deferredInfo.DeferredAtTime = UnityEngine.Time.time;
            deferredInfo.RetryCount = 0;
            deferredInfo.IsPersistent = isPersistent;
            deferredInfo.SourceGONetParticipant = sourceGONetParticipant;
            deferredInfo.LastAttemptedFrame = -1; // Never attempted yet

            deferredRpcs.Add(deferredInfo);

            // Index by GONet ID for faster lookup
            if (!deferredRpcsByGoNetId.ContainsKey(targetGoNetId))
            {
                deferredRpcsByGoNetId[targetGoNetId] = new List<DeferredRpcInfo>(4);
            }
            deferredRpcsByGoNetId[targetGoNetId].Add(deferredInfo);

            totalDeferredRpcs++;
        }

        /// <summary>
        /// High-performance deferred RPC processing - called every frame from GONetGlobal.Update()
        /// Retries deferred RPCs until participant and component are ready.
        /// IMPORTANT: Only one attempt per RPC per frame to prevent same-frame retry spam.
        /// </summary>
        public static void ProcessDeferredRpcs()
        {
            if (deferredRpcs.Count == 0) return;

            int currentFrame = UnityEngine.Time.frameCount;
            float currentTime = UnityEngine.Time.time;

            // Process deferred RPCs in reverse order for efficient removal
            for (int i = deferredRpcs.Count - 1; i >= 0; i--)
            {
                var deferred = deferredRpcs[i];
                uint targetGoNetId = deferred.IsPersistent ? deferred.PersistentRpcEvent.GONetId : deferred.RpcEvent.GONetId;

                // ONE ATTEMPT PER FRAME: Skip if we already tried this RPC this frame
                if (deferred.LastAttemptedFrame == currentFrame)
                {
                    continue;
                }

                // Check timeout
                if (currentTime - deferred.DeferredAtTime > RPC_DEFER_TIMEOUT)
                {
                    GONetLog.Warning($"[RPC TIMEOUT] RPC {(deferred.IsPersistent ? "(persistent)" : "")} 0x{(deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId):X8} for GONetId {targetGoNetId} timed out after {RPC_DEFER_TIMEOUT}s");
                    timedOutDeferredRpcs++;
                    RemoveDeferredRpc(i, deferred, targetGoNetId);
                    continue;
                }

                // Check if GONetParticipant exists
                var gnp = GONetMain.GetGONetParticipantById(targetGoNetId);
                if (gnp != null)
                {
                    // Mark this frame as attempted to prevent re-deferral in same frame
                    deferred.LastAttemptedFrame = currentFrame;

                    // Try to execute the RPC - if component not ready, it will throw ComponentNotReadyException
                    // and DeferRpcForLater will be called, but we won't retry until next frame
                    try
                    {
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

                        // Success! Remove from deferred queue
                        successfulDeferredRpcs++;
                        RemoveDeferredRpc(i, deferred, targetGoNetId);
                    }
                    catch (RpcComponentNotReadyException)
                    {
                        // Component not ready yet - will retry next frame
                        // DeferRpcForLater() was called by the handler, creating a NEW deferred entry
                        // Remove THIS old entry to avoid duplicates
                        RemoveDeferredRpc(i, deferred, targetGoNetId);
                    }
                }
                else
                {
                    // Participant doesn't exist yet - just increment retry count
                    deferred.RetryCount++;

                    if (deferred.RetryCount > MAX_RETRY_COUNT)
                    {
                        GONetLog.Warning($"[RPC TIMEOUT] RPC 0x{(deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId):X8} for GONetId {targetGoNetId} exceeded {MAX_RETRY_COUNT} retries");
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
        /// Debug information about deferred RPC system performance
        /// </summary>
        public static string GetDeferredRpcStats()
        {
            return $"Deferred RPCs - Total: {totalDeferredRpcs}, Successful: {successfulDeferredRpcs}, Timed Out: {timedOutDeferredRpcs}, Currently Pending: {deferredRpcs.Count}";
        }

        #endregion

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

        /// <summary>
        /// Detects if a validation method is async by checking its return type.
        /// </summary>
        /// <param name="method">The validation method to inspect</param>
        /// <returns>True if the method returns Task&lt;RpcValidationResult&gt;, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAsyncValidator(MethodInfo method)
        {
            if (method == null) return false;

            Type returnType = method.ReturnType;

            // Check if return type is Task<RpcValidationResult>
            return returnType.IsGenericType &&
                   returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                   returnType.GetGenericArguments()[0] == typeof(RpcValidationResult);
        }

        /// <summary>
        /// Invokes an async validator method and returns the Task.
        /// </summary>
        /// <param name="method">The async validator method to invoke</param>
        /// <param name="instance">The component instance</param>
        /// <param name="parameters">The parameter array (already deserialized)</param>
        /// <returns>Task that completes with the RpcValidationResult</returns>
        private static Task<RpcValidationResult> InvokeAsyncValidatorAsync(MethodInfo method, object instance, object[] parameters)
        {
            try
            {
                // Invoke the async validator method
                object resultObj = method.Invoke(instance, parameters);

                // Cast to Task<RpcValidationResult>
                return (Task<RpcValidationResult>)resultObj;
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Error invoking async validator {method.Name} on {instance.GetType().Name}: {ex}");

                // Return failed task with error result
                var result = RpcValidationResult.CreatePreAllocated(0);
                result.DenyAll($"Async validator invocation failed: {ex.Message}");
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// Serializes RPC parameters with validated overrides applied.
        /// Creates a merged parameter array (original + overrides) and returns serialized byte data.
        /// </summary>
        /// <param name="originalParams">Original deserialized RPC parameters</param>
        /// <param name="validatedOverrides">Dictionary of parameter index to validated override value</param>
        /// <param name="serializeDelegate">Serialization delegate from generated code (format-specific)</param>
        /// <returns>Byte array containing serialized parameters with overrides applied</returns>
        /// <remarks>
        /// This method applies validated parameter overrides from async validators and serializes
        /// the result into ModifiedData format. The actual serialization mechanism (MemoryPack, etc.)
        /// is provided by the generated code via serializeDelegate.
        ///
        /// Performance: Merging + serialization typically ~0.05-0.15ms for 1-8 parameters.
        /// Boxing overhead from Dictionary&lt;int, object&gt; is acceptable (~0.1ms vs 2000ms saved).
        ///
        /// Integration point: Called from ValidateRpcAsync after async validator completes with overrides.
        /// </remarks>
        private byte[] SerializeParamsWithOverrides(
            object[] originalParams,
            Dictionary<int, object> validatedOverrides,
            Func<object[], byte[]> serializeDelegate)
        {
            if (originalParams == null || originalParams.Length == 0)
            {
                return Array.Empty<byte>(); // No params to serialize
            }

            // Create merged parameter array (original + overrides)
            object[] finalParams = new object[originalParams.Length];

            for (int i = 0; i < originalParams.Length; i++)
            {
                // Use override if present, otherwise use original
                if (validatedOverrides != null && validatedOverrides.TryGetValue(i, out object overrideValue))
                {
                    finalParams[i] = overrideValue;
                }
                else
                {
                    finalParams[i] = originalParams[i];
                }
            }

            // Serialize merged params using format-specific delegate from generated code
            return serializeDelegate(finalParams);
        }

        /// <summary>
        /// Deserializes RPC parameters from byte array for async validation using generated delegates.
        /// Used when client-initiated TargetRPCs need async validation - deserializes serialized params
        /// so they can be passed to async validator as strongly-typed object array.
        /// </summary>
        /// <param name="componentType">Type of component (used to look up deserializer delegate)</param>
        /// <param name="methodName">Name of RPC method (used to look up deserializer delegate)</param>
        /// <param name="data">Serialized RPC parameter data (byte array from RoutedRpcEvent.Data)</param>
        /// <returns>Deserialized parameter array, or null if deserialization fails or no delegate found</returns>
        private object[] DeserializeRpcParameters(Type componentType, string methodName, byte[] data)
        {
            try
            {
                // Lookup the generated typed deserialization delegate
                if (!rpcDeserializersByType.TryGetValue(componentType, out var deserializers))
                {
                    GONetLog.Error($"No deserializers registered for component type {componentType.Name}");
                    return null;
                }

                if (!deserializers.TryGetValue(methodName, out var deserializerDelegate))
                {
                    GONetLog.Error($"No deserializer found for {componentType.Name}.{methodName}");
                    return null;
                }

                // Use the generated delegate to deserialize with correct types
                return deserializerDelegate(data);
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Failed to deserialize {componentType.Name}.{methodName}: {ex.Message}\nStack: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Re-serializes RPC parameters after async validator modifications using generated delegates.
        /// Applies validated overrides from async validator and returns serialized byte array
        /// that can be routed to target clients.
        /// </summary>
        /// <param name="componentType">Type of component (used to look up serializer delegate)</param>
        /// <param name="methodName">Name of RPC method (used to look up serializer delegate)</param>
        /// <param name="originalParams">Original deserialized parameters</param>
        /// <param name="overrides">Dictionary of parameter index → validated override value</param>
        /// <returns>Serialized byte array with overrides applied, or null if serialization fails</returns>
        private byte[] SerializeModifiedRpcParameters(Type componentType, string methodName, object[] originalParams, Dictionary<int, object> overrides)
        {
            try
            {
                // Apply overrides to create final parameter array
                object[] finalParams = new object[originalParams.Length];
                for (int i = 0; i < originalParams.Length; i++)
                {
                    finalParams[i] = overrides.ContainsKey(i) ? overrides[i] : originalParams[i];
                }

                // Lookup the generated typed serialization delegate
                if (!rpcSerializersByType.TryGetValue(componentType, out var serializers))
                {
                    GONetLog.Error($"No serializers registered for component type {componentType.Name}");
                    return null;
                }

                if (!serializers.TryGetValue(methodName, out var serializerDelegate))
                {
                    GONetLog.Error($"No serializer found for {componentType.Name}.{methodName}");
                    return null;
                }

                // Use the generated delegate to serialize with correct types
                return serializerDelegate(finalParams);
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Failed to serialize {componentType.Name}.{methodName}: {ex.Message}\nStack: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Async validation pipeline for RPCs with async validators.
        /// Invokes async validator without blocking Unity main thread, applies parameter overrides,
        /// and returns validation result with WasModified flag and override dictionary.
        /// </summary>
        /// <param name="component">Component instance containing the validator method</param>
        /// <param name="rpcMethodName">Name of the RPC method being validated</param>
        /// <param name="sourceAuthorityId">Authority ID of the RPC caller</param>
        /// <param name="targetAuthorityIds">Array of target authority IDs</param>
        /// <param name="targetCount">Number of valid targets in the array</param>
        /// <param name="rpcParameters">Deserialized RPC parameters (already extracted from byte[])</param>
        /// <returns>Task that completes with validation result (with override dictionary if params changed)</returns>
        /// <remarks>
        /// <para><b>Async Flow (Non-Blocking):</b></para>
        /// <list type="number">
        /// <item>Lookup async validator MethodInfo from asyncValidatorsByType</item>
        /// <item>Set validation context (sourceAuthority, targets) for validator access</item>
        /// <item>Invoke async validator via reflection → AWAITS without blocking Unity thread</item>
        /// <item>Check if validator set parameter overrides via SetValidatedOverride()</item>
        /// <item>Return result with WasModified flag and override dictionary</item>
        /// <item>Clear validation context</item>
        /// </list>
        ///
        /// <para><b>IMPORTANT - ModifiedData Handling:</b></para>
        /// This method does NOT set ModifiedData. The caller must handle serialization of overrides
        /// if result.WasModified is true. This is because serialization format is context-dependent
        /// (MemoryPack for generated code, SerializationUtils for runtime).
        ///
        /// <para><b>Performance:</b></para>
        /// <list type="bullet">
        /// <item>Async validation: 0-2000ms (I/O-bound, non-blocking)</item>
        /// <item>Overhead: ~0.05ms (reflection invocation)</item>
        /// <item>Net improvement: Eliminates 2000ms blocking from main thread</item>
        /// </list>
        ///
        /// <para><b>Integration Points:</b></para>
        /// <list type="bullet">
        /// <item>Called from: RPC processing pipeline (HandleTargetRpcEvent, etc.) when async validator detected</item>
        /// <item>Calls: InvokeAsyncValidatorAsync() for reflection-based async invocation</item>
        /// <item>Caller responsibility: Serialize overrides into ModifiedData if WasModified=true</item>
        /// </list>
        ///
        /// <para><b>Error Handling:</b></para>
        /// Validation context is cleared in finally block to prevent context leaks across RPCs.
        /// Exceptions propagate to caller for logging/reporting.
        /// </remarks>
        private async Task<RpcValidationResult> ValidateRpcAsync(
            object component,
            string rpcMethodName,
            ushort sourceAuthorityId,
            ushort[] targetAuthorityIds,
            int targetCount,
            object[] rpcParameters)
        {
            Type componentType = component.GetType();

            // Check if async validator is registered for this RPC method
            if (asyncValidatorsByType.TryGetValue(componentType, out var asyncDict) &&
                asyncDict.TryGetValue(rpcMethodName, out var asyncValidatorMethod))
            {
                // Set up validation context for async validator
                var result = RpcValidationResult.CreatePreAllocated(targetCount);
                var validationContext = new RpcValidationContext
                {
                    SourceAuthorityId = sourceAuthorityId,
                    TargetAuthorityIds = targetAuthorityIds,
                    TargetCount = targetCount,
                    PreAllocatedResult = result
                };
                SetValidationContext(validationContext);

                try
                {
                    // Invoke async validator - THIS AWAITS WITHOUT BLOCKING UNITY MAIN THREAD!
                    result = await InvokeAsyncValidatorAsync(
                        asyncValidatorMethod,
                        component,
                        rpcParameters);

                    // NOTE: ModifiedData serialization is caller's responsibility
                    // Caller must check result.WasModified and result.GetValidatedOverrides()
                    // and serialize using appropriate method (MemoryPack, SerializationUtils, etc.)

                    return result;
                }
                finally
                {
                    // Clear validation context to prevent leaks across RPCs
                    ClearValidationContext();
                }
            }

            // No async validator registered - return default allow-all result
            var defaultResult = RpcValidationResult.CreatePreAllocated(targetCount);
            defaultResult.AllowAll();
            return defaultResult;
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

        // Async validator storage: MethodInfo for reflection-based async invocation
        // Stored separately because async validators cannot be compiled to delegates (Task<T> return type)
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo>> asyncValidatorsByType = new();

        // RPC deserialization delegates: Typed deserialization from byte[] -> object[] for async validation
        // Generated by code generation, each delegate knows the correct RpcDataN<T1, T2, ...> type to use
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<byte[], object[]>>> rpcDeserializersByType = new();

        // RPC serialization delegates: Typed serialization from object[] -> byte[] for async validation parameter modification
        // Generated by code generation, each delegate knows the correct RpcDataN<T1, T2, ...> type to use
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<object[], byte[]>>> rpcSerializersByType = new();

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

        /// <summary>
        /// Registers async validation methods for a component type.
        /// Thread-safe registration of async validators that return Task&lt;RpcValidationResult&gt;.
        /// </summary>
        /// <param name="type">Component type to register validators for</param>
        /// <param name="asyncValidators">Dictionary of method name to MethodInfo</param>
        /// <param name="parameterCounts">Dictionary of method name to parameter count</param>
        public void RegisterAsyncValidators(Type type, Dictionary<string, MethodInfo> asyncValidators, Dictionary<string, int> parameterCounts)
        {
            // Convert to concurrent dictionaries for thread-safe access during RPC processing
            var concurrentAsyncValidators = new ConcurrentDictionary<string, MethodInfo>(asyncValidators);
            var concurrentParamCounts = new ConcurrentDictionary<string, int>(parameterCounts);

            asyncValidatorsByType.AddOrUpdate(type, concurrentAsyncValidators, (key, existing) => concurrentAsyncValidators);
            validatorParameterCounts.AddOrUpdate(type, concurrentParamCounts, (key, existing) => concurrentParamCounts);
        }

        /// <summary>
        /// Registers RPC deserialization delegates for async validation.
        /// These typed delegates convert byte[] to object[] using the correct RpcDataN types.
        /// </summary>
        /// <param name="type">Component type to register deserializers for</param>
        /// <param name="deserializers">Dictionary of method name to deserialization delegate</param>
        public void RegisterRpcDeserializers(Type type, Dictionary<string, Func<byte[], object[]>> deserializers)
        {
            // Convert to concurrent dictionary for thread-safe access
            var concurrentDeserializers = new ConcurrentDictionary<string, Func<byte[], object[]>>(deserializers);
            rpcDeserializersByType.AddOrUpdate(type, concurrentDeserializers, (key, existing) => concurrentDeserializers);
        }

        /// <summary>
        /// Registers RPC serialization delegates for async validation parameter modification.
        /// These typed delegates convert object[] to byte[] using the correct RpcDataN types.
        /// </summary>
        /// <param name="type">Component type to register serializers for</param>
        /// <param name="serializers">Dictionary of method name to serialization delegate</param>
        public void RegisterRpcSerializers(Type type, Dictionary<string, Func<object[], byte[]>> serializers)
        {
            // Convert to concurrent dictionary for thread-safe access
            var concurrentSerializers = new ConcurrentDictionary<string, Func<object[], byte[]>>(serializers);
            rpcSerializersByType.AddOrUpdate(type, concurrentSerializers, (key, existing) => concurrentSerializers);
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

        /// <summary>
        /// Helper method to build RpcDeliveryReport from validation result.
        /// Consolidates delivery report construction logic to avoid code duplication across argument variants.
        /// </summary>
        /// <param name="validationResult">The validation result containing allow/deny information</param>
        /// <param name="targetBuffer">Buffer containing target authority IDs</param>
        /// <param name="reportId">Validation report ID (0 if no report stored)</param>
        /// <returns>Constructed RpcDeliveryReport with all fields populated</returns>
        private RpcDeliveryReport BuildDeliveryReport(RpcValidationResult validationResult, ushort[] targetBuffer, ulong reportId)
        {
            var allowedTargets = validationResult.GetAllowedTargetsList(targetBuffer);
            var deniedTargets = validationResult.GetDeniedTargetsList(targetBuffer);

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedTargets,
                FailedDelivery = deniedTargets,
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.ModifiedData != null,
                ValidationReportId = reportId,
                ExpectFollowOnResponse = validationResult.ExpectFollowOnResponse // Propagate async flag
            };
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

            // CRITICAL: Check if any deferred RPCs are waiting for this handler
            // This handles runtime-added components that receive RPCs before they're initialized
            ProcessDeferredRpcsForHandler(rpcId);
        }

        /// <summary>
        /// Processes any deferred RPCs that are waiting for a specific RPC handler to be registered.
        /// Called automatically when a new handler is registered via RegisterRpcHandler.
        /// </summary>
        private static void ProcessDeferredRpcsForHandler(uint rpcId)
        {
            if (deferredRpcs.Count == 0) return;

            // Find and process all deferred RPCs for this handler
            for (int i = deferredRpcs.Count - 1; i >= 0; i--)
            {
                var deferred = deferredRpcs[i];
                uint deferredRpcId = deferred.IsPersistent ? deferred.PersistentRpcEvent.RpcId : deferred.RpcEvent.RpcId;

                if (deferredRpcId == rpcId)
                {
                    uint targetGoNetId = deferred.IsPersistent ? deferred.PersistentRpcEvent.GONetId : deferred.RpcEvent.GONetId;
                    var gnp = GONetMain.GetGONetParticipantById(targetGoNetId);

                    if (gnp != null)
                    {
                        GONetLog.Debug($"RPC handler 0x{rpcId:X8} now registered - processing deferred {(deferred.IsPersistent ? "persistent " : "")}RPC");

                        successfulDeferredRpcs++;
                        if (deferred.IsPersistent)
                        {
                            var envelope = GONetEventEnvelope<PersistentRpcEvent>.Borrow(deferred.PersistentRpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                            envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;
                            Instance.HandlePersistentRpcForMe(envelope);
                        }
                        else
                        {
                            var envelope = GONetEventEnvelope<RpcEvent>.Borrow(deferred.RpcEvent, (ushort)deferred.SourceAuthorityId, gnp, (ushort)deferred.SourceAuthorityId);
                            envelope.TargetClientAuthorityId = (ushort)deferred.TargetClientAuthorityId;
                            Instance.HandleRpcForMe_Immediate(envelope);
                        }

                        RemoveDeferredRpc(i, deferred, targetGoNetId);
                    }
                }
            }
        }

        private async void HandleRpcForMe(GONetEventEnvelope<RpcEvent> envelope)
        {
            var rpcEvent = envelope.Event;

            GONetLog.Debug($"[TARGETRPC-HANDLER] HandleRpcForMe called: RpcId={rpcEvent.RpcId}, OriginatorId={rpcEvent.OriginatorAuthorityId}, MyId={GONetMain.MyAuthorityId}, HasValidation={rpcEvent.HasValidation}");

            // CRITICAL: ServerRpc should ONLY execute on the server, never on clients
            // Check if this is a ServerRpc by looking up its metadata
            if (componentTypeByRpcId.TryGetValue(rpcEvent.RpcId, out var componentType) &&
                rpcMetadataByType.TryGetValue(componentType, out var typeMetadata))
            {
                string methodName = GetMethodNameFromRpcId(rpcEvent.RpcId);
                if (methodName != null && typeMetadata.TryGetValue(methodName, out var metadata))
                {
                    if (metadata.Type == RpcType.ServerRpc && !GONetMain.IsServer)
                    {
                        GONetLog.Debug($"[SERVERRPC-HANDLER] SKIPPING ServerRpc execution on client (should only execute on server)");
                        return;
                    }
                }
            }

            // CRITICAL: Prevent double-execution for non-validated TargetRPCs
            // If this RPC:
            //   1. Was originated by us (we called it locally)
            //   2. AND has no validation (no server approval needed)
            // Then we ALREADY executed it locally - skip execution here to avoid duplicate
            bool isOriginator = rpcEvent.OriginatorAuthorityId == GONetMain.MyAuthorityId;
            bool hasNoValidation = !rpcEvent.HasValidation;
            bool shouldSkip = isOriginator && hasNoValidation;

            GONetLog.Debug($"[TARGETRPC-HANDLER] Double-exec check: isOriginator={isOriginator} ({rpcEvent.OriginatorAuthorityId}=={GONetMain.MyAuthorityId}), hasNoValidation={hasNoValidation}, shouldSkip={shouldSkip}");

            if (shouldSkip)
            {
                GONetLog.Debug($"[TARGETRPC-HANDLER] SKIPPING execution (already executed locally as originator)");
                // We already executed this locally when we called it - don't execute again
                return;
            }

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
                // CRITICAL: Handler not registered yet - this can happen when runtime-added components
                // receive RPCs before they're fully initialized (e.g., GONetRuntimeComponentInitializer)
                // Defer the RPC until the handler is registered
                GONetLog.Debug($"Handler not registered yet for RPC ID: 0x{rpcEvent.RpcId:X8} - deferring until component is ready");
                DeferRpcForLater(rpcEvent, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: false);
                return;
            }

            // Set context for this RPC execution
            currentRpcContext = new GONetRpcContext(envelope);

            try
            {
                await handler(envelope);
                // The handler will send response if needed
            }
            catch (RpcComponentNotReadyException)
            {
                // Component doesn't exist yet - defer the RPC until it's added
                // NOTE: This can generate significant log output during late-joiner connection
                // To enable this logging, add LOG_RPC_VERBOSE to Player Settings → Scripting Define Symbols
                #if LOG_RPC_VERBOSE
                GONetLog.Debug($"Component not ready for RPC 0x{rpcEvent.RpcId:X8} on GONetId {rpcEvent.GONetId} - deferring");
                #endif
                DeferRpcForLater(rpcEvent, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: false);
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

            // Check if this RPC has an async validator - if so, defer to async handler
            if (asyncValidatorsByType.TryGetValue(componentType, out var asyncValidators) &&
                asyncValidators.ContainsKey(methodName))
            {
                // Extract values from pooled event BEFORE going async (event will be returned to pool immediately)
                uint rpcId = evt.RpcId;
                uint gonetId = evt.GONetId;
                byte[] eventData = evt.Data;
                int targetCount = evt.TargetCount;
                ushort[] targets = new ushort[targetCount];
                Array.Copy(evt.TargetAuthorities, targets, targetCount);
                ushort originatorAuthorityId = evt.OriginatorAuthorityId;
                bool hasValidation = evt.HasValidation;
                bool shouldExpandToAllClients = evt.ShouldExpandToAllClients;

                // Fire and forget - async handler will route after validation completes
                _ = Server_HandleRoutedRpcFromClientAsync(rpcId, gonetId, eventData, targets, targetCount, sourceAuthority, componentType, component, methodName, rpcMeta, originatorAuthorityId, hasValidation, shouldExpandToAllClients);
                return;
            }

            // No async validator - use sync validation path
            var targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            try
            {
                Array.Copy(evt.TargetAuthorities, targetBuffer, evt.TargetCount);
                int targetCount = evt.TargetCount;
                int originalTargetCount = targetCount;

                GONetLog.Debug($"[TARGETRPC-SERVER] Received RoutedRpc: Method={methodName}, OriginalTargetCount={originalTargetCount}, OriginalTargets=[{string.Join(",", targetBuffer.Take(originalTargetCount))}], ShouldExpand={evt.ShouldExpandToAllClients}, OriginatorId={evt.OriginatorAuthorityId}");

                // Expand target list if this was RpcTarget.All or RpcTarget.Others
                if (evt.ShouldExpandToAllClients)
                {
                    // Add all connected clients that aren't already in the list
                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                    {
                        ushort clientId = client.ConnectionToClient.OwnerAuthorityId;
                        bool alreadyInList = false;
                        for (int i = 0; i < targetCount; i++)
                        {
                            if (targetBuffer[i] == clientId)
                            {
                                alreadyInList = true;
                                break;
                            }
                        }
                        if (!alreadyInList && targetCount < MAX_RPC_TARGETS)
                        {
                            targetBuffer[targetCount++] = clientId;
                        }
                    }

                    GONetLog.Debug($"[TARGETRPC-SERVER] After expansion: ExpandedTargetCount={targetCount}, ExpandedTargets=[{string.Join(",", targetBuffer.Take(targetCount))}]");
                }

                // Use enhanced validation system for profanity filtering and other validation
                RpcValidationResult validationResult;
                if (enhancedValidatorsByType.TryGetValue(componentType, out var validators) &&
                    validators.TryGetValue(methodName, out var validatorObj) &&
                    validatorParameterCounts.TryGetValue(componentType, out var paramCounts) &&
                    paramCounts.TryGetValue(methodName, out var paramCount))
                {
                    // Invoke enhanced validator (with profanity filtering) - use EXPANDED targetCount
                    validationResult = InvokeValidator(validatorObj, paramCount, component, sourceAuthority, targetBuffer, targetCount, evt.Data);
                }
                else
                {
                    // Fallback to basic connection validation - use EXPANDED targetCount
                    validationResult = RpcValidationResult.CreatePreAllocated(targetCount);
                    for (int i = 0; i < targetCount; i++)
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
                        // Skip sending back to originator if no validation (they already executed locally)
                        bool hasValidation = evt.HasValidation;
                        if (!hasValidation && targetBuffer[i] == evt.OriginatorAuthorityId)
                        {
                            GONetLog.Debug($"[TARGETRPC-SERVER] Skipping originator: TargetId={targetBuffer[i]}, OriginatorId={evt.OriginatorAuthorityId}, HasValidation={hasValidation}");
                            continue; // Originator already executed locally - don't echo back
                        }

                        var rpcEvent = RpcEvent.Borrow();
                        rpcEvent.RpcId = evt.RpcId;
                        rpcEvent.GONetId = evt.GONetId;
                        rpcEvent.Data = dataToUse;
                        rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        rpcEvent.IsSingularRecipientOnly = true;
                        rpcEvent.OriginatorAuthorityId = evt.OriginatorAuthorityId;
                        rpcEvent.HasValidation = evt.HasValidation;

                        GONetLog.Debug($"[TARGETRPC-SERVER] Routing RpcEvent to target: TargetId={targetBuffer[i]}, IsServer={targetBuffer[i] == GONetMain.MyAuthorityId}, Method={methodName}");

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
        /// Async handler for client-initiated TargetRPCs with async validators.
        /// Deserializes parameters, validates asynchronously, re-serializes if modified, and routes to targets.
        /// Ensures execution returns to Unity main thread after async validation completes.
        /// </summary>
        private async Task Server_HandleRoutedRpcFromClientAsync(
            uint rpcId,
            uint gonetId,
            byte[] eventData,
            ushort[] targetAuthorities,
            int targetCount,
            ushort sourceAuthority,
            Type componentType,
            GONetParticipantCompanionBehaviour component,
            string methodName,
            RpcMetadata rpcMeta,
            ushort originatorAuthorityId,
            bool hasValidation,
            bool shouldExpandToAllClients)
        {
            var targetBuffer = targetAuthorityArrayPool.Borrow(MAX_RPC_TARGETS);
            try
            {
                Array.Copy(targetAuthorities, targetBuffer, targetCount);

                // Expand target list if this was RpcTarget.All or RpcTarget.Others
                if (shouldExpandToAllClients)
                {
                    // Add all connected clients that aren't already in the list
                    foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                    {
                        ushort clientId = client.ConnectionToClient.OwnerAuthorityId;
                        bool alreadyInList = false;
                        for (int i = 0; i < targetCount; i++)
                        {
                            if (targetBuffer[i] == clientId)
                            {
                                alreadyInList = true;
                                break;
                            }
                        }
                        if (!alreadyInList && targetCount < MAX_RPC_TARGETS)
                        {
                            targetBuffer[targetCount++] = clientId;
                        }
                    }
                }

                // Deserialize parameters using generated typed delegate
                object[] parameters = DeserializeRpcParameters(componentType, methodName, eventData);
                if (parameters == null)
                {
                    GONetLog.Error($"Failed to deserialize parameters for {componentType.Name}.{methodName}");
                    return;
                }

                // Validate asynchronously (does NOT block Unity main thread)
                var validationResult = await ValidateRpcAsync(component, methodName, sourceAuthority, targetBuffer, targetCount, parameters);

                // Ensure we're back on main thread before routing (Unity API calls required)
                await GONetThreading.EnsureMainThread();

                // If validator modified parameters, serialize them
                byte[] dataToUse = eventData;
                if (validationResult.WasModified)
                {
                    var overrides = validationResult.GetValidatedOverrides();
                    if (overrides != null && overrides.Count > 0)
                    {
                        dataToUse = SerializeModifiedRpcParameters(componentType, methodName, parameters, overrides);

                        if (dataToUse == null)
                        {
                            GONetLog.Error($"Serialization failed - cannot route message");
                            return;
                        }
                    }
                }

                // Route to validated targets
                for (int i = 0; i < validationResult.TargetCount; i++)
                {
                    if (validationResult.AllowedTargets[i])
                    {
                        // Skip sending back to originator if no validation (they already executed locally)
                        if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                        {
                            continue; // Originator already executed locally - don't echo back
                        }

                        var rpcEvent = RpcEvent.Borrow();
                        rpcEvent.RpcId = rpcId;
                        rpcEvent.GONetId = gonetId;
                        rpcEvent.Data = dataToUse;
                        rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        rpcEvent.IsSingularRecipientOnly = true;
                        rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                        rpcEvent.HasValidation = hasValidation;

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
            catch (Exception ex)
            {
                GONetLog.Error($"Exception in async handler for {componentType.Name}.{methodName}: {ex.Message}\nStack: {ex.StackTrace}");
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

            //GONetLog.Debug($"This just in: persistent RPC ID: 0x{rpcEvent.RpcId:X8}, for gonetId: {rpcEvent.GONetId}, exists yet? {participantExists}");

            // CRITICAL: ServerRpc should ONLY execute on the server, never on clients
            // Check if this is a ServerRpc by looking up its metadata
            if (componentTypeByRpcId.TryGetValue(rpcEvent.RpcId, out var componentType) &&
                rpcMetadataByType.TryGetValue(componentType, out var typeMetadata))
            {
                string methodName = GetMethodNameFromRpcId(rpcEvent.RpcId);
                if (methodName != null && typeMetadata.TryGetValue(methodName, out var metadata))
                {
                    if (metadata.Type == RpcType.ServerRpc && !GONetMain.IsServer)
                    {
                        GONetLog.Debug($"[SERVERRPC-HANDLER] SKIPPING persistent ServerRpc execution on client (should only execute on server)");
                        return;
                    }
                }
            }

            // Check if GONetParticipant exists - if not, defer processing
            var targetParticipant = GONetMain.GetGONetParticipantById(rpcEvent.GONetId);
            bool participantExists = targetParticipant != null;

            if (!participantExists)
            {
                // Defer processing until GONetParticipant becomes available
                DeferRpcForLater(null, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: true, persistentRpcEvent: rpcEvent);
                return;
            }

            if (!rpcHandlers.TryGetValue(rpcEvent.RpcId, out var handler))
            {
                // CRITICAL: Handler not registered yet - this can happen when runtime-added components
                // receive persistent RPCs before they're fully initialized (e.g., GONetRuntimeComponentInitializer)
                // Defer the RPC until the handler is registered
                GONetLog.Debug($"Handler not registered yet for persistent RPC ID: 0x{rpcEvent.RpcId:X8} - deferring until component is ready");
                DeferRpcForLater(null, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: true, persistentRpcEvent: rpcEvent);
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
            catch (RpcComponentNotReadyException)
            {
                // Component doesn't exist yet - defer the RPC until it's added
                // NOTE: This can generate significant log output during late-joiner connection
                // To enable this logging, add LOG_RPC_VERBOSE to Player Settings → Scripting Define Symbols
                #if LOG_RPC_VERBOSE
                GONetLog.Debug($"Component not ready for persistent RPC 0x{rpcEvent.RpcId:X8} on GONetId {rpcEvent.GONetId} - deferring");
                #endif
                DeferRpcForLater(null, envelope.SourceAuthorityId, envelope.TargetClientAuthorityId, envelope.GONetParticipant,
                    isPersistent: true, persistentRpcEvent: rpcEvent);
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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                    // Copy to right-sized array (don't store pooled buffer)
                    persistentEvent.Data = new byte[bytesUsed];
                    Buffer.BlockCopy(serialized, 0, persistentEvent.Data, 0, bytesUsed);

                    persistentEvent.SourceAuthorityId = GONetMain.MyAuthorityId;
                    persistentEvent.OriginalTarget = RpcTarget.All;

                    // Return the pooled buffer
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(serialized);
                    }

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

                            GONetLog.Debug($"[TARGETRPC-TARGET-BUILD] RpcTarget.All: MyId={GONetMain.MyAuthorityId}, IsServer={GONetMain.IsServer}, OwnerAuthorityId_Server={GONetMain.OwnerAuthorityId_Server}");

                            if (GONetMain.IsServer)
                            {
                                GONetLog.Debug($"[TARGETRPC-TARGET-BUILD] IsServer branch: Adding connected clients");
                                foreach (GONetRemoteClient client in GONetMain.gonetServer.remoteClients)
                                {
                                    if (targetCount < MAX_RPC_TARGETS)
                                    {
                                        targetBuffer[targetCount++] = client.ConnectionToClient.OwnerAuthorityId;
                                    }
                                }
                            }
                            else
                            {
                                GONetLog.Debug($"[TARGETRPC-TARGET-BUILD] Else branch: Adding server {GONetMain.OwnerAuthorityId_Server} to target list");
                                // Pure client: add server to target list so server can execute and route to other clients
                                targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
                            }

                            GONetLog.Debug($"[TARGETRPC-TARGET-BUILD] Final targetCount={targetCount}, targets=[{string.Join(",", targetBuffer.Take(targetCount))}]");
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

                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName);
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

                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);

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

                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId; // Default to server if server called it

                    // Check RpcContext to see if this came from a client
                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets (no data for 0-param)
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            // Skip sending back to originator if no validation (they already executed locally)
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue; // Originator already executed locally - don't echo back
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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

                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
                {
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;

                    // Check if caller is in target list
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }

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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = hasValidation;
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    // If validation required, wait for server validation before executing
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1);
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

                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);

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

                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId; // Default to server if server called it

                    // Check RpcContext to see if this came from a client
                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            // Skip sending back to originator if no validation (they already executed locally)
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue; // Originator already executed locally - don't echo back
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
                {
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;

                    // Check if caller is in target list
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }

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
                    routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                    routedRpc.HasValidation = hasValidation;
                    routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                    Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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

                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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
                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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
                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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

                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            // Skip sending back to originator if no validation
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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
                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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
                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
                                rpcEvent.OriginatorAuthorityId = originatorAuthorityId;
                                rpcEvent.HasValidation = hasValidation;
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
                                else
                                {
                                    // Pure client: add server to target list so server can execute and route to other clients
                                    targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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
                // Non-authoritative clients send to authoritative server for routing
                if (GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Server)
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

                        // Copy to right-sized array (don't store pooled buffer)
                        persistentRoutedRpc.Data = new byte[bytesUsed];
                        Buffer.BlockCopy(serialized, 0, persistentRoutedRpc.Data, 0, bytesUsed);

                        persistentRoutedRpc.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                        persistentRoutedRpc.SourceAuthorityId = GONetMain.MyAuthorityId;
                        persistentRoutedRpc.OriginalTarget = metadata.Target;
                        persistentRoutedRpc.TargetPropertyName = metadata.TargetPropertyName;

                        // Return the pooled buffer
                        if (needsReturn)
                        {
                            SerializationUtils.ReturnByteArray(serialized);
                        }

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
                        routedRpc.OriginatorAuthorityId = GONetMain.MyAuthorityId;
                        routedRpc.HasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                        routedRpc.ShouldExpandToAllClients = (metadata.Target == RpcTarget.All || metadata.Target == RpcTarget.Others);

                        GONetLog.Debug($"[TARGETRPC-CLIENT] Publishing RoutedRpc: Method={methodName}, TargetCount={targetCount}, Targets=[{string.Join(",", targetBuffer.Take(targetCount))}], ShouldExpand={routedRpc.ShouldExpandToAllClients}");

                        Publish(routedRpc, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: metadata.IsReliable);
                    }

                    // Execute locally NOW only if: (1) we're a target AND (2) no validation required
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    bool callerIsTarget = false;
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (targetBuffer[i] == GONetMain.MyAuthorityId)
                        {
                            callerIsTarget = true;
                            break;
                        }
                    }
                    if (callerIsTarget && !hasValidation)
                    {
                        ExecuteRpcLocally(instance, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
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
                        var report = BuildDeliveryReport(validationResult, targetBuffer, reportId);
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
                    // Determine originator (the authority that initiated this RPC)
                    bool hasValidation = !string.IsNullOrEmpty(metadata.ValidationMethodName);
                    ushort originatorAuthorityId = GONetMain.MyAuthorityId;

                    if (currentRpcContext.HasValue && currentRpcContext.Value.SourceAuthorityId != GONetMain.MyAuthorityId)
                    {
                        originatorAuthorityId = currentRpcContext.Value.SourceAuthorityId;
                    }

                    // Send to allowed targets
                    for (int i = 0; i < validationResult.TargetCount; i++)
                    {
                        if (validationResult.AllowedTargets[i] && targetBuffer[i] != GONetMain.MyAuthorityId)
                        {
                            if (!hasValidation && targetBuffer[i] == originatorAuthorityId)
                            {
                                continue;
                            }

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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
            Publish(rpcEvent, targetClientAuthorityId: GONetMain.OwnerAuthorityId_Server, shouldPublishReliably: isReliable);
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with empty parameters array
                        object[] parameters = new object[0];
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult);
                    }

                    // Server: Validate, route, and generate delivery report (sync fallback)

                    // Validate targets
                    RpcValidationResult syncValidationResult;
                    if (enhancedValidatorsByType.TryGetValue(instance.GetType(), out var validators) &&
                        validators.TryGetValue(methodName, out var validatorObj) &&
                        validatorParameterCounts.TryGetValue(instance.GetType(), out var paramCounts) &&
                        paramCounts.TryGetValue(methodName, out var paramCount))
                    {
                        syncValidationResult = InvokeValidator(validatorObj, paramCount, instance, GONetMain.MyAuthorityId, targetBuffer, targetCount, null);
                    }
                    else
                    {
                        syncValidationResult = Server_DefaultValidation(targetBuffer, targetCount);
                    }

                    // Store validation report if significant
                    ulong reportId = 0;
                    var deniedTargets = syncValidationResult.GetDeniedTargetsList(targetBuffer);
                    if (deniedTargets.Length > 0 || syncValidationResult.ModifiedData != null)
                    {
                        reportId = StoreValidationReport(syncValidationResult);
                    }

                    // Route to allowed targets
                    if (syncValidationResult.TargetCount > 0)
                    {
                        for (int i = 0; i < syncValidationResult.TargetCount; i++)
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
                    var allowedTargets = syncValidationResult.GetAllowedTargetsList(targetBuffer);

                    var deliveryReport = new RpcDeliveryReport
                    {
                        DeliveredTo = allowedTargets,
                        FailedDelivery = deniedTargets,
                        FailureReason = syncValidationResult.DenialReason,
                        WasModified = syncValidationResult.ModifiedData != null,
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with strongly-typed parameters
                        object[] parameters = new object[] { arg1 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1);
                    }

                    // Fall back to sync validation or default
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with strongly-typed parameters
                        object[] parameters = new object[] { arg1, arg2 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2);
                    }

                    // Fall back to sync validation or default
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with strongly-typed parameters
                        object[] parameters = new object[] { arg1, arg2, arg3 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3);
                    }

                    // Fall back to sync validation or default
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with strongly-typed parameters
                        object[] parameters = new object[] { arg1, arg2, arg3, arg4 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3, arg4);
                    }

                    // Fall back to sync validation or default
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
                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with strongly-typed parameters
                        object[] parameters = new object[] { arg1, arg2, arg3, arg4, arg5 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3, arg4, arg5);
                    }

                    // Fall back to sync validation or default
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

                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with object array (to support SetValidatedOverride modifications)
                        object[] parameters = new object[] { arg1, arg2, arg3, arg4, arg5, arg6 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result and apply any parameter modifications
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3, arg4, arg5, arg6);
                    }

                    // Fall back to SYNC validation - serialize first, then validate with byte[]
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

                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with object array (to support SetValidatedOverride modifications)
                        object[] parameters = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result and apply any parameter modifications
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    }

                    // Fall back to SYNC validation - serialize first, then validate with byte[]
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

                    // Check for ASYNC validators first - validate BEFORE serialization with strongly-typed parameters
                    if (asyncValidatorsByType.TryGetValue(instance.GetType(), out var asyncValidators) &&
                        asyncValidators.TryGetValue(methodName, out var asyncValidatorMethod))
                    {
                        // Invoke async validator with object array (to support SetValidatedOverride modifications)
                        object[] parameters = new object[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
                        var validationResult = await ValidateRpcAsync(instance, methodName, GONetMain.MyAuthorityId, targetBuffer, targetCount, parameters);

                        // Handle validation result and apply any parameter modifications
                        return (TResult)(object)await Server_ProcessValidatedTargetsWithDataDoReportingAsync(
                            instance, methodName, metadata, targetBuffer, targetCount, validationResult, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                    }

                    // Fall back to SYNC validation - serialize first, then validate with byte[]
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

        /// <summary>
        /// Processes async-validated TargetRPC targets and applies parameter modifications before routing.
        /// This method is called ONLY when an async validator has completed validation.
        /// Handles parameter modification by async validators via SetValidatedOverride API.
        /// </summary>
        /// <remarks>
        /// ASYNC VALIDATION FLOW:
        /// 1. Client calls TargetRPC with 5 parameters
        /// 2. Server invokes async validator (non-blocking: ValidateMessageAsync)
        /// 3. Validator returns RpcValidationResult with:
        ///    - AllowedTargets[] (bool array, one per target)
        ///    - SetValidatedOverride() calls (if parameters were modified)
        ///    - DenialReason (if any targets were denied)
        /// 4. THIS METHOD applies validation results:
        ///    - Builds allowed/denied lists from bool array
        ///    - Applies parameter overrides (e.g., profanity filter modified "content" param)
        ///    - Serializes (possibly modified) parameters
        ///    - Routes to allowed targets
        ///    - Returns delivery report with success/failure details
        ///
        /// KEY DIFFERENCE FROM SYNC VALIDATION:
        /// - Sync: Serialize first → Validate with byte[] → Route
        /// - Async: Validate with object[] → Apply modifications → Serialize → Route
        ///
        /// This async-first approach enables:
        /// - Non-blocking I/O (web APIs, database lookups)
        /// - Parameter modification via SetValidatedOverride (can't use 'ref' with async)
        /// - Smooth gameplay during validation (no frame stutter)
        ///
        /// PERFORMANCE CHARACTERISTICS:
        /// - Async overhead: ~0.1-0.5ms (Task creation, await, state machine)
        /// - Web API latency: 50-500ms (doesn't block Unity main thread!)
        /// - Serialization: ~0.05-0.2ms (same as sync path)
        /// - Network send: ~0.1-1ms (same as sync path)
        ///
        /// EXAMPLE (from GONetSampleChatSystem):
        /// <code>
        /// [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true,
        ///            validationMethod: nameof(ValidateMessageAsync))]
        /// async Task&lt;RpcDeliveryReport&gt; SendMessage(string content, string channel, ChatType type, ushort from, ushort[] recipients)
        ///
        /// async Task&lt;RpcValidationResult&gt; ValidateMessageAsync(string content, ...)
        /// {
        ///     // Non-blocking profanity filter
        ///     string filtered = await CallProfanityApiAsync(content);
        ///
        ///     var result = validationContext.GetValidationResult();
        ///     result.AllowAll(); // Or selective denial
        ///
        ///     if (filtered != content)
        ///         result.SetValidatedOverride(0, filtered); // Param index 0 = content
        ///
        ///     return result;
        /// }
        /// </code>
        ///
        /// MEMORY SAFETY:
        /// - byte[] returned to SerializationUtils pool in finally block
        /// - bool[] returned to RpcValidationArrayPool in finally block
        /// - RpcEvent instances returned to object pool after send
        ///
        /// VARIANTS:
        /// - 9 overloads exist for 0-8 parameters (this is the 5-param version)
        /// - All follow identical pattern: validate → modify → serialize → route
        /// - See <see cref="HandleTargetRpcWithDeliveryReportAsync{TResult,T1,T2,T3,T4,T5}"/> for invocation
        /// </remarks>
        /// <typeparam name="T1">Type of first RPC parameter</typeparam>
        /// <typeparam name="T2">Type of second RPC parameter</typeparam>
        /// <typeparam name="T3">Type of third RPC parameter</typeparam>
        /// <typeparam name="T4">Type of fourth RPC parameter</typeparam>
        /// <typeparam name="T5">Type of fifth RPC parameter</typeparam>
        /// <param name="instance">Component instance to invoke RPC on</param>
        /// <param name="methodName">Name of the RPC method</param>
        /// <param name="metadata">RPC metadata (reliability, persistence, etc.)</param>
        /// <param name="targetBuffer">Array of target authority IDs (pre-allocated from pool)</param>
        /// <param name="targetCount">Number of valid targets in targetBuffer</param>
        /// <param name="validationResult">Result from async validator (contains allowed/denied/modified data)</param>
        /// <param name="arg1">First RPC parameter (may be overridden by validator)</param>
        /// <param name="arg2">Second RPC parameter (may be overridden by validator)</param>
        /// <param name="arg3">Third RPC parameter (may be overridden by validator)</param>
        /// <param name="arg4">Fourth RPC parameter (may be overridden by validator)</param>
        /// <param name="arg5">Fifth RPC parameter (may be overridden by validator)</param>
        /// <returns>
        /// Delivery report containing:
        /// - DeliveredTo: Array of authority IDs that received the RPC
        /// - FailedDelivery: Array of authority IDs that were denied
        /// - FailureReason: Human-readable reason for denials (e.g., "Profanity detected")
        /// - WasModified: True if validator modified parameters via SetValidatedOverride
        /// - ValidationReportId: Unique ID for retrieving full validation details
        /// </returns>
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3, T4, T5>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
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

            // Check if validator modified parameters via SetValidatedOverride
            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    // Apply overrides (validator used SetValidatedOverride API)
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                    if (overrides.ContainsKey(3)) arg4 = (T4)overrides[3];
                    if (overrides.ContainsKey(4)) arg5 = (T5)overrides[4];
                }
            }

            // Serialize the (possibly modified) parameters
            var data = new RpcData5<T1, T2, T3, T4, T5> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            // Store validation report if significant
            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                // Store the full validation result for later retrieval
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                // 1. Check if server is a target and execute ONCE locally
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    // Execute locally ONCE
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
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
                    remoteEvent.Data = serialized;
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
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 0-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult)
        {
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

            // No parameters to modify for 0-param RPCs

            // Store validation report if significant
            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                // 1. Check if server is a target and execute ONCE locally
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                // 2. Send directly to remote clients
                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 1-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                }
            }

            var data = new RpcData1<T1> { Arg1 = arg1 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 2-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                }
            }

            var data = new RpcData2<T1, T2> { Arg1 = arg1, Arg2 = arg2 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 3-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                }
            }

            var data = new RpcData3<T1, T2, T3> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 4-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3, T4>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                    if (overrides.ContainsKey(3)) arg4 = (T4)overrides[3];
                }
            }

            var data = new RpcData4<T1, T2, T3, T4> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 6-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3, T4, T5, T6>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                    if (overrides.ContainsKey(3)) arg4 = (T4)overrides[3];
                    if (overrides.ContainsKey(4)) arg5 = (T5)overrides[4];
                    if (overrides.ContainsKey(5)) arg6 = (T6)overrides[5];
                }
            }

            var data = new RpcData6<T1, T2, T3, T4, T5, T6> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 7-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3, T4, T5, T6, T7>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                    if (overrides.ContainsKey(3)) arg4 = (T4)overrides[3];
                    if (overrides.ContainsKey(4)) arg5 = (T5)overrides[4];
                    if (overrides.ContainsKey(5)) arg6 = (T6)overrides[5];
                    if (overrides.ContainsKey(6)) arg7 = (T7)overrides[6];
                }
            }

            var data = new RpcData7<T1, T2, T3, T4, T5, T6, T7> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
                ValidationReportId = reportId
            };
        }

        // 8-param async validation handler
        private async Task<RpcDeliveryReport> Server_ProcessValidatedTargetsWithDataDoReportingAsync<T1, T2, T3, T4, T5, T6, T7, T8>(
            GONetParticipantCompanionBehaviour instance, string methodName, RpcMetadata metadata,
            ushort[] targetBuffer, int targetCount, RpcValidationResult validationResult,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            var allowedList = new List<ushort>(targetCount);
            var deniedList = new List<ushort>(targetCount);

            for (int i = 0; i < validationResult.TargetCount; i++)
            {
                if (validationResult.AllowedTargets[i])
                    allowedList.Add(targetBuffer[i]);
                else
                    deniedList.Add(targetBuffer[i]);
            }

            if (validationResult.WasModified)
            {
                var overrides = validationResult.GetValidatedOverrides();
                if (overrides != null)
                {
                    if (overrides.ContainsKey(0)) arg1 = (T1)overrides[0];
                    if (overrides.ContainsKey(1)) arg2 = (T2)overrides[1];
                    if (overrides.ContainsKey(2)) arg3 = (T3)overrides[2];
                    if (overrides.ContainsKey(3)) arg4 = (T4)overrides[3];
                    if (overrides.ContainsKey(4)) arg5 = (T5)overrides[4];
                    if (overrides.ContainsKey(5)) arg6 = (T6)overrides[5];
                    if (overrides.ContainsKey(6)) arg7 = (T7)overrides[6];
                    if (overrides.ContainsKey(7)) arg8 = (T8)overrides[7];
                }
            }

            var data = new RpcData8<T1, T2, T3, T4, T5, T6, T7, T8> { Arg1 = arg1, Arg2 = arg2, Arg3 = arg3, Arg4 = arg4, Arg5 = arg5, Arg6 = arg6, Arg7 = arg7, Arg8 = arg8 };
            int bytesUsed;
            bool needsReturn;
            byte[] serialized = SerializationUtils.SerializeToBytes(data, out bytesUsed, out needsReturn);

            ulong reportId = 0;
            if (deniedList.Count > 0 || validationResult.WasModified)
            {
                reportId = StoreValidationReport(validationResult);
            }

            try
            {
                bool serverIsTarget = allowedList.Contains(GONetMain.MyAuthorityId);

                if (serverIsTarget)
                {
                    var rpcEvent = RpcEvent.Borrow();
                    rpcEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    rpcEvent.GONetId = instance.GONetParticipant.GONetId;
                    rpcEvent.Data = serialized;
                    rpcEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;

                    Publish(rpcEvent, targetClientAuthorityId: GONetMain.MyAuthorityId, shouldPublishReliably: metadata.IsReliable);
                    allowedList.Remove(GONetMain.MyAuthorityId);
                }

                if (allowedList.Count > 0)
                {
                    var remoteEvent = RpcEvent.Borrow();
                    remoteEvent.RpcId = GetRpcId(instance.GetType(), methodName);
                    remoteEvent.GONetId = instance.GONetParticipant.GONetId;
                    remoteEvent.Data = serialized;
                    remoteEvent.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;
                    remoteEvent.IsSingularRecipientOnly = true;

                    var allowedArray = allowedList.ToArray();
                    GONetMain.Server_SendEventToSpecificRemoteConnections(
                        remoteEvent,
                        allowedArray,
                        allowedArray.Length,
                        metadata.IsReliable);

                    remoteEvent.Return();
                }
            }
            finally
            {
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serialized);
                }

                if (validationResult.AllowedTargets != null)
                {
                    RpcValidationArrayPool.ReturnAllowedTargets(validationResult.AllowedTargets);
                }
            }

            return new RpcDeliveryReport
            {
                DeliveredTo = allowedList.ToArray(),
                FailedDelivery = deniedList.ToArray(),
                FailureReason = validationResult.DenialReason,
                WasModified = validationResult.WasModified,
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
                        else
                        {
                            // Pure client: add server to target list so server can execute and route to other clients
                            targetBuffer[targetCount++] = GONetMain.OwnerAuthorityId_Server;
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

            TResult result = await tcs.Task;

            // CRITICAL: Ensure we're back on Unity main thread after RPC response
            // RPC responses may arrive on network threads, so async continuation could be on wrong thread
            await GONetThreading.EnsureMainThread();

            return result;
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

            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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
            TResult result = await tcs.Task;
            await GONetThreading.EnsureMainThread();
            return result;
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

    }
}