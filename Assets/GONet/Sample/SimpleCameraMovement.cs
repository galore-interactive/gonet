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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GONet.Sample
{
    [RequireComponent(typeof(Camera))]
    public class SimpleCameraMovement : GONetBehaviour
    {
        private static readonly Dictionary<ushort, Vector3> cameraPositionByAuthorityId = new Dictionary<ushort, Vector3>();

        GONetSampleInputSync myInputSync;
        GONetSampleInputSync MyInputSync => myInputSync ?? (GONetMain.IsClientVsServerStatusKnown ? myInputSync = ((object)GONetLocal.LookupByAuthorityId[GONetMain.MyAuthorityId] == null ? null : GONetLocal.LookupByAuthorityId[GONetMain.MyAuthorityId].GetComponent<GONetSampleInputSync>()) : null);

        public float speed = 5;

        public Image cameraMinimapIconPrefab;

        CanvasRenderer remoteCamerasMinimap;
        RectTransform remoteCamerasMinimapRectTransform;
        static readonly Vector2 minimapRepresentativeArea = new Vector2(60, 25);
        readonly List<Image> cameraMinimapIcons = new List<Image>(1024);

        Vector3 initialCameraPositionForEveryone;

        protected override void Awake()
        {
            base.Awake();

            initialCameraPositionForEveryone = transform.position;

            const string MINI = "RemoteCamerasMinimap";
            remoteCamerasMinimap = GameObject.Find(MINI).GetComponent<CanvasRenderer>();
            remoteCamerasMinimapRectTransform = remoteCamerasMinimap.GetComponent<RectTransform>();
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            if (gonetParticipant.GetComponent<GONetLocal>() != null)
            {
                Image cameraMinimapIcon = Instantiate(cameraMinimapIconPrefab, remoteCamerasMinimap.transform);
                cameraMinimapIcons.Add(cameraMinimapIcon);
            }
        }

        private void Update()
        {
            /* input sync was updated to new input system and some other stuff was commented out....need revisit this:
            MoveCameraIfMyInputDicates();

            CalculateEveryonesCameraPositionFromInputs();

            UpdateMinimap();
            */
        }

        private void MoveCameraIfMyInputDicates()
        {
            if ((object)MyInputSync != null)
            {
                transform.position = CalculateUpdatedPosition(transform.position, speed, MyInputSync);

                // also, update our data structure with my camera position after the move/update
                cameraPositionByAuthorityId[GONetMain.MyAuthorityId] = transform.position;
            }
        }

        /// <summary>
        /// Call this every frame as <see cref="Time.deltaTime"/> is used to calculate how much movement based on input keys pressed on remote machines
        /// </summary>
        private void CalculateEveryonesCameraPositionFromInputs()
        {
            using (var gonetLocalEnumerator = GONetLocal.GetEnumerator_AllGONetLocals())
            {
                while (gonetLocalEnumerator.MoveNext())
                {
                    KeyValuePair<ushort, GONetLocal> authorityIdAndGONetLocal = gonetLocalEnumerator.Current;
                    ushort authorityId = authorityIdAndGONetLocal.Key;

                    if (authorityId != GONetMain.MyAuthorityId) // my camera position is tracked already from the call to MoveCameraIfMyInputDicates(), so we will not do it again
                    {
                        GONetLocal gonetLocal = authorityIdAndGONetLocal.Value;
                        GONetSampleInputSync inputSync = gonetLocal.GetComponent<GONetSampleInputSync>();

                        Vector3 calculatedCameraPosition;
                        if (!cameraPositionByAuthorityId.TryGetValue(authorityId, out calculatedCameraPosition))
                        {
                            calculatedCameraPosition = initialCameraPositionForEveryone;
                        }

                        calculatedCameraPosition = CalculateUpdatedPosition(calculatedCameraPosition, speed, inputSync);
                        cameraPositionByAuthorityId[authorityId] = calculatedCameraPosition;
                    }
                }
            }
        }

        /// <summary>
        /// IMPORTANT: This is an extremely simple way of calculating the position of all other remote cameras based on the remote key input from each machine.
        ///            For a much more accurate implementation, you would have to keep track of the related events (e.g., <see cref="SyncEvent_GONetSampleInputSync_GetKey_LeftArrow"/>)
        ///            and their <see cref="SyncEvent_ValueChangeProcessed.OccurredAtElapsedSeconds"/> so you could calculate and then use the exact amount of time/seconds each key
        ///            has been pressed in order to apply the precise amount of movement that will match the remote machine's processing.  This would be much more accurate than just using
        ///            <see cref="Time.deltaTime"/> while the remote key(s) is listed as being down.
        /// </summary>
        private Vector3 CalculateUpdatedPosition(Vector3 initialPosition, float speed, GONetSampleInputSync inputSync)
        {
            Vector3 updatedPosition = initialPosition;

            float moveAmount = speed * Time.deltaTime;

            if (inputSync.GetKey_DownArrow || inputSync.GetKey_S)
            {
                updatedPosition += new Vector3(0, -moveAmount);
            }
            if (inputSync.GetKey_UpArrow || inputSync.GetKey_W)
            {
                updatedPosition += new Vector3(0, moveAmount);
            }

            if (inputSync.GetKey_LeftArrow || inputSync.GetKey_A)
            {
                updatedPosition += new Vector3(0, 0, -moveAmount); // NOTE: since the camera is rotated 270 around Y, this yields relative x movement
            }
            if (inputSync.GetKey_RightArrow || inputSync.GetKey_D) // NOTE: since the camera is rotated 270 around Y, this yields relative x movement
            {
                updatedPosition += new Vector3(0, 0, moveAmount);
            }

            return updatedPosition;
        }

        private void UpdateMinimap()
        {
            using (var enumerator = cameraPositionByAuthorityId.GetEnumerator())
            {
                int iIcon = 0;
                while (enumerator.MoveNext())
                {
                    Vector3 remoteCameraPosition = enumerator.Current.Value;
                    Image remoteCameraIcon = cameraMinimapIcons[iIcon++];
                    Vector3 diffFromOriginal = remoteCameraPosition - initialCameraPositionForEveryone;

                    Vector2 newPositionInMinimap = 
                        new Vector2(
                            (diffFromOriginal.z / minimapRepresentativeArea.x) * remoteCamerasMinimapRectTransform.sizeDelta.x, 
                            (diffFromOriginal.y / minimapRepresentativeArea.y) * remoteCamerasMinimapRectTransform.sizeDelta.y);

                    remoteCameraIcon.rectTransform.anchoredPosition = newPositionInMinimap;
                }
            }
        }
    }
}
