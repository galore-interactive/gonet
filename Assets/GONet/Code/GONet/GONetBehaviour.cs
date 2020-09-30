/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
using System.Collections;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// </summary>
    public abstract class GONetBehaviour : MonoBehaviour
    {
        private Subscription<GONetParticipantEnabledEvent> gonetSubscriptionEnabled;
        private Subscription<GONetParticipantStartedEvent> gonetSubscriptionStarted;
        private Subscription<GONetParticipantDisabledEvent> gonetSubscriptionDisabled;
        private Subscription<SyncEvent_GONetParticipant_OwnerAuthorityId> gonetSubscriptionOwnerAuthorityId;

        protected virtual void Awake()
        {
            gonetSubscriptionEnabled = GONetMain.EventBus.Subscribe<GONetParticipantEnabledEvent>(envelope => OnGONetParticipantEnabled(envelope.GONetParticipant));
            gonetSubscriptionStarted = GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(envelope => OnGONetParticipantStarted(envelope.GONetParticipant));
            gonetSubscriptionDisabled = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(envelope => OnGONetParticipantDisabled(envelope.GONetParticipant));
            gonetSubscriptionOwnerAuthorityId = GONetMain.EventBus.Subscribe<SyncEvent_GONetParticipant_OwnerAuthorityId>(envelope => OnGONetParticipant_OwnerAuthorityIdChanged(envelope.GONetParticipant, envelope.Event.GONetId, envelope.Event.valuePrevious, envelope.Event.valueNew));
        }

        private void OnGONetParticipant_OwnerAuthorityIdChanged(GONetParticipant gonetParticipant, uint gonetId, ushort valuePrevious, ushort valueNew)
        {
            if ((object)gonetParticipant == null)
            {
                gonetParticipant = GONetMain.DeriveGNPFromCurrentAndPreviousValues(gonetId, valuePrevious, valueNew);
            }

            bool isSetToValidValue = gonetParticipant.gonetId_raw != GONetParticipant.GONetId_Unset && valueNew != GONetMain.OwnerAuthorityId_Unset;
            if (isSetToValidValue)
            {
                OnGONetParticipant_OwnerAuthorityIdSet(gonetParticipant);
            }
        }

        protected virtual void Start()
        {
            StartCoroutine(WaitThenTriggerClientVsServerStatusKnown());
        }

        private IEnumerator WaitThenTriggerClientVsServerStatusKnown()
        {
            while (!GONetMain.IsClientVsServerStatusKnown || GONetMain.MyAuthorityId == GONetMain.OwnerAuthorityId_Unset)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.1f);
            OnGONetClientVsServerStatusKnown(GONetMain.IsClient, GONetMain.IsServer, GONetMain.MyAuthorityId);
        }

        protected virtual void OnDestroy()
        {
            gonetSubscriptionEnabled.Unsubscribe();
            gonetSubscriptionStarted.Unsubscribe();
            gonetSubscriptionDisabled.Unsubscribe();
            gonetSubscriptionOwnerAuthorityId.Unsubscribe();
        }

        /// <summary>
        /// <para>When this is called, GONet knows whether or not this machine is going to be a GONet client or server.  So any action that must know that first is now OK to execute.</para>
        /// <para>Futhermore, GONet has also assigned an authority id for this machine (i.e., <see cref="GONetMain.MyAuthorityId"/> is set) and that is important as well before doing certain things like instantiating/spawning prefabs with <see cref="GONetParticipant"/> attached.</para>
        /// <para>So, please after this is called, feel free to instantiate/spawn networked GameObjects (i.e., with <see cref="GONetParticipant"/>) into the scene.</para>
        /// </summary>
        /// <param name="isClient">The value of <see cref="GONetMain.IsClient"/> at the time of calling this method.</param>
        /// <param name="isServer">The value of <see cref="GONetMain.IsServer"/> at the time of calling this method.</param>
        /// <param name="myAuthorityId">The value of <see cref="GONetMain.MyAuthorityId"/> at the time of calling this method, which is guaranteed to be valid and not <see cref="GONetMain.OwnerAuthorityId_Unset"/></param>
        public virtual void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId) { }

        public virtual void OnGONetParticipantEnabled(GONetParticipant gonetParticipant) { }

        public virtual void OnGONetParticipantStarted(GONetParticipant gonetParticipant) { }

        public virtual void OnGONetParticipantDisabled(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// Since there is some order of operations differences between machines who instantiate a new <see cref="GONetParticipant"/> and others in regards to 
        /// at what point the <see cref="GONetParticipant.OwnerAuthorityId"/> is set AND one of those differences is the value not being set at the point of 
        /// the call to <see cref="OnGONetParticipantStarted(GONetParticipant)"/>, this method exists to have a callback.
        /// </summary>
        /// <param name="gonetParticipant"></param>
        public virtual void OnGONetParticipant_OwnerAuthorityIdSet(GONetParticipant gonetParticipant) { }
    }

    /// <summary>
    /// <para>
    /// For <see cref="GameObject"/>s that have a <see cref="GONet.GONetParticipant"/> "installed" on them, the other <see cref="MonoBehaviour"/>s also "installed" can 
    /// optionally extend this class to automatically have a reference to the <see cref="GONetParticipant"/> instance to reference it when making decisions
    /// on what to execute.  The most common example is to use <see cref="GONetParticipant.IsMine"/> to know whether or not to execute some game logic or not so that
    /// the logic is only executed on the owner's machine and the networking will handle the rest so the other machines will see the results of the game logic being
    /// executed by "the owner."
    /// </para>
    /// <para>
    /// It is also important to know that a <see cref="MonoBehaviour"/> that extends this class can be "installed" on a child <see cref="GameObject"/> of where the 
    /// <see cref="GONetParticipant"/> is "installed" and this class will look up to the parent to find the nearest "installed" <see cref="GONetParticipant"/> and 
    /// reference that.  This is helpful when some game logic is present in children that is relevant to networking stuffs and you want to keep it that way.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public abstract class GONetParticipantCompanionBehaviour : GONetBehaviour
    {
        public GONetParticipant GONetParticipant => gonetParticipant;

        protected GONetParticipant gonetParticipant;

        protected override void Awake()
        {
            base.Awake();

            Transform xform = transform;

            while (gonetParticipant == null && xform != null)
            {
                gonetParticipant = xform.gameObject.GetComponent<GONetParticipant>();
                xform = xform.parent;
            }
        }

        public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantEnabled(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantEnabled();
            }
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantStarted();
            }
        }

        public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantDisabled(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantDisabled();
            }
        }

        public override void OnGONetParticipant_OwnerAuthorityIdSet(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipant_OwnerAuthorityIdSet(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipant_OwnerAuthorityIdSet();
            }
        }

        public virtual void OnGONetParticipantEnabled() { }

        public virtual void OnGONetParticipantStarted() { }

        public virtual void OnGONetParticipantDisabled() { }

        public virtual void OnGONetParticipant_OwnerAuthorityIdSet() { }
    }
}
