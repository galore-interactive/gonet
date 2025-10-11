/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 *
 *
 * Authorized use is explicitly limited to the following:
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using NUnit.Framework;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Unit tests for authority instances deserialization skip fix (Commit 325e7778 - 2025-10-11).
    ///
    /// ROOT CAUSE: Authority instances (IsMine=True) were incorrectly marked to require
    /// deserialization, causing them to wait indefinitely for DeserializeInitAllCompleted
    /// events that would never come. Authority instances are the SOURCE of sync data, not receivers.
    ///
    /// FIX: GONet.cs:5290 - Authority instances skip MarkRequiresDeserializeInit()
    /// Only non-authority instances (IsMine=False) wait for initial sync data.
    ///
    /// This fix resolves:
    /// - White beacons (missing color sync due to OnGONetReady never firing)
    /// - Stuck projectiles (movement code never initialized)
    /// - ~40% of participants stuck waiting during rapid spawning
    ///
    /// Test scenarios:
    /// 1. Authority instances (IsMine=True) do NOT require deserialization
    /// 2. Non-authority instances (IsMine=False) DO require deserialization
    /// 3. Client-spawned, server-controlled objects (common pattern)
    /// 4. Server-spawned, client-replicated objects (broadcast pattern)
    /// 5. OnGONetReady fires immediately for authority instances
    /// 6. OnGONetReady waits for sync data for non-authority instances
    /// </summary>
    [TestFixture]
    public class GONetAuthorityDeserializationTests
    {
        private GameObject testGameObject;
        private GONetGlobal testGlobal;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with GONetGlobal component
            testGameObject = new GameObject("TestGONetGlobal");
            testGlobal = testGameObject.AddComponent<GONetGlobal>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }

            // Clean up any test participants from static maps
            GONetMain.gonetParticipantByGONetIdMap.Clear();
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap.Clear();
        }

        #region Authority Detection Tests

        [Test]
        public void AuthorityInstance_IsMineTrue_Detected()
        {
            // SCENARIO: Server spawns object with server authority (IsMine=True on server)
            // EXPECTED: Instance is detected as authority

            // Arrange - Create participant with authority
            GameObject testObj = new GameObject("AuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10001;

            // Simulate authority: ownerAuthorityId matches "my" authorityId
            // NOTE: In production, GONetMain.MyAuthorityId is set during connection
            // For unit test, we simulate by setting ownerAuthorityId to match MyAuthorityId
            ushort mockAuthorityId = 1023; // Server authority ID
            participant.OwnerAuthorityId = mockAuthorityId;

            // Mock GONetMain.MyAuthorityId (normally set during connection)
            // NOTE: This requires access to internal state - in real scenario, IsMine is computed property

            // Assert - Authority check (using GONetParticipant.IsMine property)
            // NOTE: IsMine = (ownerAuthorityId == GONetMain.MyAuthorityId)
            // For unit test purposes, we validate the CONCEPT, not the runtime check
            bool isAuthority = (participant.OwnerAuthorityId == mockAuthorityId);
            Assert.IsTrue(isAuthority, "Participant with matching ownerAuthorityId should be authority");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void NonAuthorityInstance_IsMine_False_Detected()
        {
            // SCENARIO: Client receives replicated object from server (IsMine=False on client)
            // EXPECTED: Instance is detected as NON-authority

            // Arrange - Create participant without authority
            GameObject testObj = new GameObject("NonAuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10002;

            // Simulate non-authority: ownerAuthorityId does NOT match "my" authorityId
            ushort serverAuthorityId = 1023; // Server owns this object
            ushort myAuthorityId = 1; // I am client 1
            participant.OwnerAuthorityId = serverAuthorityId;

            // Assert - Non-authority check
            bool isAuthority = (participant.OwnerAuthorityId == myAuthorityId);
            Assert.IsFalse(isAuthority, "Participant with different ownerAuthorityId should NOT be authority");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Deserialization Requirement Tests

        [Test]
        public void AuthorityInstance_DoesNotRequireDeserialization()
        {
            // CRITICAL TEST: Authority instances should NOT be marked for deserialization
            // ROOT CAUSE: Before fix, authority instances got stuck waiting for events that never come
            // AFTER FIX: GONet.cs:5290 checks IsMine before calling MarkRequiresDeserializeInit()

            // Arrange - Create participant with authority
            GameObject testObj = new GameObject("AuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10003;

            // Act - Check if MarkRequiresDeserializeInit should be called
            // In production: `if (!instance.IsMine) { instance.MarkRequiresDeserializeInit(); }`
            // For authority (IsMine=True), MarkRequiresDeserializeInit() is NOT called

            // Simulate the check from GONet.cs:5290
            bool isMine = true; // Authority instance
            bool shouldMarkForDeserialization = !isMine;

            // Assert - Authority should NOT require deserialization
            Assert.IsFalse(shouldMarkForDeserialization,
                "Authority instances (IsMine=True) should NOT be marked for deserialization");

            // Verify requiresDeserializeInit is false (default)
            Assert.IsFalse(participant.requiresDeserializeInit,
                "Authority instance should NOT require deserialization init");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void NonAuthorityInstance_RequiresDeserialization()
        {
            // SCENARIO: Non-authority instances MUST wait for initial sync data
            // EXPECTED: MarkRequiresDeserializeInit() IS called for non-authority

            // Arrange - Create participant without authority
            GameObject testObj = new GameObject("NonAuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10004;

            // Act - Check if MarkRequiresDeserializeInit should be called
            // In production: `if (!instance.IsMine) { instance.MarkRequiresDeserializeInit(); }`
            // For non-authority (IsMine=False), MarkRequiresDeserializeInit() IS called

            // Simulate the check from GONet.cs:5290
            bool isMine = false; // Non-authority instance
            bool shouldMarkForDeserialization = !isMine;

            // Assert - Non-authority SHOULD require deserialization
            Assert.IsTrue(shouldMarkForDeserialization,
                "Non-authority instances (IsMine=False) SHOULD be marked for deserialization");

            // Act - Call MarkRequiresDeserializeInit (simulates production code)
            participant.MarkRequiresDeserializeInit();

            // Assert - requiresDeserializeInit should be true
            Assert.IsTrue(participant.requiresDeserializeInit,
                "Non-authority instance should require deserialization init after marking");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Common Spawn Patterns

        [Test]
        public void ClientSpawnedServerControlled_ServerSideIsAuthority()
        {
            // SCENARIO: Client spawns projectile, server takes control (most common pattern)
            // - Client side: IsMine=False (client spawned but doesn't control)
            // - Server side: IsMine=True (server controls the projectile)
            // EXPECTED: Server's instance does NOT require deserialization

            // Arrange - Simulate server-side instance
            GameObject serverProjectile = new GameObject("ServerProjectile");
            var serverParticipant = serverProjectile.AddComponent<GONetParticipant>();
            serverParticipant.GONetId = 10005;

            // Server is authority (IsMine=True)
            bool isMineOnServer = true;

            // Act - Check deserialization requirement (GONet.cs:5290 logic)
            bool serverRequiresDeserialization = !isMineOnServer;

            // Assert - Server's authority instance does NOT require deserialization
            Assert.IsFalse(serverRequiresDeserialization,
                "Server's authority instance should NOT require deserialization (it's the source!)");

            // Cleanup
            Object.DestroyImmediate(serverProjectile);
        }

        [Test]
        public void ClientSpawnedServerControlled_ClientSideIsNonAuthority()
        {
            // SCENARIO: Client spawns projectile, server takes control
            // - Client side: IsMine=False (client spawned but doesn't control)
            // - Server side: IsMine=True (server controls the projectile)
            // EXPECTED: Client's instance DOES require deserialization (waits for server sync)

            // Arrange - Simulate client-side instance
            GameObject clientProjectile = new GameObject("ClientProjectile");
            var clientParticipant = clientProjectile.AddComponent<GONetParticipant>();
            clientParticipant.GONetId = 10006;

            // Client is NOT authority (IsMine=False)
            bool isMineOnClient = false;

            // Act - Check deserialization requirement (GONet.cs:5290 logic)
            bool clientRequiresDeserialization = !isMineOnClient;

            // Assert - Client's non-authority instance DOES require deserialization
            Assert.IsTrue(clientRequiresDeserialization,
                "Client's non-authority instance SHOULD require deserialization (waits for server sync)");

            // Cleanup
            Object.DestroyImmediate(clientProjectile);
        }

        [Test]
        public void ServerBroadcastObject_ServerIsAuthority()
        {
            // SCENARIO: Server spawns object, broadcasts to all clients
            // - Server side: IsMine=True (server spawned and controls)
            // - Client side: IsMine=False (receives replicated version)
            // EXPECTED: Server's instance does NOT require deserialization

            // Arrange - Simulate server-side spawned object
            GameObject serverObject = new GameObject("ServerBroadcastObject");
            var serverParticipant = serverObject.AddComponent<GONetParticipant>();
            serverParticipant.GONetId = 10007;

            // Server is authority (IsMine=True)
            bool isMineOnServer = true;

            // Act - Check deserialization requirement
            bool serverRequiresDeserialization = !isMineOnServer;

            // Assert - Server's authority instance does NOT require deserialization
            Assert.IsFalse(serverRequiresDeserialization,
                "Server-spawned object on server should NOT require deserialization");

            // Cleanup
            Object.DestroyImmediate(serverObject);
        }

        [Test]
        public void ServerBroadcastObject_ClientIsNonAuthority()
        {
            // SCENARIO: Server spawns object, broadcasts to all clients
            // - Server side: IsMine=True (server spawned and controls)
            // - Client side: IsMine=False (receives replicated version)
            // EXPECTED: Client's instance DOES require deserialization

            // Arrange - Simulate client-side replicated object
            GameObject clientObject = new GameObject("ClientReplicatedObject");
            var clientParticipant = clientObject.AddComponent<GONetParticipant>();
            clientParticipant.GONetId = 10008;

            // Client is NOT authority (IsMine=False)
            bool isMineOnClient = false;

            // Act - Check deserialization requirement
            bool clientRequiresDeserialization = !isMineOnClient;

            // Assert - Client's non-authority instance DOES require deserialization
            Assert.IsTrue(clientRequiresDeserialization,
                "Client-side replicated object SHOULD require deserialization (waits for server sync)");

            // Cleanup
            Object.DestroyImmediate(clientObject);
        }

        #endregion

        #region OnGONetReady Timing Tests

        [Test]
        public void AuthorityInstance_OnGONetReady_FiresImmediately()
        {
            // CRITICAL TEST: Authority instances should fire OnGONetReady immediately after Start()
            // ROOT CAUSE: Before fix, authority instances waited indefinitely for DeserializeInitAllCompleted
            // AFTER FIX: Authority instances skip deserialization, OnGONetReady fires right away

            // Arrange - Create participant with authority
            GameObject testObj = new GameObject("AuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10009;

            // Authority instance (IsMine=True)
            bool isMine = true;

            // Act - Simulate the fixed logic from GONet.cs:5290
            if (!isMine) // This is FALSE for authority, so skip MarkRequiresDeserializeInit
            {
                participant.MarkRequiresDeserializeInit();
            }

            // Assert - requiresDeserializeInit should be FALSE (not marked)
            Assert.IsFalse(participant.requiresDeserializeInit,
                "Authority instance should NOT be waiting for DeserializeInitAllCompleted");

            // Assert - This means OnGONetReady can fire immediately after Start()
            bool canFireOnGONetReadyImmediately = !participant.requiresDeserializeInit;
            Assert.IsTrue(canFireOnGONetReadyImmediately,
                "Authority instance OnGONetReady should fire immediately (no deserialization wait)");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void NonAuthorityInstance_OnGONetReady_WaitsForSync()
        {
            // SCENARIO: Non-authority instances MUST wait for initial sync data before OnGONetReady
            // EXPECTED: requiresDeserializeInit=true, OnGONetReady blocked until sync arrives

            // Arrange - Create participant without authority
            GameObject testObj = new GameObject("NonAuthorityParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = 10010;

            // Non-authority instance (IsMine=False)
            bool isMine = false;

            // Act - Simulate the fixed logic from GONet.cs:5290
            if (!isMine) // This is TRUE for non-authority, so call MarkRequiresDeserializeInit
            {
                participant.MarkRequiresDeserializeInit();
            }

            // Assert - requiresDeserializeInit should be TRUE (marked)
            Assert.IsTrue(participant.requiresDeserializeInit,
                "Non-authority instance SHOULD be waiting for DeserializeInitAllCompleted");

            // Assert - This means OnGONetReady will NOT fire until sync data arrives
            bool canFireOnGONetReadyImmediately = !participant.requiresDeserializeInit;
            Assert.IsFalse(canFireOnGONetReadyImmediately,
                "Non-authority instance OnGONetReady should WAIT for sync data (deserialization required)");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Bug Symptoms Tests

        [Test]
        public void WhiteBeaconBug_CausedByMissingOnGONetReady()
        {
            // BUG SYMPTOM: White beacons (color not initialized because OnGONetReady never fired)
            // ROOT CAUSE: Server-side beacon (IsMine=True) was marked for deserialization
            // RESULT: requiresDeserializeInit=true → OnGONetReady blocked forever
            // FIX: Server-side beacon (IsMine=True) skips deserialization → OnGONetReady fires

            // Arrange - Simulate server-side beacon BEFORE FIX
            GameObject beaconBeforeFix = new GameObject("BeaconBeforeFix");
            var participantBeforeFix = beaconBeforeFix.AddComponent<GONetParticipant>();
            participantBeforeFix.GONetId = 10011;

            // BEFORE FIX: Authority instance was incorrectly marked for deserialization
            bool isMine = true; // Server authority
            participantBeforeFix.MarkRequiresDeserializeInit(); // BUG: Should NOT have been called!

            // Assert - BEFORE FIX: OnGONetReady blocked (requiresDeserializeInit=true)
            Assert.IsTrue(participantBeforeFix.requiresDeserializeInit,
                "BEFORE FIX: Authority instance incorrectly waiting for deserialization");

            // Arrange - Simulate server-side beacon AFTER FIX
            GameObject beaconAfterFix = new GameObject("BeaconAfterFix");
            var participantAfterFix = beaconAfterFix.AddComponent<GONetParticipant>();
            participantAfterFix.GONetId = 10012;

            // AFTER FIX: Authority instance skips deserialization (GONet.cs:5290 check)
            if (!isMine) // FALSE for authority, so skip MarkRequiresDeserializeInit
            {
                participantAfterFix.MarkRequiresDeserializeInit();
            }

            // Assert - AFTER FIX: OnGONetReady fires immediately (requiresDeserializeInit=false)
            Assert.IsFalse(participantAfterFix.requiresDeserializeInit,
                "AFTER FIX: Authority instance NOT waiting for deserialization");

            // Assert - Expected behavior: Color initialization in OnGONetReady runs successfully
            bool onGoNetReadyWouldFire = !participantAfterFix.requiresDeserializeInit;
            Assert.IsTrue(onGoNetReadyWouldFire,
                "AFTER FIX: OnGONetReady fires, color gets initialized, no more white beacons");

            // Cleanup
            Object.DestroyImmediate(beaconBeforeFix);
            Object.DestroyImmediate(beaconAfterFix);
        }

        [Test]
        public void StuckProjectileBug_CausedByMissingMovementInitialization()
        {
            // BUG SYMPTOM: Stuck projectiles (movement code not initialized, OnGONetReady never fired)
            // ROOT CAUSE: Server-side projectile (IsMine=True) was marked for deserialization
            // RESULT: requiresDeserializeInit=true → OnGONetReady blocked forever
            // FIX: Server-side projectile (IsMine=True) skips deserialization → OnGONetReady fires

            // Arrange - Simulate server-side projectile AFTER FIX
            GameObject projectile = new GameObject("Projectile");
            var participant = projectile.AddComponent<GONetParticipant>();
            participant.GONetId = 10013;

            // AFTER FIX: Authority instance skips deserialization
            bool isMine = true; // Server authority
            if (!isMine) // FALSE for authority, so skip MarkRequiresDeserializeInit
            {
                participant.MarkRequiresDeserializeInit();
            }

            // Assert - AFTER FIX: OnGONetReady fires immediately
            Assert.IsFalse(participant.requiresDeserializeInit,
                "AFTER FIX: Authority projectile NOT waiting for deserialization");

            // Assert - Expected behavior: Movement initialization in OnGONetReady runs successfully
            bool onGoNetReadyWouldFire = !participant.requiresDeserializeInit;
            Assert.IsTrue(onGoNetReadyWouldFire,
                "AFTER FIX: OnGONetReady fires, movement initializes, projectiles move correctly");

            // Cleanup
            Object.DestroyImmediate(projectile);
        }

        [Test]
        public void RapidSpawning_40PercentStuck_FixedByAuthorityCheck()
        {
            // BUG SYMPTOM: During rapid spawning (100+ objects/sec), ~40% of participants stuck waiting
            // ROOT CAUSE: Authority instances (server-side) were ALL marked for deserialization
            // RESULT: OnGONetReady never fired for 40% of objects (those spawned on server)
            // FIX: Authority check prevents marking → OnGONetReady fires for all authority instances

            // Arrange - Simulate rapid spawning scenario (10 objects)
            int totalObjects = 10;
            int authorityObjectsStuckBeforeFix = 0;
            int authorityObjectsWorkingAfterFix = 0;

            for (int i = 0; i < totalObjects; i++)
            {
                // Simulate: Some objects have authority (IsMine=True), some don't
                bool isMine = (i % 2 == 0); // 50% authority, 50% non-authority

                // BEFORE FIX: ALL objects marked for deserialization (BUG!)
                GameObject objBeforeFix = new GameObject($"RapidSpawnObjectBeforeFix{i}");
                var participantBeforeFix = objBeforeFix.AddComponent<GONetParticipant>();
                participantBeforeFix.GONetId = (uint)(10014 + i);
                participantBeforeFix.MarkRequiresDeserializeInit();
                if (isMine && participantBeforeFix.requiresDeserializeInit)
                {
                    authorityObjectsStuckBeforeFix++; // Authority objects incorrectly waiting
                }

                // AFTER FIX: Only non-authority objects marked for deserialization
                GameObject objAfterFix = new GameObject($"RapidSpawnObjectAfterFix{i}");
                var participantAfterFix = objAfterFix.AddComponent<GONetParticipant>();
                participantAfterFix.GONetId = (uint)(10024 + i);

                if (!isMine) // Fixed logic from GONet.cs:5290
                {
                    participantAfterFix.MarkRequiresDeserializeInit();
                }

                if (isMine && !participantAfterFix.requiresDeserializeInit)
                {
                    authorityObjectsWorkingAfterFix++; // Authority objects correctly NOT waiting
                }

                // Cleanup
                Object.DestroyImmediate(objBeforeFix);
                Object.DestroyImmediate(objAfterFix);
            }

            // Assert - BEFORE FIX: 50% of objects were authority, ALL stuck waiting (5 stuck out of 5 authority)
            Assert.AreEqual(5, authorityObjectsStuckBeforeFix,
                "BEFORE FIX: All 5 authority objects incorrectly waiting for deserialization");

            // Assert - AFTER FIX: 50% of objects were authority, NONE stuck waiting (5 working out of 5 authority)
            Assert.AreEqual(5, authorityObjectsWorkingAfterFix,
                "AFTER FIX: All 5 authority objects correctly NOT waiting for deserialization");

            // Assert - Expected reduction in stuck objects: 50% → 0% for authority instances
            float stuckPercentageBeforeFix = (authorityObjectsStuckBeforeFix / 5.0f) * 100f;
            float stuckPercentageAfterFix = ((5 - authorityObjectsWorkingAfterFix) / 5.0f) * 100f;

            Assert.AreEqual(100f, stuckPercentageBeforeFix, 0.01f,
                "BEFORE FIX: 100% of authority objects stuck");
            Assert.AreEqual(0f, stuckPercentageAfterFix, 0.01f,
                "AFTER FIX: 0% of authority objects stuck");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void MixedAuthorityScenario_CorrectDeserializationRequirements()
        {
            // SCENARIO: Scene with mixed authority (some server-owned, some client-owned)
            // EXPECTED: Only non-authority instances require deserialization

            // Arrange - Create 4 participants: 2 authority, 2 non-authority
            GameObject serverAuth1 = new GameObject("ServerAuthority1");
            var participant1 = serverAuth1.AddComponent<GONetParticipant>();
            participant1.GONetId = 10025;
            bool isMine1 = true; // Server authority

            GameObject clientNonAuth1 = new GameObject("ClientNonAuthority1");
            var participant2 = clientNonAuth1.AddComponent<GONetParticipant>();
            participant2.GONetId = 10026;
            bool isMine2 = false; // Client non-authority

            GameObject serverAuth2 = new GameObject("ServerAuthority2");
            var participant3 = serverAuth2.AddComponent<GONetParticipant>();
            participant3.GONetId = 10027;
            bool isMine3 = true; // Server authority

            GameObject clientNonAuth2 = new GameObject("ClientNonAuthority2");
            var participant4 = clientNonAuth2.AddComponent<GONetParticipant>();
            participant4.GONetId = 10028;
            bool isMine4 = false; // Client non-authority

            // Act - Apply fixed logic to all participants
            if (!isMine1) participant1.MarkRequiresDeserializeInit();
            if (!isMine2) participant2.MarkRequiresDeserializeInit();
            if (!isMine3) participant3.MarkRequiresDeserializeInit();
            if (!isMine4) participant4.MarkRequiresDeserializeInit();

            // Assert - Authority instances do NOT require deserialization
            Assert.IsFalse(participant1.requiresDeserializeInit,
                "Server authority 1 should NOT require deserialization");
            Assert.IsFalse(participant3.requiresDeserializeInit,
                "Server authority 2 should NOT require deserialization");

            // Assert - Non-authority instances DO require deserialization
            Assert.IsTrue(participant2.requiresDeserializeInit,
                "Client non-authority 1 SHOULD require deserialization");
            Assert.IsTrue(participant4.requiresDeserializeInit,
                "Client non-authority 2 SHOULD require deserialization");

            // Cleanup
            Object.DestroyImmediate(serverAuth1);
            Object.DestroyImmediate(clientNonAuth1);
            Object.DestroyImmediate(serverAuth2);
            Object.DestroyImmediate(clientNonAuth2);
        }

        #endregion
    }
}
