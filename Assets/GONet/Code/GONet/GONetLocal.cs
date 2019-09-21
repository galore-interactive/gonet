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
using System.Collections.Generic;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// One of these is automatically spawned for each machine in the network game during GONet initialization.
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public class GONetLocal : MonoBehaviour
    {
        public ushort OwnerAuthorityId => gonetParticipant.OwnerAuthorityId;

        internal GONetParticipant gonetParticipant;

        private readonly List<GONetParticipant> myEnabledGONetParticipants = new List<GONetParticipant>(200);
        /// <summary>
        /// <para>A convenient collection of all the <see cref="GONetParticipant"/> instances that are currently enabled and share the same <see cref="GONetParticipant.OwnerAuthorityId"/> value as <see cref="gonetParticipant"/>.</para>
        /// <para>Elements are added here once Start() was called on the <see cref="GONetParticipant"/> and removed once OnDisable() is called.</para>
        /// <para>Do NOT attempt to modify this collection as to avoid creating issues for yourself/others.</para>
        /// </summary>
        public IEnumerable<GONetParticipant> MyEnabledGONetParticipants => myEnabledGONetParticipants;

        private void Awake()
        {
            gonetParticipant = GetComponent<GONetParticipant>();

            myEnabledGONetParticipants.Clear();

            foreach (GONetParticipant gnp in GameObject.FindObjectsOfType<GONetParticipant>()) // since GONetLocal is spawned in at runtime (unlike GONetGlobal), go ahead and add all the ones that are present now
            {
                if (gnp.IsMine)
                {
                    myEnabledGONetParticipants.Add(gnp);
                }
            }

            GONetMain.EventBus.Subscribe<GONetParticipantEnabledEvent>(OnGNPEnabled_AddToList);
            GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(OnGNPStarted_AddToList);
            GONetMain.EventBus.Subscribe<SyncEvent_GONetParticipant_OwnerAuthorityId>(OnGNPAuthorityChanged_CheckIfStilllMine);
            GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(OnGNPDisabled_RemoveFromList);
        }

        private void OnGNPEnabled_AddToList(GONetEventEnvelope<GONetParticipantEnabledEvent> eventEnvelope)
        {
            bool isTooEarlyToAdd_StartRequiredFirst = (object)eventEnvelope.GONetParticipant == null;
            if (!isTooEarlyToAdd_StartRequiredFirst)
            {
                myEnabledGONetParticipants.Add(eventEnvelope.GONetParticipant);
            }
        }

        private void OnGNPStarted_AddToList(GONetEventEnvelope<GONetParticipantStartedEvent> eventEnvelope)
        {
            if (eventEnvelope.GONetParticipant.IsMine && !myEnabledGONetParticipants.Contains(eventEnvelope.GONetParticipant)) // may have already been added in OnGNPEnabled_AddToList
            {
                myEnabledGONetParticipants.Add(eventEnvelope.GONetParticipant);
            }
        }

        private void OnGNPAuthorityChanged_CheckIfStilllMine(GONetEventEnvelope<SyncEvent_GONetParticipant_OwnerAuthorityId> eventEnvelope)
        {
            if (eventEnvelope.GONetParticipant.IsMine)
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

        private void OnGNPDisabled_RemoveFromList(GONetEventEnvelope<GONetParticipantDisabledEvent> eventEnvelope)
        {
            myEnabledGONetParticipants.Remove(eventEnvelope.GONetParticipant); // regardless of whether or not it was present before this call, it will not be present afterward
        }
    }
}
