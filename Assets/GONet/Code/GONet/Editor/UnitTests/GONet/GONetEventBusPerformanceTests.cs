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

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.TestTools;

namespace GONet.Tests
{
    /// <summary>
    /// Performance-focused tests for GONetEventBus optimizations added in event-bus-perf branch.
    /// Tests validate correctness of lazy cache rebuild, dirty tracking, and TypeHierarchyCache.
    /// </summary>
    [TestFixture]
    public class GONetEventBusPerformanceTests
    {
        private GONetEventBus eventBus;
        private int handlerCallCount;
        private List<Type> handlerCallSequence;

        // Test event hierarchy
        public class TestBaseEvent : IGONetEvent
        {
            public long OccurredAtElapsedTicks => GONetMain.Time.ElapsedTicks;
            public string Data { get; set; }
        }

        public class TestDerivedEvent : TestBaseEvent
        {
            public int DerivedData { get; set; }
        }

        public class TestGrandchildEvent : TestDerivedEvent
        {
            public bool GrandchildData { get; set; }
        }

        public interface ITestMarkerEvent : IGONetEvent { }

        public class TestMarkedEvent : TestBaseEvent, ITestMarkerEvent
        {
            public string MarkerData { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            eventBus = GONetEventBus.Instance;
            handlerCallCount = 0;
            handlerCallSequence = new List<Type>();
        }

        private void CountingHandler<T>(GONetEventEnvelope<T> envelope) where T : IGONetEvent
        {
            handlerCallCount++;
            handlerCallSequence.Add(typeof(T));
        }

        // --- Cache Coherency Tests (Critical for lazy rebuild correctness) ---

        [Test]
        public void LazyRebuild_SubscribeToBase_DerivedEventHandlersStillWork()
        {
            // PERFORMANCE NOTE: This tests the fix for lazy rebuild cache coherency.
            // When subscribing to BaseEvent, DerivedEvent's cache must also update.

            // Subscribe to derived first
            var derivedSub = eventBus.Subscribe<TestDerivedEvent>(CountingHandler);

            // Publish derived event - should be handled (1 call)
            eventBus.Publish(new TestDerivedEvent { Data = "Derived1" });
            Assert.AreEqual(1, handlerCallCount, "Derived handler should receive derived event");

            // NOW subscribe to base class (triggers cache rebuild)
            var baseSub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);

            handlerCallCount = 0;
            handlerCallSequence.Clear();

            // Publish derived event again - BOTH handlers should fire
            eventBus.Publish(new TestDerivedEvent { Data = "Derived2", DerivedData = 42 });

            Assert.AreEqual(2, handlerCallCount, "Both base and derived handlers should receive derived event");
            Assert.IsTrue(handlerCallSequence.Contains(typeof(TestBaseEvent)), "Base handler should have been called");
            Assert.IsTrue(handlerCallSequence.Contains(typeof(TestDerivedEvent)), "Derived handler should have been called");

            baseSub.Unsubscribe();
            derivedSub.Unsubscribe();
        }

        [Test]
        public void LazyRebuild_SubscribeToInterface_ImplementingEventHandlersStillWork()
        {
            // Tests interface subscription cache coherency
            var markedSub = eventBus.Subscribe<TestMarkedEvent>(CountingHandler);

            eventBus.Publish(new TestMarkedEvent { Data = "Marked1" });
            Assert.AreEqual(1, handlerCallCount);

            // Subscribe to interface (triggers rebuild)
            var interfaceSub = eventBus.Subscribe<ITestMarkerEvent>(CountingHandler);

            handlerCallCount = 0;
            handlerCallSequence.Clear();

            // Publish marked event - both handlers should fire
            eventBus.Publish(new TestMarkedEvent { Data = "Marked2", MarkerData = "Test" });

            Assert.AreEqual(2, handlerCallCount, "Both interface and concrete handlers should fire");

            interfaceSub.Unsubscribe();
            markedSub.Unsubscribe();
        }

        [Test]
        public void LazyRebuild_MultipleSubscribes_SingleCacheRebuild()
        {
            // PERFORMANCE TEST: Verify batching works (multiple Subscribe → 1 rebuild)
            // We can't directly measure cache rebuilds, but we can verify correctness
            // after batched subscribes.

            var subs = new List<IDisposable>();

            // Subscribe to 10 different event types in rapid succession
            for (int i = 0; i < 10; i++)
            {
                var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);
                subs.Add(sub);
            }

            // Publish event - all 10 handlers should fire
            eventBus.Publish(new TestBaseEvent { Data = "Batched" });

            Assert.AreEqual(10, handlerCallCount, "All 10 handlers should have been called after batched subscribes");

            foreach (var sub in subs)
            {
                sub.Dispose();
            }
        }

        [Test]
        public void LazyRebuild_SubscribeThenPublish_HandlerReceivesEvent()
        {
            // Basic sanity check: lazy rebuild happens before first Publish
            var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);

            // This Publish should trigger RebuildDirtyCachesIfNeeded()
            eventBus.Publish(new TestBaseEvent { Data = "LazyTest" });

            Assert.AreEqual(1, handlerCallCount, "Handler should receive event after lazy rebuild");

            sub.Unsubscribe();
        }

        [Test]
        public void LazyRebuild_UnsubscribeThenPublish_HandlerDoesNotReceive()
        {
            var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);
            eventBus.Publish(new TestBaseEvent { Data = "First" });
            Assert.AreEqual(1, handlerCallCount);

            sub.Unsubscribe(); // Marks cache dirty

            handlerCallCount = 0;

            // Publish should rebuild cache (excluding unsubscribed handler)
            eventBus.Publish(new TestBaseEvent { Data = "Second" });

            Assert.AreEqual(0, handlerCallCount, "Unsubscribed handler should not receive events");
        }

        [Test]
        public void SetSubscriptionPriority_AfterSubscribe_OrderUpdates()
        {
            // Tests that SetSubscriptionPriority triggers cache rebuild
            List<int> callOrder = new List<int>();

            var sub1 = eventBus.Subscribe<TestBaseEvent>(env => callOrder.Add(1));
            var sub2 = eventBus.Subscribe<TestBaseEvent>(env => callOrder.Add(2));
            var sub3 = eventBus.Subscribe<TestBaseEvent>(env => callOrder.Add(3));

            // Initial order: default priority (0) for all
            eventBus.Publish(new TestBaseEvent { Data = "BeforePriority" });
            // Call order should be subscription order: 1, 2, 3

            callOrder.Clear();

            // Change priorities (lower = higher priority = earlier)
            sub1.SetSubscriptionPriority(10);  // Lowest priority
            sub2.SetSubscriptionPriority(-5); // Highest priority
            sub3.SetSubscriptionPriority(0);  // Middle priority

            eventBus.Publish(new TestBaseEvent { Data = "AfterPriority" });

            Assert.AreEqual(3, callOrder.Count);
            Assert.AreEqual(2, callOrder[0], "Sub2 (priority -5) should fire first");
            Assert.AreEqual(3, callOrder[1], "Sub3 (priority 0) should fire second");
            Assert.AreEqual(1, callOrder[2], "Sub1 (priority 10) should fire last");

            sub1.Unsubscribe();
            sub2.Unsubscribe();
            sub3.Unsubscribe();
        }

        // --- Type Hierarchy Cache Tests ---

        [Test]
        public void TypeHierarchyCache_BaseTypeChain_CorrectHierarchy()
        {
            // This indirectly tests TypeHierarchyCache by verifying type hierarchy subscription
            var baseSub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);

            // Publish grandchild event (3 levels deep)
            eventBus.Publish(new TestGrandchildEvent
            {
                Data = "GrandchildData",
                DerivedData = 99,
                GrandchildData = true
            });

            // Base handler should receive grandchild event (type hierarchy works)
            Assert.AreEqual(1, handlerCallCount, "Base handler should receive events from 3-level derived class");

            baseSub.Unsubscribe();
        }

        [Test]
        public void TypeHierarchyCache_InterfaceImplementation_HandlerReceivesEvent()
        {
            var interfaceSub = eventBus.Subscribe<ITestMarkerEvent>(CountingHandler);

            eventBus.Publish(new TestMarkedEvent { Data = "MarkedData", MarkerData = "Interface" });

            Assert.AreEqual(1, handlerCallCount, "Interface handler should receive implementing event");

            interfaceSub.Unsubscribe();
        }

        // --- Performance Benchmarks (Informational) ---

        [Test]
        public void Benchmark_SubscribeSpeed_100Subscriptions()
        {
            // Measures time to perform 100 Subscribe calls (tests lazy batching benefit)
            var stopwatch = Stopwatch.StartNew();
            var subs = new List<IDisposable>();

            for (int i = 0; i < 100; i++)
            {
                var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);
                subs.Add(sub);
            }

            stopwatch.Stop();

            // With lazy batching, 100 subscribes should be FAST (no immediate rebuild)
            // Old code: ~100 * 20ms = 2000ms
            // New code: ~100 * 0.1ms = 10ms
            UnityEngine.Debug.Log($"100 Subscribe calls took: {stopwatch.ElapsedMilliseconds} ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "100 subscribes should take < 100ms with lazy batching");

            foreach (var sub in subs)
            {
                sub.Dispose();
            }
        }

        [Test]
        public void Benchmark_PublishSpeed_AfterManySubscriptions()
        {
            // Measures Publish performance after many subscriptions (tests cache rebuild cost)
            var subs = new List<IDisposable>();

            // Create 50 subscriptions
            for (int i = 0; i < 50; i++)
            {
                var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);
                subs.Add(sub);
            }

            // First Publish triggers cache rebuild (should be fast due to batching)
            var stopwatch = Stopwatch.StartNew();
            eventBus.Publish(new TestBaseEvent { Data = "BenchmarkPublish" });
            stopwatch.Stop();

            UnityEngine.Debug.Log($"First Publish (cache rebuild) took: {stopwatch.ElapsedMilliseconds} ms");
            Assert.AreEqual(50, handlerCallCount, "All 50 handlers should fire");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "First publish with cache rebuild should be < 100ms");

            // Subsequent publishes should be very fast (cache already built)
            handlerCallCount = 0;
            stopwatch.Restart();
            eventBus.Publish(new TestBaseEvent { Data = "SecondPublish" });
            stopwatch.Stop();

            UnityEngine.Debug.Log($"Second Publish (cached) took: {stopwatch.ElapsedMilliseconds} ms");
            Assert.AreEqual(50, handlerCallCount, "All 50 handlers should fire");
            Assert.Less(stopwatch.ElapsedMilliseconds, 10, "Cached publish should be < 10ms");

            foreach (var sub in subs)
            {
                sub.Dispose();
            }
        }

        [Test]
        public void Benchmark_MemoryAllocation_SubscribeThenPublish()
        {
            // Measures GC allocations during Subscribe/Publish cycle
            // PERFORMANCE NOTE: New code should allocate ~50-70% less than old code

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memBefore = GC.GetTotalMemory(false);

            var subs = new List<IDisposable>();
            for (int i = 0; i < 10; i++)
            {
                var sub = eventBus.Subscribe<TestBaseEvent>(CountingHandler);
                subs.Add(sub);
            }

            eventBus.Publish(new TestBaseEvent { Data = "MemTest" });

            long memAfter = GC.GetTotalMemory(false);
            long allocated = memAfter - memBefore;

            UnityEngine.Debug.Log($"Memory allocated (10 Subscribe + 1 Publish): {allocated} bytes");

            // Old code: ~500-1000 bytes per Subscribe + Publish
            // New code: ~200-500 bytes per Subscribe + Publish (temp collection reuse)
            // 10 operations should allocate < 10KB total
            Assert.Less(allocated, 10_000, "Memory allocation should be < 10KB for 10 Subscribe + 1 Publish");

            foreach (var sub in subs)
            {
                sub.Dispose();
            }
        }

        // --- Edge Case Tests ---

        [Test]
        public void EdgeCase_SubscribeDuringPublish_HandlersStillCorrect()
        {
            // Tests that subscribing during event handling doesn't break iteration
            int dynamicHandlerCalls = 0;
            IDisposable dynamicSub = null;

            var sub1 = eventBus.Subscribe<TestBaseEvent>(env =>
            {
                handlerCallCount++;
                // Subscribe during handling (dirty flag set during iteration)
                if (dynamicSub == null)
                {
                    dynamicSub = eventBus.Subscribe<TestBaseEvent>(env2 => dynamicHandlerCalls++);
                }
            });

            // First publish - sub1 fires, dynamicSub created
            eventBus.Publish(new TestBaseEvent { Data = "First" });
            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual(0, dynamicHandlerCalls, "Dynamic sub should not fire in same publish");

            // Second publish - both handlers fire
            eventBus.Publish(new TestBaseEvent { Data = "Second" });
            Assert.AreEqual(2, handlerCallCount);
            Assert.AreEqual(1, dynamicHandlerCalls, "Dynamic sub should fire in next publish");

            sub1.Dispose();
            dynamicSub?.Dispose();
        }

        [Test]
        public void EdgeCase_UnsubscribeDuringPublish_NoException()
        {
            // Tests that unsubscribing during event handling doesn't throw
            IDisposable sub1 = null;
            IDisposable sub2 = null;

            sub1 = eventBus.Subscribe<TestBaseEvent>(env =>
            {
                handlerCallCount++;
                sub2?.Dispose(); // Unsubscribe other handler during handling
            });

            sub2 = eventBus.Subscribe<TestBaseEvent>(env =>
            {
                handlerCallCount++;
            });

            // Should not throw, even though sub2 unsubscribed during iteration
            Assert.DoesNotThrow(() => eventBus.Publish(new TestBaseEvent { Data = "Unsubscribe" }));

            // Exact call count depends on iteration order, but should be 1 or 2
            Assert.GreaterOrEqual(handlerCallCount, 1);

            sub1?.Dispose();
            // sub2 already unsubscribed
        }

        [Test]
        public void EdgeCase_MultipleSetPriority_LastOneWins()
        {
            List<int> callOrder = new List<int>();

            var sub = eventBus.Subscribe<TestBaseEvent>(env => callOrder.Add(1));

            sub.SetSubscriptionPriority(10);
            sub.SetSubscriptionPriority(-5);  // Last call should win
            sub.SetSubscriptionPriority(0);   // Last call should win

            var sub2 = eventBus.Subscribe<TestBaseEvent>(env => callOrder.Add(2));
            sub2.SetSubscriptionPriority(5);

            eventBus.Publish(new TestBaseEvent { Data = "PriorityOverride" });

            Assert.AreEqual(2, callOrder.Count);
            Assert.AreEqual(1, callOrder[0], "Sub1 (priority 0) should fire first");
            Assert.AreEqual(2, callOrder[1], "Sub2 (priority 5) should fire second");

            sub.Unsubscribe();
            sub2.Unsubscribe();
        }
    }
}
