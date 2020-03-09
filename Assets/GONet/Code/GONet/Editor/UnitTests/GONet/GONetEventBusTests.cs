using NUnit.Framework;
using System;
using System.Collections.Generic;
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

            var subscription5 = GONetEventBus.Instance.Subscribe<SyncEvent_GONetParticipant_IsRotationSyncd>(OnIsRotEvent);
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
        }

        [Test]
        public void SubscriptionsAccountForGenericsAndInterfacesProperly()
        {
            iGONetEventSubscriptionsFulfilled = 0;
            iPersistentEventSubscriptionsFulfilled = 0;
            iTransientEventSubscriptionsFulfilled = 0;
            instantiateSubscriptionsFulfilled = 0;
            isRotSubscriptionsFulfilled = 0;
            syncSubscriptionsFulfilled = 0;

            // TODO GONetEventBus.Instance.ResetAll or UnsubscribeAll

            GONetEventBus.Instance.Subscribe<IGONetEvent>(OnIGONetEvent);
            GONetEventBus.Instance.Subscribe<IPersistentEvent>(OnIPersistentEvent);
            GONetEventBus.Instance.Subscribe<ITransientEvent>(OnITransientEvent);
            GONetEventBus.Instance.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent);
            GONetEventBus.Instance.Subscribe<SyncEvent_GONetParticipant_IsRotationSyncd>(OnIsRotEvent);
            GONetEventBus.Instance.Subscribe<SyncEvent_ValueChangeProcessed>(OnSyncEvent);
            
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
        }

        private void OnSyncEvent(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ++syncSubscriptionsFulfilled;

            orderedSubscriptions.AddLast(nameof(OnSyncEvent));
        }

        private void OnIsRotEvent(GONetEventEnvelope<SyncEvent_GONetParticipant_IsRotationSyncd> eventEnvelope)
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
