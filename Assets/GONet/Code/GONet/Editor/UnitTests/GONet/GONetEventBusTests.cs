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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GONet
{
    [TestFixture]
    public class GONetEventBusTests
    {
        int iGONetEventSubscriptionsFulfilled;
        int iPersistentEventSubscriptionsFulfilled;
        int iTransientEventSubscriptionsFulfilled;
        int instantiateSubscriptionsFulfilled;
        int isRotSubscriptionsFulfilled;
        int syncSubscriptionsFulfilled;

        LinkedList<string> orderedSubscriptions;

        //Only define this preprocessor directive if you have already generated the runtime only scripts.
#if MANUAL_UNIT_TESTING_SYNC_EVENTS
        [Test]
        public void SubscriptionPriorityYieldsProperCallOrderToHandlers()
        {
            orderedSubscriptions = new LinkedList<string>();

            // TODO GONetEventBus.Instance.ResetAll or UnsubscribeAll

            var subscription2 = GONetEventBus.Instance.Subscribe<IPersistentEvent>(OnIPersistentEvent);
            //subscription2.SetSubscriptionPriority();

            var subscription6 = GONetEventBus.Instance.Subscribe<SyncEvent_ValueChangeProcessed>(OnSyncEvent);
            subscription6.SetSubscriptionPriority(1);

            var subscription3 = GONetEventBus.Instance.Subscribe<ITransientEvent>(OnITransientEvent);
            subscription3.SetSubscriptionPriority(-5);

            var subscription4 = GONetEventBus.Instance.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent);
            subscription4.SetSubscriptionPriority(5);

            var subscription1 = GONetEventBus.Instance.Subscribe<IGONetEvent>(OnIGONetEvent);
            subscription1.SetSubscriptionPriority(-10);

            var subscription5 = GONetEventBus.Instance.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_IsRotationSyncd, OnIsRotEvent);
            subscription5.SetSubscriptionPriority(10);


            {
                orderedSubscriptions.Clear();

                IGONetEvent iGONetEvent = new InstantiateGONetParticipantEvent();
                GONetEventBus.Instance.Publish(iGONetEvent);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIPersistentEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnInstantiationEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                IPersistentEvent instantiationEvent_per = new InstantiateGONetParticipantEvent();
                GONetEventBus.Instance.Publish(instantiationEvent_per);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIPersistentEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnInstantiationEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                InstantiateGONetParticipantEvent instantiationEvent = new InstantiateGONetParticipantEvent();
                GONetEventBus.Instance.Publish(instantiationEvent);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIPersistentEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnInstantiationEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                ITransientEvent rotEvent_i = new SyncEvent_GONetParticipant_IsRotationSyncd();
                GONetEventBus.Instance.Publish(rotEvent_i);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIsRotEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                SyncEvent_ValueChangeProcessed rotEvent_base = new SyncEvent_GONetParticipant_IsRotationSyncd();
                GONetEventBus.Instance.Publish(rotEvent_base);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIsRotEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                SyncEvent_GONetParticipant_IsRotationSyncd rotEvent = new SyncEvent_GONetParticipant_IsRotationSyncd();
                GONetEventBus.Instance.Publish(rotEvent);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIsRotEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                ITransientEvent posEvent_i = new SyncEvent_GONetParticipant_IsPositionSyncd();
                GONetEventBus.Instance.Publish(posEvent_i);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                SyncEvent_ValueChangeProcessed posEvent_base = new SyncEvent_GONetParticipant_IsPositionSyncd();
                GONetEventBus.Instance.Publish(posEvent_base);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            {
                orderedSubscriptions.Clear();

                SyncEvent_GONetParticipant_IsPositionSyncd posEvent = new SyncEvent_GONetParticipant_IsPositionSyncd();
                GONetEventBus.Instance.Publish(posEvent);

                using (var enumerator = orderedSubscriptions.GetEnumerator())
                {
                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnIGONetEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnITransientEvent), enumerator.Current);

                    enumerator.MoveNext();
                    Assert.AreEqual(nameof(OnSyncEvent), enumerator.Current);

                    Assert.IsFalse(enumerator.MoveNext());
                }
            }

            subscription1.Unsubscribe();
            subscription2.Unsubscribe();
            subscription3.Unsubscribe();
            subscription4.Unsubscribe();
            subscription5.Unsubscribe();
            subscription6.Unsubscribe();
        }

        [Test]
        public void SubscriptionsAccountForGenericsAndInterfacesProperly()
        {
            orderedSubscriptions = new LinkedList<string>();

            iGONetEventSubscriptionsFulfilled = 0;
            iPersistentEventSubscriptionsFulfilled = 0;
            iTransientEventSubscriptionsFulfilled = 0;
            instantiateSubscriptionsFulfilled = 0;
            isRotSubscriptionsFulfilled = 0;
            syncSubscriptionsFulfilled = 0;

            // TODO GONetEventBus.Instance.ResetAll or UnsubscribeAll

            var s1 = GONetEventBus.Instance.Subscribe<IGONetEvent>(OnIGONetEvent);
            var s2 = GONetEventBus.Instance.Subscribe<IPersistentEvent>(OnIPersistentEvent);
            var s3 = GONetEventBus.Instance.Subscribe<ITransientEvent>(OnITransientEvent);
            var s4 = GONetEventBus.Instance.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent);
            var s5 = GONetEventBus.Instance.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_IsRotationSyncd, OnIsRotEvent);
            var s6 = GONetEventBus.Instance.Subscribe<SyncEvent_ValueChangeProcessed>(OnSyncEvent);

            IGONetEvent iGONetEvent = new InstantiateGONetParticipantEvent();
            GONetEventBus.Instance.Publish(iGONetEvent); // +1 persistent, +1 iGONet, +1 instantiation

            IPersistentEvent instantiationEvent_per = new InstantiateGONetParticipantEvent();
            GONetEventBus.Instance.Publish(instantiationEvent_per); // +1 persistent, +1 iGONet, +1 instantiation

            InstantiateGONetParticipantEvent instantiationEvent = new InstantiateGONetParticipantEvent();
            GONetEventBus.Instance.Publish(instantiationEvent); // +1 persistent, +1 iGONet, +1 instantiation

            ITransientEvent rotEvent_i = new SyncEvent_GONetParticipant_IsRotationSyncd();
            GONetEventBus.Instance.Publish(rotEvent_i); // +1 transient, +1 iGONet, +1 isRot, +1 sync

            SyncEvent_ValueChangeProcessed rotEvent_base = new SyncEvent_GONetParticipant_IsRotationSyncd();
            GONetEventBus.Instance.Publish(rotEvent_base); // +1 transient, +1 iGONet, +1 iRot, +1 sync

            SyncEvent_GONetParticipant_IsRotationSyncd rotEvent = new SyncEvent_GONetParticipant_IsRotationSyncd();
            GONetEventBus.Instance.Publish(rotEvent); // +1 transient, +1 iGONet, +1 isRot, +1 sync

            ITransientEvent posEvent_i = new SyncEvent_GONetParticipant_IsPositionSyncd();
            GONetEventBus.Instance.Publish(posEvent_i); // +1 transient, +1 iGONet, +1 sync

            SyncEvent_ValueChangeProcessed posEvent_base = new SyncEvent_GONetParticipant_IsPositionSyncd();
            GONetEventBus.Instance.Publish(posEvent_base); // +1 transient, +1 iGONet, +1 sync

            SyncEvent_GONetParticipant_IsPositionSyncd posEvent = new SyncEvent_GONetParticipant_IsPositionSyncd();
            GONetEventBus.Instance.Publish(posEvent); // +1 transient, +1 iGONet, +1 sync

            Assert.AreEqual(3, isRotSubscriptionsFulfilled);
            Assert.AreEqual(3, instantiateSubscriptionsFulfilled);
            Assert.AreEqual(9, iGONetEventSubscriptionsFulfilled);
            Assert.AreEqual(6, iTransientEventSubscriptionsFulfilled);
            Assert.AreEqual(3, iPersistentEventSubscriptionsFulfilled);
            Assert.AreEqual(6, syncSubscriptionsFulfilled);

            s1.Unsubscribe();
            s2.Unsubscribe();
            s3.Unsubscribe();
            s4.Unsubscribe();
            s5.Unsubscribe();
            s6.Unsubscribe();
        }
#endif

        [Test]
        public void PublishCallDepth()
        {
            // TODO GONetEventBus.Instance.ResetAll or UnsubscribeAll

            var s1 = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(OnGNPDisabled);
            var s2 = GONetMain.EventBus.Subscribe<DestroyGONetParticipantEvent>(OnDestroyGNP);

            IGONetEvent ev = new DestroyGONetParticipantEvent();
            Assert.AreEqual(0, GONetMain.EventBus.Publish(ev));

            s1.Unsubscribe();
            s2.Unsubscribe();
        }

        // Including this causes the event generation stuff to crash...somehow need to add to an exclude from generation list.....likely by checking if editor or not..namespace?
        public class TestEvent : IGONetEvent
        {
            long IGONetEvent.OccurredAtElapsedTicks => 0;
        }

        [Test]
        public void HandlersAreCalledInOrderOfPriority()
        {
            // Arrange
            TestEvent testEvent = new TestEvent();
            List<int> handlerOrder = new List<int>();

            void Handler1(GONetEventEnvelope<TestEvent> eventEnvelope) => handlerOrder.Add(1);
            void Handler2(GONetEventEnvelope<TestEvent> eventEnvelope) => handlerOrder.Add(2);
            void Handler3(GONetEventEnvelope<TestEvent> eventEnvelope) => handlerOrder.Add(3);

            var subscription1 = GONetMain.EventBus.Subscribe<TestEvent>(Handler1);
            var subscription2 = GONetMain.EventBus.Subscribe<TestEvent>(Handler2);
            var subscription3 = GONetMain.EventBus.Subscribe<TestEvent>(Handler3);

            subscription1.SetSubscriptionPriority(3);
            subscription2.SetSubscriptionPriority(1);
            subscription3.SetSubscriptionPriority(2);

            // Act
            GONetMain.EventBus.Publish(testEvent);

            // Assert
            Assert.AreEqual(3, handlerOrder.Count);
            Assert.AreEqual(2, handlerOrder[0]);
            Assert.AreEqual(3, handlerOrder[1]);
            Assert.AreEqual(1, handlerOrder[2]);
        }

        private void OnDestroyGNP(GONetEventEnvelope<DestroyGONetParticipantEvent> eventEnvelope)
        {
            Assert.AreEqual(0, GONetMain.EventBus.Publish(new GONetParticipantDisabledEvent()));
        }

        private void OnGNPDisabled(GONetEventEnvelope<GONetParticipantDisabledEvent> eventEnvelope)
        {
            //throw new NotImplementedException();
        }

        private void OnSyncEvent(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ++syncSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnSyncEvent));
        }

        private void OnIsRotEvent(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ++isRotSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnIsRotEvent));
        }

        private void OnInstantiationEvent(GONetEventEnvelope<InstantiateGONetParticipantEvent> eventEnvelope)
        {
            ++instantiateSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnInstantiationEvent));
        }

        private void OnITransientEvent(GONetEventEnvelope<ITransientEvent> eventEnvelope)
        {
            ++iTransientEventSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnITransientEvent));
        }

        private void OnIPersistentEvent(GONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            ++iPersistentEventSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnIPersistentEvent));
        }

        private void OnIGONetEvent(GONetEventEnvelope<IGONetEvent> eventEnvelope)
        {
            ++iGONetEventSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnIGONetEvent));
        }
    }
}