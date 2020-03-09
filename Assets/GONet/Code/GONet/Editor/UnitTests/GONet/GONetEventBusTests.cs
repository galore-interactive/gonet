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

        [Test]
        public void SubscriptionsAccountForGenericsAndInterfacesProperly()
        {
            iGONetEventSubscriptionsFulfilled = 0;
            iPersistentEventSubscriptionsFulfilled = 0;
            iTransientEventSubscriptionsFulfilled = 0;
            instantiateSubscriptionsFulfilled = 0;
            isRotSubscriptionsFulfilled = 0;
            syncSubscriptionsFulfilled = 0;

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
            Assert.AreEqual(6, syncSubscriptionsFulfilled); // TODO FIXME this one fail with 3 instead of 6
        }

        private void OnSyncEvent(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ++syncSubscriptionsFulfilled;
        }

        private void OnIsRotEvent(GONetEventEnvelope<SyncEvent_GONetParticipant_IsRotationSyncd> eventEnvelope)
        {
            ++isRotSubscriptionsFulfilled;
        }

        private void OnInstantiationEvent(GONetEventEnvelope<InstantiateGONetParticipantEvent> eventEnvelope)
        {
            ++instantiateSubscriptionsFulfilled;
        }

        private void OnITransientEvent(GONetEventEnvelope<ITransientEvent> eventEnvelope)
        {
            ++iTransientEventSubscriptionsFulfilled;
        }

        private void OnIPersistentEvent(GONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            ++iPersistentEventSubscriptionsFulfilled;
        }

        private void OnIGONetEvent(GONetEventEnvelope<IGONetEvent> eventEnvelope)
        {
            ++iGONetEventSubscriptionsFulfilled;
        }
    }
}
