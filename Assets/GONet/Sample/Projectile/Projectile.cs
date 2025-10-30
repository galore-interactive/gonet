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

using TMPro;
using UnityEngine;

namespace GONet.Sample
{
    [RequireComponent(typeof(GONetParticipant))]
    public class Projectile : GONetParticipantCompanionBehaviour
    {
        private float startSpeed;
        public float speed = 5;

        /// <summary>
        /// Movement direction set at spawn time (for shotgun spread effect).
        /// Stored separately so rotation doesn't affect movement path.
        /// IMPORTANT: Initialized to Vector3.zero as sentinel value. Set in Awake() to transform.forward.
        /// ProjectileSpawner checks if zero before applying movement to handle OnGONetReady/Awake race condition.
        /// </summary>
        [HideInInspector]
        public Vector3 movementDirection = Vector3.zero;

        TextMeshProUGUI text;

        protected override void Awake()
        {
            base.Awake();

            text = GetComponentInChildren<TextMeshProUGUI>();

            startSpeed = speed;

            // Store initial forward direction for movement (unaffected by rotation)
            // CRITICAL: ProjectileSpawner may add this projectile to its update loop BEFORE Awake() runs
            // (OnGONetReady called before Awake on server). Setting this at the END of Awake ensures
            // movement doesn't start until direction is properly initialized.
            movementDirection = transform.forward;

            /*
            // DIAGNOSTIC: Log initial movement direction AND spawn event
            GONetLog.Info($"[PROJECTILE-SPAWN] 🚀 GONetId will be assigned soon, " +
                         $"GameObject: '{gameObject.name}', " +
                         $"Position: {transform.position}, " +
                         $"MovementDirection: {movementDirection}");
            */
        }

        // DIAGNOSTIC: Track first update to log ownership state once
        private bool hasLoggedOwnershipOnce = false;
        private float despawnTimer = 0f;

        /// <summary>
        /// EARLY FRAME UPDATE: Projectile movement using GONet's UpdateAfterGONetReady pattern.
        ///
        /// This demonstrates the ⭐ HIGHLY PREFERRED pattern for networked game logic:
        /// - NO defensive checks needed (guaranteed movementDirection is initialized)
        /// - Runs early in frame (priority -32000, before most Update() methods)
        /// - Zero overhead if not overridden (static per-type caching)
        /// - Constant overhead (1 Update() call) vs linear overhead (N Update() calls with Unity's Update())
        ///
        /// ALTERNATIVE PATTERN: See SpawnTestBeacon.cs for defensive Update() pattern
        /// (only use when you need precise Script Execution Order control).
        ///
        /// See ONGONETREADY_LIFECYCLE_DESIGN.md for detailed pattern comparison.
        /// </summary>
        internal override void UpdateAfterGONetReady()
        {
            // DIAGNOSTIC: Log on first UpdateAfterGONetReady call
            if (!hasLoggedOwnershipOnce)
            {
                hasLoggedOwnershipOnce = true;
                //GONetLog.Debug($"[Projectile] '{gameObject.name}' (GONetId: {gonetParticipant.GONetId}) FIRST UpdateAfterGONetReady - " +
                //    $"IsMine: {gonetParticipant.IsMine}, " +
                //    $"OwnerAuthorityId: {gonetParticipant.OwnerAuthorityId}, " +
                //    $"MyAuthorityId: {GONetMain.MyAuthorityId}, " +
                //    $"IsServer: {GONetMain.IsServer}, " +
                //    $"IsClient: {GONetMain.IsClient}, " +
                //    $"startSpeed: {startSpeed}, " +
                //    $"speed: {speed}, " +
                //    $"movementDirection: {movementDirection}");
            }

            if (gonetParticipant.IsMine)
            {
                // NO DEFENSIVE CHECK NEEDED for movementDirection!
                // UpdateAfterGONetReady guarantees:
                // - OnGONetReady has fired for this projectile
                // - Projectile.Awake() has completed (sets movementDirection)
                // - movementDirection is guaranteed to be initialized (non-zero)

                // Move in stored direction (unaffected by rotation - shotgun spread effect)
                transform.position += movementDirection * UnityEngine.Time.deltaTime * speed;

                // Visual rotation (doesn't affect movement path)
                const float CYCLE_SECONDS = 5f;
                const float DEGREES_PER_CYCLE = 360f / CYCLE_SECONDS;
                var smoothlyChangingMultiplyFactor = UnityEngine.Time.time % CYCLE_SECONDS;
                smoothlyChangingMultiplyFactor *= DEGREES_PER_CYCLE;
                smoothlyChangingMultiplyFactor = Mathf.Sin(smoothlyChangingMultiplyFactor * Mathf.Deg2Rad) + 2; // should be between 1 and 3 after this
                float rotationAngle = UnityEngine.Time.deltaTime * 100 * smoothlyChangingMultiplyFactor;
                transform.Rotate(rotationAngle, rotationAngle, rotationAngle);
            }
        }

        private void Update()
        {
            if (gonetParticipant.IsMine)
            {
                if (speed > -startSpeed)
                {
                    speed -= Time.deltaTime;
                    despawnTimer += Time.deltaTime;

                    const string MINE = "Mine";
                    text.text = MINE;
                    text.color = Color.green;
                }
                else
                {
                    // DIAGNOSTIC: Log despawn decision with GONetId tracking
                    //GONetLog.Debug($"[PROJECTILE-DESPAWN] 💥 GONetId: {gonetParticipant.GONetId}, " +
                    //    $"GameObject: '{gameObject.name}', " +
                    //    $"Lifetime: {despawnTimer:F2}s, " +
                    //    $"Speed: {speed:F2} (threshold: {-startSpeed:F2}), " +
                    //    $"IsMine: {gonetParticipant.IsMine}, " +
                    //    $"OwnerAuthorityId: {gonetParticipant.OwnerAuthorityId}, " +
                    //    $"IsServer: {GONetMain.IsServer}");

                    Destroy(gameObject); // avoid having an ever growing list of things going when they go off screen and cannot be seen
                }
            }
            else
            {
                const string WHO = "?";
                text.text = WHO;
                text.color = Color.red;
            }
        }
    }
}
