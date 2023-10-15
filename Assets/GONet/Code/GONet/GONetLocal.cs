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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// One of these is automatically spawned for each machine in the network game during GONet initialization.
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public class GONetLocal : GONetParticipantCompanionBehaviour, IEnumerable<KeyValuePair<ushort, GONetLocal>>
    {
        private static readonly Dictionary<ushort, GONetLocal> localsByAuthorityId = new Dictionary<ushort, GONetLocal>(1024);

        private static GONetLocal lookupByAuthorityId;
        /// <summary>
        /// This is only to be used as a means by which to statically use the indexer in order to look up the instance of this class that "belongs to" the authority id passed in as the index.
        /// </summary>
        public static GONetLocal LookupByAuthorityId => lookupByAuthorityId;

        public GONetLocal this[ushort authorityId]
        {
            get
            {
                GONetLocal local;
                if (localsByAuthorityId.TryGetValue(authorityId, out local))
                {
                    Dictionary<ushort, GONetLocal>.Enumerator enumerator = localsByAuthorityId.GetEnumerator();
                    return local;
                }
                return null;
            }
        }

        /// <summary>
        /// This will enumerator over all <see cref="GONetLocal"/> instances in the game (i.e., from server and each client connected).
        /// </summary>
        public static IEnumerator<KeyValuePair<ushort, GONetLocal>> GetEnumerator_AllGONetLocals()
        {
            return LookupByAuthorityId.GetEnumerator();
        }

        #region IEnumerable impl

        /// <summary>
        /// This will enumerator over all <see cref="GONetLocal"/> instances in the game (i.e., from server and each client connected).
        /// </summary>
        public IEnumerator<KeyValuePair<ushort, GONetLocal>> GetEnumerator()
        {
            return localsByAuthorityId.GetEnumerator();
        }

        /// <summary>
        /// This will enumerator over all <see cref="GONetLocal"/> instances in the game (i.e., from server and each client connected).
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return localsByAuthorityId.GetEnumerator();
        }

        #endregion

        public ushort OwnerAuthorityId => gonetParticipant.OwnerAuthorityId;

        private readonly List<GONetParticipant> myEnabledGONetParticipants = new List<GONetParticipant>(200);
        /// <summary>
        /// <para>A convenient collection of all the <see cref="GONetParticipant"/> instances that are currently enabled and share the same <see cref="GONetParticipant.OwnerAuthorityId"/> value as <see cref="gonetParticipant"/>.</para>
        /// <para>Elements are added here once Start() was called on the <see cref="GONetParticipant"/> and removed once OnDisable() is called.</para>
        /// <para>Do NOT attempt to modify this collection as to avoid creating issues for yourself/others.</para>
        /// </summary>
        public IEnumerable<GONetParticipant> MyEnabledGONetParticipants => myEnabledGONetParticipants;

        public GONetLocal()
        {
            if ((object)lookupByAuthorityId == null)
            {
                lookupByAuthorityId = this;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            myEnabledGONetParticipants.Clear();

            foreach (GONetParticipant gnp in GameObject.FindObjectsOfType<GONetParticipant>()) // since GONetLocal is spawned in at runtime (unlike GONetGlobal), go ahead and add all the ones that are present now
            {
                AddIfAppropriate(gnp);
            }

            GONetMain.EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_OwnerAuthorityId, OnGNPAuthorityChanged_CheckIfStilllMine);

            StartCoroutine(AddToLookupOnceAuthorityIdKnown(this));
        }

        private IEnumerator AddToLookupOnceAuthorityIdKnown(GONetLocal gonetLocal)
        {
            while (gonetLocal.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset)
            {
                yield return null;
            }

            localsByAuthorityId[gonetLocal.OwnerAuthorityId] = gonetLocal;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            localsByAuthorityId.Remove(OwnerAuthorityId);
        }

        public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantEnabled(gonetParticipant);

            AddIfAppropriate(gonetParticipant);
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            AddIfAppropriate(gonetParticipant);
        }

        private void AddIfAppropriate(GONetParticipant gonetParticipant)
        {
            if (IsRelatedToThisLocality(gonetParticipant) &&
                !myEnabledGONetParticipants.Contains(gonetParticipant)) // may have already been added elsewhere
            {
                myEnabledGONetParticipants.Add(gonetParticipant);
            }
        }

        public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
        {
            myEnabledGONetParticipants.Remove(gonetParticipant); // regardless of whether or not it was present before this call, it will not be present afterward
        }

        private void LateUpdate()
        {
            GONetMain.Update_DoTheHeavyLifting_IfAppropriate(this, true);
        }

        /// <summary>
        /// PRE: <paramref name="someGNP"/> known to not be null!
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsRelatedToThisLocality(GONetParticipant someGNP)
        {
            return someGNP.OwnerAuthorityId == gonetParticipant.OwnerAuthorityId;
        }

        private void OnGNPAuthorityChanged_CheckIfStilllMine(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            if ((object)eventEnvelope.GONetParticipant != null && // not sure why this would be the case there, but have to double check..no likie the null
                IsRelatedToThisLocality(eventEnvelope.GONetParticipant))
            {
                // since we have a list and not a hashset, we need to double check we do not already have this stored
                if (!myEnabledGONetParticipants.Contains(eventEnvelope.GONetParticipant))
                {
                    myEnabledGONetParticipants.Add(eventEnvelope.GONetParticipant);
                }
            }
            else
            {
                myEnabledGONetParticipants.Remove(eventEnvelope.GONetParticipant);
            }
        }
    }
}
