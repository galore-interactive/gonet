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
using System.Collections;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// </summary>
    public abstract class GONetBehaviour : MonoBehaviour
    {
        [Tooltip("GONet will send a 'tick' at each of the unique synchronization schedules as defined in various profiles (i.e., GONet => GONet Editor Support => Create New Sync Settings Profile) used when syncing values.")]
        [SerializeField] private bool isTickReceiver;
        public bool IsTickReceiver
        {
            get => isTickReceiver;
            set
            {
                isTickReceiver = value;
                if (isTickReceiver)
                {
                    GONetMain.AddTickReceiver(this);
                }
            }
        }

        private Subscription<GONetParticipantEnabledEvent> gonetSubscriptionEnabled;
        private Subscription<GONetParticipantStartedEvent> gonetSubscriptionStarted;
        private Subscription<GONetParticipantDisabledEvent> gonetSubscriptionDisabled;
        private Subscription<GONetParticipantDeserializeInitAllCompletedEvent> gonetSubscriptionDeserializeInitAllCompleted;
        private Subscription<SyncEvent_ValueChangeProcessed> gonetSubscriptionOwnerAuthorityId;
        

        /// <summary>
        /// IMPORTANT: Keep in mind this is not going to be a good/final value until <see cref="OnGONetClientVsServerStatusKnown(bool, bool, ushort)"/> is called, which is also when <see cref="GONetMain.IsClientVsServerStatusKnown"/> turns true.
        /// </summary>
        public bool IsServer => GONetMain.IsServer;

        /// <summary>
        /// IMPORTANT: Keep in mind this is not going to be a good/final value until <see cref="OnGONetClientVsServerStatusKnown(bool, bool, ushort)"/> is called, which is also when <see cref="GONetMain.IsClientVsServerStatusKnown"/> turns true.
        /// </summary>
        public bool IsClient => GONetMain.IsClient;

        /// <summary>
        /// Since this is a vital feature of GONet, it is conveniently placed here to avoid having to type "GONetMain." each time when in a child class.
        /// </summary>
        public GONetEventBus EventBus => GONetMain.EventBus;

        protected virtual void Awake()
        {
            gonetSubscriptionEnabled = GONetMain.EventBus.Subscribe<GONetParticipantEnabledEvent>(envelope => OnGONetParticipantEnabled(envelope.GONetParticipant));
            gonetSubscriptionStarted = GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(envelope => OnGONetParticipantStarted(envelope.GONetParticipant));
            gonetSubscriptionDisabled = GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(envelope => OnGONetParticipantDisabled(envelope.GONetParticipant));
            gonetSubscriptionDeserializeInitAllCompleted = GONetMain.EventBus.Subscribe<GONetParticipantDeserializeInitAllCompletedEvent>(envelope => OnGONetParticipantDeserializeInitAllCompleted(envelope.GONetParticipant));
            gonetSubscriptionOwnerAuthorityId = GONetMain.EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_OwnerAuthorityId, envelope => OnGONetParticipant_OwnerAuthorityIdChanged(envelope.GONetParticipant, envelope.Event.GONetId, envelope.Event.ValuePrevious.System_UInt16, envelope.Event.ValueNew.System_UInt16));
        }

        protected virtual void OnEnable()
        {
            if (IsTickReceiver)
            {
                GONetMain.AddTickReceiver(this);
            }
        }

        protected virtual void OnDisable()
        {
            GONetMain.RemoveTickReceiver(this);
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
            yield return new WaitForSecondsRealtime(0.1f); // TODO this magic number to wait is bogus and not sure fire....we need to wait only the exact amount of "time" required and no more/no less
            OnGONetClientVsServerStatusKnown(GONetMain.IsClient, GONetMain.IsServer, GONetMain.MyAuthorityId);
        }

        protected virtual void OnDestroy()
        {
            gonetSubscriptionEnabled.Unsubscribe();
            gonetSubscriptionStarted.Unsubscribe();
            gonetSubscriptionDisabled.Unsubscribe();
            gonetSubscriptionDeserializeInitAllCompleted.Unsubscribe();
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

        /// <summary>
        /// IMPORTANT: When this is called, this is the first time it is certain that the 
        ///            <see cref="GONetParticipant.GONetId"/> value is fully assigned!
        /// </summary>
        public virtual void OnGONetParticipantStarted(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <paramref name="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public virtual void OnGONetParticipantDeserializeInitAllCompleted(GONetParticipant gonetParticipant) { }

        public virtual void OnGONetParticipantDisabled(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// Since there is some order of operations differences between machines who instantiate a new <see cref="GONetParticipant"/> and others in regards to 
        /// at what point the <see cref="GONetParticipant.OwnerAuthorityId"/> is set AND one of those differences is the value not being set at the point of 
        /// the call to <see cref="OnGONetParticipantStarted(GONetParticipant)"/>, this method exists to have a callback.
        /// </summary>
        /// <param name="gonetParticipant"></param>
        public virtual void OnGONetParticipant_OwnerAuthorityIdSet(GONetParticipant gonetParticipant) { }

        /// <param name="uniqueTickHz">how many times a second this unique frequency is called at...there are many possibilities since each GONet sync settings profile can have its frequency set to a different value</param>
        /// <param name="elapsedSeconds"></param>
        /// <param name="deltaTime">seconds passed since last call to this</param>
        internal virtual void Tick(short uniqueTickHz, double elapsedSeconds, double deltaTime) { }
    }

    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// NOTE: This is a convenience class named for Photon PUN users as they might be used to using MonoBehaviourPunCallbacks, but this is the same as <see cref="GONetBehaviour"/>.
    /// </summary>
    public abstract class MonoBehaviourGONetCallbacks : GONetBehaviour { }

    /// <summary>
    /// NOTE: This is a convenience class named with the "MonoBehaviour" prefix in case it helps identifying this class as a possible one to use.
    ///       This is the same as <see cref="GONetParticipantCompanionBehaviour"/> and you can read the class documentation there to know how to use.
    /// </summary>
    public abstract class MonoBehaviourGONetParticipantCompanion : GONetParticipantCompanionBehaviour { }

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
        public bool IsMine => gonetParticipant.IsMine;
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

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <paramref name="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public override void OnGONetParticipantDeserializeInitAllCompleted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantDeserializeInitAllCompleted(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantDeserializeInitAllCompleted();
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

        /// <summary>
        /// IMPORTANT: When this is called, this is the first time it is certain that the 
        ///            <see cref="GONetParticipant.GONetId"/> value is fully assigned!
        /// </summary>
        public virtual void OnGONetParticipantStarted() { }

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <see cref="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public virtual void OnGONetParticipantDeserializeInitAllCompleted() { }

        public virtual void OnGONetParticipantDisabled() { }

        public virtual void OnGONetParticipant_OwnerAuthorityIdSet() { }
    }
}
