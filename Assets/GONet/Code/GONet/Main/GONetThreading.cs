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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GONet
{
    /// <summary>
    /// Provides thread-safe utilities for async/await operations in Unity.
    /// Ensures async continuations execute on Unity's main thread.
    /// </summary>
    public static class GONetThreading
    {
        private static int mainThreadId = -1;
        private static readonly Queue<Action> mainThreadCallbacks = new Queue<Action>();
        private static readonly object callbackLock = new object();
        private static bool isInitialized = false;

        /// <summary>
        /// Initializes the threading system automatically when Unity is ready.
        /// Uses RuntimeInitializeOnLoadMethod to ensure Unity APIs are available.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            if (isInitialized)
                return;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            isInitialized = true;

            // Don't log here - GONetLog might not be initialized yet
            // Will log on first use instead
        }

        /// <summary>
        /// Ensures initialization has happened. Called automatically on first use.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Processes queued callbacks on Unity's main thread. Called from GONetMain.Update.
        /// </summary>
        internal static void ProcessMainThreadCallbacks()
        {
            EnsureInitialized();

            // Process all queued callbacks
            while (true)
            {
                Action callback = null;
                lock (callbackLock)
                {
                    if (mainThreadCallbacks.Count > 0)
                    {
                        callback = mainThreadCallbacks.Dequeue();
                    }
                }

                if (callback == null)
                    break;

                try
                {
                    callback();
                }
                catch (Exception e)
                {
                    GONetLog.Error($"[THREADING] Exception in main thread callback: {e}");
                }
            }
        }

        /// <summary>
        /// Returns true if currently executing on Unity's main thread.
        /// </summary>
        public static bool IsMainThread
        {
            get
            {
                EnsureInitialized();
                return Thread.CurrentThread.ManagedThreadId == mainThreadId;
            }
        }

        /// <summary>
        /// Ensures execution continues on Unity's main thread after await.
        ///
        /// CRITICAL FOR IL2CPP BUILDS: Async continuations may execute on background threads.
        /// Always call this after awaiting tasks before calling Unity APIs.
        ///
        /// Example usage:
        /// <code>
        /// var result = await SomeAsyncOperation();
        /// await GONetThreading.EnsureMainThread(); // REQUIRED before Unity API calls
        /// transform.position = result.position; // Safe - guaranteed on main thread
        /// </code>
        /// </summary>
        /// <param name="timeoutMs">Maximum time to wait for main thread (default 5000ms)</param>
        public static async Task EnsureMainThread(int timeoutMs = 5000)
        {
            EnsureInitialized();

            // Already on main thread - nothing to do
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
            {
                GONetLog.Debug("[THREADING] Already on Unity main thread");
                return;
            }

            GONetLog.Warning($"[THREADING] Detected background thread {Thread.CurrentThread.ManagedThreadId} - marshaling to main thread");

            // Use TaskCompletionSource to wait for main thread callback
            var tcs = new TaskCompletionSource<bool>();
            bool callbackExecuted = false;

            // Schedule callback on main thread
            lock (callbackLock)
            {
                mainThreadCallbacks.Enqueue(() =>
                {
                    callbackExecuted = true;

                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    if (threadId == mainThreadId)
                    {
                        GONetLog.Debug("[THREADING] Successfully marshaled to Unity main thread");
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        GONetLog.Error($"[THREADING] Callback executed on wrong thread {threadId}! This should never happen!");
                        tcs.TrySetResult(false);
                    }
                });
            }

            // Wait for callback with timeout using polling (doesn't block main thread)
            int elapsed = 0;
            const int pollInterval = 10; // ms

            while (!callbackExecuted && elapsed < timeoutMs)
            {
                await Task.Yield();
                elapsed += pollInterval;
                await Task.Delay(pollInterval);
            }

            if (!callbackExecuted)
            {
                GONetLog.Error($"[THREADING] Timeout waiting for main thread callback after {timeoutMs}ms!");
                tcs.TrySetResult(false);
            }

            await tcs.Task;

            // Final verification
            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
            {
                GONetLog.Error($"[THREADING] CRITICAL: Still on thread {Thread.CurrentThread.ManagedThreadId} after marshaling attempt!");
            }
        }

        /// <summary>
        /// ADVANCED USE ONLY: Logs current thread without attempting to marshal.
        /// Use this when you understand threading and want to avoid marshaling overhead.
        /// </summary>
        public static void LogCurrentThread()
        {
            EnsureInitialized();

            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            if (currentThreadId == mainThreadId)
            {
                GONetLog.Debug($"[THREADING] On Unity main thread (ID: {currentThreadId})");
            }
            else
            {
                GONetLog.Warning($"[THREADING] On background thread (ID: {currentThreadId}) - Unity API calls will crash!");
            }
        }
    }
}
