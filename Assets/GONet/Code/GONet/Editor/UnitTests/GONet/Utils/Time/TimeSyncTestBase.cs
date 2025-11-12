using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Base class for all time sync tests that handles proper setup/teardown
    /// including static field resets and thread management
    /// </summary>
    public abstract class TimeSyncTestBase
    {

        // Mock request message for testing
        internal class MockRequestMessage : RequestMessage
        {
            public MockRequestMessage(long occurredAtTicks) : base(occurredAtTicks) { }
        }


        protected CancellationTokenSource cts;

        [SetUp]
        public virtual void BaseSetUp()
        {
            cts = new CancellationTokenSource();
            ResetAllStaticFields();
        }

        [TearDown]
        public virtual void BaseTearDown()
        {
            try
            {
                // Only cancel if not already disposed
                if (cts != null && !cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            try
            {
                cts?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            // Give threads time to process cancellation
            Thread.Sleep(100);

            // Reset static fields again to ensure clean state
            ResetAllStaticFields();
        }

        /// <summary>
        /// Resets all static fields in time sync related classes to ensure test isolation
        /// </summary>
        private void ResetAllStaticFields()
        {
            // Reset HighPerfTimeSync static fields
            var highPerfTimeSyncType = typeof(HighPerfTimeSync);

            // Reset lastProcessedResponseTicks
            var lastProcessedField = highPerfTimeSyncType.GetField("lastProcessedResponseTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            lastProcessedField?.SetValue(null, 0L);

            // Reset lastAdjustmentTicks
            var lastAdjustmentField = highPerfTimeSyncType.GetField("lastAdjustmentTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            lastAdjustmentField?.SetValue(null, 0L);

            // Reset adjustmentCount
            var adjustmentCountField = highPerfTimeSyncType.GetField("adjustmentCount",
                BindingFlags.NonPublic | BindingFlags.Static);
            adjustmentCountField?.SetValue(null, 0);

            // Reset rttWriteIndex
            var rttWriteIndexField = highPerfTimeSyncType.GetField("rttWriteIndex",
                BindingFlags.NonPublic | BindingFlags.Static);
            rttWriteIndexField?.SetValue(null, 0);

            // Clear RTT buffer
            var rttBufferField = highPerfTimeSyncType.GetField("rttBuffer",
                BindingFlags.NonPublic | BindingFlags.Static);
            var rttBuffer = rttBufferField?.GetValue(null);
            if (rttBuffer != null && rttBuffer is Array bufferArray)
            {
                var elementType = bufferArray.GetType().GetElementType();
                var timestampField = elementType?.GetField("Timestamp");
                var valueField = elementType?.GetField("Value");

                for (int i = 0; i < bufferArray.Length; i++)
                {
                    var element = bufferArray.GetValue(i);
                    if (element != null && timestampField != null && valueField != null)
                    {
                        // Create new struct instance with zeroed values
                        var newElement = Activator.CreateInstance(elementType);
                        timestampField.SetValue(newElement, 0L);
                        valueField.SetValue(newElement, 0f);
                        bufferArray.SetValue(newElement, i);
                    }
                }
            }

            // Reset TimeSyncScheduler static fields
            var schedulerType = typeof(TimeSyncScheduler);

            // Reset lastSyncTimeRawTicks (field renamed from lastSyncTimeTicks)
            var lastSyncTimeField = schedulerType.GetField("lastSyncTimeRawTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            lastSyncTimeField?.SetValue(null, 0L);

            // CRITICAL: Reset aggressiveModeEndRawTicks to disable aggressive mode between tests
            var aggressiveModeEndField = schedulerType.GetField("aggressiveModeEndRawTicks",
                BindingFlags.NonPublic | BindingFlags.Static);
            aggressiveModeEndField?.SetValue(null, 0L);

            HighPerfTimeSync.ResetForTesting();

            // CRITICAL: Reset SecretaryOfTemporalAffairs statics to prevent stale time baselines
            SecretaryOfTemporalAffairs.ResetStaticsForTesting();
        }

        /// <summary>
        /// Helper method to run an action on a specific thread with proper error handling
        /// </summary>
        protected T RunOnThread<T>(Func<T> func, BlockingCollection<Action> actions, int timeoutMs = 2000)
        {
            if (actions == null || actions.IsAddingCompleted)
                throw new InvalidOperationException("Action collection is not available");

            if (cts == null || cts.IsCancellationRequested)
                throw new OperationCanceledException("Test is being torn down");

            T result = default(T);
            Exception taskException = null;
            ManualResetEventSlim resetEvent = null;

            try
            {
                resetEvent = new ManualResetEventSlim(false);

                actions.Add(() =>
                {
                    try
                    {
                        result = func();
                    }
                    catch (Exception ex)
                    {
                        taskException = ex;
                    }
                    finally
                    {
                        try
                        {
                            resetEvent?.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed, ignore
                        }
                    }
                });
            }
            catch (InvalidOperationException)
            {
                // Collection was completed, test is ending
                try { resetEvent?.Dispose(); } catch { }
                throw new OperationCanceledException("Test is being torn down");
            }

            try
            {
                if (resetEvent != null)
                {
                    bool signaled = false;
                    try
                    {
                        if (cts != null && !cts.IsCancellationRequested)
                        {
                            signaled = resetEvent.Wait(timeoutMs, cts.Token);
                        }
                        else
                        {
                            signaled = resetEvent.Wait(timeoutMs);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Test is being cancelled
                        throw;
                    }

                    if (!signaled)
                    {
                        throw new TimeoutException($"RunOnThread timed out after {timeoutMs}ms");
                    }
                }
            }
            finally
            {
                try { resetEvent?.Dispose(); } catch { }
            }

            if (taskException != null)
                throw taskException;

            return result;
        }

        /// <summary>
        /// Helper method to run an action on a specific thread
        /// </summary>
        protected void RunOnThread(Action action, BlockingCollection<Action> actions, int timeoutMs = 2000)
        {
            RunOnThread<object>(() => { action(); return null; }, actions, timeoutMs);
        }
    }
}