using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine; // Assuming UnityEngine stubs or mocks if running outside Unity

namespace GONet.Tests
{
    // --- Mock Event Types ---

    [MemoryPack.MemoryPackable]
    public partial class BaseEvent : GONetEventBusHolisticTests.IBaseInterfaceEvent
    {
        public virtual long OccurredAtElapsedTicks => GONetMain.Time.ElapsedTicks; // Mock time
        public string BaseData { get; set; }
    }

    [MemoryPack.MemoryPackable]
    public partial class DerivedPersistentEvent : BaseEvent, IPersistentEvent
    {
        public string PersistentData { get; set; }
    }

    [MemoryPack.MemoryPackable]
    public partial class DerivedTransientEvent : BaseEvent, ITransientEvent
    {
        public string TransientData { get; set; }
    }

    [MemoryPack.MemoryPackable]
    public partial class LocalOnlyEvent : BaseEvent, ITransientEvent, ILocalOnlyPublish
    {
        public string LocalData { get; set; }
    }

    [MemoryPack.MemoryPackable]
    public partial class AnotherTransientEvent : ITransientEvent
    {
        public long OccurredAtElapsedTicks => GONetMain.Time.ElapsedTicks;
        public int Value { get; set; }
    }

    // Mock for SyncEvent_GeneratedTypes (replace with actual if available)
    // public enum Mock_SyncEvent_GeneratedTypes { MockSyncEvent1 }
    // [MemoryPack.MemoryPackable]
    // public partial class MockSyncEvent1 : SyncEvent_ValueChangeProcessed { /* ... implement abstract members ... */ }


    [TestFixture]
    public class GONetEventBusHolisticTests
    {
        private GONetEventBus eventBus;
        private int handlerCallCount;
        private string lastReceivedData;
        private List<string> callOrder;

        // Mock GONetMain static properties/methods if needed, or assume defaults.
        // For simplicity here, we'll assume GONetMain.MyAuthorityId is 1.
        // Need to handle GONetMain.Time potentially.

        [SetUp]
        public void Setup()
        {
            // Ideally, we'd reset the singleton instance or use a fresh one per test.
            // Since it's a static singleton, this is tricky without modification.
            // We'll rely on unsubscribing carefully in each test.
            eventBus = GONetEventBus.Instance;
            handlerCallCount = 0;
            lastReceivedData = null;
            callOrder = new List<string>();
            // NOTE: UnsubscribeAll is not available, tests might interfere if not cleaned up properly.
        }

        // --- Test Helper Handlers ---
        private void BasicHandler<T>(GONetEventEnvelope<T> envelope) where T : IGONetEvent
        {
            handlerCallCount++;
            // Could add asserts here about envelope properties if needed
            if (envelope.Event is BaseEvent baseEvent) lastReceivedData = baseEvent.BaseData;
            if (envelope.Event is AnotherTransientEvent another) lastReceivedData = another.Value.ToString();
        }

        private void OrderHandler(string id, GONetEventEnvelope<BaseEvent> envelope)
        {
            callOrder.Add(id);
        }

        private bool FilterHandler<T>(GONetEventEnvelope<T> envelope) where T : IGONetEvent
        {
            // Example Filter: Only handle if BaseData is "HandleMe"
            return envelope.Event is BaseEvent baseEvent && baseEvent.BaseData == "HandleMe";
        }

        private void ExceptionHandler<T>(GONetEventEnvelope<T> envelope) where T : IGONetEvent
        {
            handlerCallCount++;
            throw new Exception("Test Exception from Handler");
        }


        // --- Test Cases ---

        [Test]
        public void Publish_SingleSubscriber_HandlerCalled()
        {
            var sub = eventBus.Subscribe<BaseEvent>(BasicHandler);
            var ev = new BaseEvent { BaseData = "TestData" };

            eventBus.Publish(ev);

            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("TestData", lastReceivedData);
            sub.Unsubscribe();
        }

        [Test]
        public void Publish_MultipleSubscribers_AllHandlersCalled()
        {
            var sub1 = eventBus.Subscribe<BaseEvent>(BasicHandler);
            var sub2 = eventBus.Subscribe<BaseEvent>(BasicHandler); // Same handler, just count calls

            eventBus.Publish(new BaseEvent { BaseData = "MultiSub" });

            Assert.AreEqual(2, handlerCallCount);
            sub1.Unsubscribe();
            sub2.Unsubscribe();
        }

        [Test]
        public void Publish_NoSubscribers_DoesNotThrow()
        {
            var ev = new AnotherTransientEvent { Value = 99 };
            Assert.DoesNotThrow(() => eventBus.Publish(ev));
            Assert.AreEqual(0, handlerCallCount);
        }

        [Test]
        public void Subscribe_Interface_ReceivesDerivedEvents()
        {
            var sub = eventBus.Subscribe<IBaseInterfaceEvent>(BasicHandler); // Assuming BaseEvent implements IBaseInterfaceEvent
            var ev = new DerivedPersistentEvent { BaseData = "DerivedData", PersistentData = "Persist" };

            eventBus.Publish<IBaseInterfaceEvent>(ev); // Publish as interface

            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("DerivedData", lastReceivedData); // Check data from BaseEvent
            sub.Unsubscribe();
        }


        [Test]
        public void Subscribe_BaseClass_ReceivesDerivedEvents()
        {
            var sub = eventBus.Subscribe<BaseEvent>(BasicHandler);
            var ev = new DerivedPersistentEvent { BaseData = "DerivedData", PersistentData = "Persist" };

            eventBus.Publish(ev);

            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("DerivedData", lastReceivedData);
            sub.Unsubscribe();
        }

        [Test]
        public void Subscribe_DerivedClass_DoesNotReceiveBaseEvents()
        {
            var sub = eventBus.Subscribe<DerivedPersistentEvent>(BasicHandler);
            var ev = new BaseEvent { BaseData = "BaseOnly" };

            eventBus.Publish(ev);

            Assert.AreEqual(0, handlerCallCount); // Derived handler should not be called
            sub.Unsubscribe();
        }

        [Test]
        public void Subscribe_WithFilter_OnlyMatchingEventsHandled()
        {
            var sub = eventBus.Subscribe<BaseEvent>(BasicHandler, FilterHandler);

            // This one should NOT be handled
            eventBus.Publish(new BaseEvent { BaseData = "IgnoreMe" });
            Assert.AreEqual(0, handlerCallCount);

            // This one SHOULD be handled
            eventBus.Publish(new BaseEvent { BaseData = "HandleMe" });
            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("HandleMe", lastReceivedData);

            sub.Unsubscribe();
        }

        [Test]
        public void Unsubscribe_StopsReceivingEvents()
        {
            var sub = eventBus.Subscribe<BaseEvent>(BasicHandler);

            eventBus.Publish(new BaseEvent { BaseData = "First" });
            Assert.AreEqual(1, handlerCallCount);

            sub.Unsubscribe();

            eventBus.Publish(new BaseEvent { BaseData = "Second" });
            Assert.AreEqual(1, handlerCallCount); // Count should not increase
        }

        [Test]
        public void SubscriptionDispose_StopsReceivingEvents()
        {
            var sub = eventBus.Subscribe<BaseEvent>(BasicHandler);

            eventBus.Publish(new BaseEvent { BaseData = "First" });
            Assert.AreEqual(1, handlerCallCount);

            sub.Dispose(); // Use Dispose instead of Unsubscribe

            eventBus.Publish(new BaseEvent { BaseData = "Second" });
            Assert.AreEqual(1, handlerCallCount); // Count should not increase
        }

        [Test]
        public void Publish_LocalOnlyEvent_NotRelayed()
        {
            // This test primarily checks that the local handler is called.
            // Verifying non-relay requires mocking GONetMain or integration testing.
            var sub = eventBus.Subscribe<LocalOnlyEvent>(BasicHandler);
            var ev = new LocalOnlyEvent { BaseData = "LocalData", LocalData = "OnlyHere" };

            eventBus.Publish(ev);

            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("LocalData", lastReceivedData);
            sub.Unsubscribe();
        }

        // Basic test for ASAP queue - doesn't involve real threads
        [Test]
        public void PublishASAP_QueuesEvent_PublishedOnMainThread()
        {
            var sub = eventBus.Subscribe<AnotherTransientEvent>(BasicHandler);
            var ev = new AnotherTransientEvent { Value = 123 };

            // Simulate publish from another thread
            eventBus.PublishASAP(ev);

            // Handler should NOT have been called yet
            Assert.AreEqual(0, handlerCallCount);

            // Simulate main thread update processing the queue
            eventBus.PublishQueuedEventsForMainThread();

            // Handler SHOULD now have been called
            Assert.AreEqual(1, handlerCallCount);
            Assert.AreEqual("123", lastReceivedData);

            sub.Unsubscribe();
        }

        [Test]
        public void Publish_HandlerThrowsException_OtherHandlersExecute()
        {
            var sub1 = eventBus.Subscribe<BaseEvent>(ExceptionHandler);
            var sub2 = eventBus.Subscribe<BaseEvent>(BasicHandler); // Should still run

            var ev = new BaseEvent { BaseData = "ExceptionTest" };
            int exceptionCount = eventBus.Publish(ev);

            Assert.AreEqual(1, exceptionCount, "Publish should report 1 exception.");
            Assert.AreEqual(2, handlerCallCount, "Both handlers should have been attempted.");
            Assert.AreEqual("ExceptionTest", lastReceivedData, "Second handler should have updated data.");

            sub1.Unsubscribe();
            sub2.Unsubscribe();
        }

        [Test]
        public void SubscriptionPriority_HandlersCalledInOrder()
        {
            // Setup handlers that record their call order
            var sub1 = eventBus.Subscribe<BaseEvent>(env => OrderHandler("Handler_P10", env));
            var sub2 = eventBus.Subscribe<BaseEvent>(env => OrderHandler("Handler_P-5", env));
            var sub3 = eventBus.Subscribe<BaseEvent>(env => OrderHandler("Handler_P0", env));

            // Set priorities (Lower value = higher priority = runs earlier)
            sub1.SetSubscriptionPriority(10);
            sub2.SetSubscriptionPriority(-5);
            sub3.SetSubscriptionPriority(0);

            eventBus.Publish(new BaseEvent { BaseData = "PriorityTest" });

            Assert.AreEqual(3, callOrder.Count);
            Assert.AreEqual("Handler_P-5", callOrder[0]);
            Assert.AreEqual("Handler_P0", callOrder[1]);
            Assert.AreEqual("Handler_P10", callOrder[2]);

            sub1.Unsubscribe();
            sub2.Unsubscribe();
            sub3.Unsubscribe();
        }

        // --- Helper Interfaces/Structs for Testing ---
        public interface IBaseInterfaceEvent : IGONetEvent { }
        // Ensure BaseEvent implements IBaseInterfaceEvent if using the interface test
    }
}