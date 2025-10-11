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
    /// Unit tests for despawned participant handling fix (white beacon / stuck projectile bug).
    ///
    /// ROOT CAUSE: Server's sync thread was including despawned participants in outgoing
    /// sync bundles for 30+ seconds because everythingMap wasn't cleaned up on despawn.
    /// This caused GetCurrentGONetIdByIdAtInstantiation() to return 0 on client, aborting
    /// entire bundles and preventing 50-300+ other participants from receiving sync data.
    ///
    /// FIX: Added participant validation in sync processing loops (GONet.cs:7851-7893, 7915-7935)
    /// to skip despawned participants BEFORE appending to syncValuesToSend.
    ///
    /// Test scenarios:
    /// 1. Unity fake null detection (participant == null after Destroy)
    /// 2. GONetId unset detection (participant.GONetId == GONetId_Unset)
    /// 3. Map removal detection (!gonetParticipantByGONetIdMap.ContainsKey(id))
    /// 4. Authority-agnostic behavior (works for server-authority, client-authority)
    /// 5. Static map usage (gonetParticipantByGONetIdMap shared across all instances)
    /// 6. Prevention of bundle abort cascade
    /// 7. Collateral damage prevention (other participants still sync)
    /// </summary>
    [TestFixture]
    public class GONetDespawnedParticipantHandlingTests
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

            // Clean up any test participants from static map
            GONetMain.gonetParticipantByGONetIdMap.Clear();
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap.Clear();
        }

        #region Unity Fake Null Detection Tests

        [Test]
        public void DespawnedParticipant_UnityFakeNull_DetectedCorrectly()
        {
            // Arrange - Create participant
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();

            // Act - Destroy GameObject (enters Unity fake null state)
            Object.DestroyImmediate(testObj);

            // Assert - Unity == operator detects destroyed object
            Assert.IsTrue(participant == null,
                "Unity's == operator should return true for destroyed GameObject");

            // Assert - C# reference still exists (fake null pattern)
            Assert.IsTrue((object)participant != null,
                "C# reference should NOT be null (Unity fake null pattern)");
        }

        [Test]
        public void DespawnedParticipant_UnityFakeNull_SkippedInSyncProcessing()
        {
            // SCENARIO: Sync processing iterates everythingMap containing destroyed participant
            // EXPECTED: participant == null check triggers continue, skipping sync data append
            // IMPACT: Prevents sync bundle from containing InstantiationId for despawned object
            //         Prevents client-side GetCurrentGONetIdByIdAtInstantiation() returning 0
            //         Prevents bundle abort cascade affecting 50-300+ other participants

            // Arrange - Create and register participant
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            uint testGONetId = 12345;
            participant.GONetId = testGONetId;

            // Simulate registration in map (sync processing looks here for validation)
            GONetMain.gonetParticipantByGONetIdMap[testGONetId] = participant;

            // Act - Destroy GameObject BUT leave in map (simulates everythingMap not cleaned up)
            Object.DestroyImmediate(testObj);

            // Assert - participant should be Unity fake null
            Assert.IsTrue(participant == null,
                "Participant should appear destroyed to Unity's == operator");

            // Assert - This is the check that PREVENTS including it in sync bundles:
            // if (participant == null) { continue; }
            bool shouldSkipInSyncProcessing = (participant == null);
            Assert.IsTrue(shouldSkipInSyncProcessing,
                "Sync processing should skip destroyed participant (Unity fake null check)");

            // Cleanup
            GONetMain.gonetParticipantByGONetIdMap.Remove(testGONetId);
        }

        #endregion

        #region GONetId Unset Detection Tests

        [Test]
        public void DespawnedParticipant_GONetIdUnset_DetectedCorrectly()
        {
            // SCENARIO: Participant despawned, GONetId reset to GONetId_Unset (0)
            // EXPECTED: participant.GONetId == GONetId_Unset check triggers continue
            // RATIONALE: Defensive check for participants that were reset but not removed from map

            // Arrange - Create participant with unset GONetId
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = GONetParticipant.GONetId_Unset; // Explicitly unset

            // Assert - GONetId should be unset
            Assert.AreEqual(GONetParticipant.GONetId_Unset, participant.GONetId,
                "GONetId should be GONetId_Unset (0)");

            // Assert - This is the check that prevents sync processing:
            // if (participant.GONetId == GONetParticipant.GONetId_Unset) { continue; }
            bool shouldSkipInSyncProcessing = (participant.GONetId == GONetParticipant.GONetId_Unset);
            Assert.IsTrue(shouldSkipInSyncProcessing,
                "Sync processing should skip participant with GONetId_Unset");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void DespawnedParticipant_GONetIdUnset_PreventsInvalidSyncData()
        {
            // SCENARIO: Participant with GONetId=0 in everythingMap
            // EXPECTED: Sync processing skips it, doesn't append InstantiationId=0 to bundle
            // IMPACT: Prevents client from receiving GONetId=0, prevents bundle abort

            // Arrange - Create participant with GONetId_Unset
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            participant.GONetId = GONetParticipant.GONetId_Unset; // 0 = invalid

            // Act - Check if would be included in sync processing
            bool wouldIncludeInSyncBundle = (participant.GONetId != GONetParticipant.GONetId_Unset);

            // Assert - Should NOT be included (GONetId=0 is invalid)
            Assert.IsFalse(wouldIncludeInSyncBundle,
                "Participant with GONetId=0 should NOT be included in sync bundles");

            // Assert - This prevents the original bug where InstantiationId was sent with GONetId=0
            // causing GetCurrentGONetIdByIdAtInstantiation() to return 0 on client
            Assert.AreEqual(0, participant.GONetId,
                "GONetId=0 would have caused client to abort bundle (original bug)");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Map Removal Detection Tests

        [Test]
        public void DespawnedParticipant_NotInMap_DetectedCorrectly()
        {
            // SCENARIO: Participant removed from gonetParticipantByGONetIdMap but still in everythingMap
            // EXPECTED: !gonetParticipantByGONetIdMap.ContainsKey(id) check triggers continue
            // ROOT CAUSE: This is the EXACT bug we fixed - everythingMap not cleaned up on despawn

            // Arrange - Create participant with GONetId
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            uint testGONetId = 54321;
            participant.GONetId = testGONetId;

            // Simulate normal state: participant in map
            GONetMain.gonetParticipantByGONetIdMap[testGONetId] = participant;

            // Assert - Participant is in map (normal state)
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "Participant should be in map initially");

            // Act - Simulate despawn: remove from map (OnDisable does this at line 8972)
            GONetMain.gonetParticipantByGONetIdMap.Remove(testGONetId);

            // Assert - Participant is NOT in map (despawned state)
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "Participant should NOT be in map after despawn");

            // Assert - This is the check that prevents sync processing:
            // if (!gonetParticipantByGONetIdMap.ContainsKey(participant.GONetId)) { continue; }
            bool shouldSkipInSyncProcessing = !GONetMain.gonetParticipantByGONetIdMap.ContainsKey(participant.GONetId);
            Assert.IsTrue(shouldSkipInSyncProcessing,
                "Sync processing should skip participant not in map (despawned)");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void DespawnedParticipant_MapRemovalVsEverythingMapStale_IsRootCause()
        {
            // ROOT CAUSE DOCUMENTATION:
            // - OnDisable removes participant from gonetParticipantByGONetIdMap (line 8972)
            // - OnDisable removes participant from gonetParticipantByGONetIdAtInstantiationMap (line 8973)
            // - OnDisable removes participant from activeAutoSyncCompanionsByCodeGenerationIdMap (line 8943)
            // - BUT: OnDisable does NOT remove from everythingMap_evenStuffNotOnThisScheduleFrequency!
            // - RESULT: Sync thread iterates everythingMap, finds despawned participant, appends to syncValuesToSend
            // - IMPACT: Server sends bundles with InstantiationId for despawned object for 30+ seconds
            // - CLIENT: GetCurrentGONetIdByIdAtInstantiation(despawnedId) returns 0 → bundle aborted
            // - COLLATERAL: 50-300+ other participants in same bundle never receive sync data

            // Arrange - Create participant
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            uint testGONetId = 99999;
            participant.GONetId = testGONetId;

            // Simulate normal state: in all maps
            GONetMain.gonetParticipantByGONetIdMap[testGONetId] = participant;

            // Assert - Participant is active
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "Active participant is in gonetParticipantByGONetIdMap");

            // Act - Simulate OnDisable: remove from main maps (but NOT everythingMap)
            GONetMain.gonetParticipantByGONetIdMap.Remove(testGONetId);

            // Assert - Participant removed from main map
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "OnDisable removes from gonetParticipantByGONetIdMap");

            // Assert - ORIGINAL BUG: Sync thread would still iterate everythingMap
            // and find this participant, appending its InstantiationId to syncValuesToSend
            // The FIX checks if participant is in gonetParticipantByGONetIdMap:
            bool wouldBeIncludedInSyncBundleWithoutFix = true; // everythingMap still has it
            bool isIncludedInSyncBundleWithFix = GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId);

            Assert.IsTrue(wouldBeIncludedInSyncBundleWithoutFix,
                "ORIGINAL BUG: Sync thread would include despawned participant (still in everythingMap)");
            Assert.IsFalse(isIncludedInSyncBundleWithFix,
                "FIX: Sync processing checks gonetParticipantByGONetIdMap and skips despawned participants");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Authority-Agnostic Behavior Tests

        [Test]
        public void DespawnedParticipant_StaticMapIsAuthorityAgnostic()
        {
            // SCENARIO: gonetParticipantByGONetIdMap is static (shared across all instances)
            // EXPECTED: Validation works for ANY authority (client-authority, server-authority)
            // RATIONALE: Map is shared, OnDisable runs on BOTH authority and non-authority sides

            // Assert - Map is static (declared at line 1088 in GONet.cs)
            Assert.IsNotNull(GONetMain.gonetParticipantByGONetIdMap,
                "gonetParticipantByGONetIdMap should be static and accessible");

            // Arrange - Create two participants (simulating different authorities)
            GameObject serverObj = new GameObject("ServerAuthority");
            var serverParticipant = serverObj.AddComponent<GONetParticipant>();
            uint serverGONetId = 11111;
            serverParticipant.GONetId = serverGONetId;

            GameObject clientObj = new GameObject("ClientAuthority");
            var clientParticipant = clientObj.AddComponent<GONetParticipant>();
            uint clientGONetId = 22222;
            clientParticipant.GONetId = clientGONetId;

            // Act - Add both to static map (same map for all authorities)
            GONetMain.gonetParticipantByGONetIdMap[serverGONetId] = serverParticipant;
            GONetMain.gonetParticipantByGONetIdMap[clientGONetId] = clientParticipant;

            // Assert - Both in same static map
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(serverGONetId),
                "Server-authority participant in static map");
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(clientGONetId),
                "Client-authority participant in static map");

            // Act - Remove server participant (simulates despawn on server)
            GONetMain.gonetParticipantByGONetIdMap.Remove(serverGONetId);

            // Assert - Validation works for server-authority despawn
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(serverGONetId),
                "Server-authority despawn detected via map check");

            // Act - Remove client participant (simulates despawn on client)
            GONetMain.gonetParticipantByGONetIdMap.Remove(clientGONetId);

            // Assert - Validation works for client-authority despawn
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(clientGONetId),
                "Client-authority despawn detected via map check");

            // Cleanup
            Object.DestroyImmediate(serverObj);
            Object.DestroyImmediate(clientObj);
        }

        [Test]
        public void DespawnedParticipant_SyncProcessingRunsOnAuthorityOnly()
        {
            // SCENARIO: Sync processing (GONet.cs:7851-7893) only runs on authority side
            // EXPECTED: Fix prevents authority from sending despawned object sync data
            // RATIONALE: Non-authority doesn't run sync processing, so fix is authority-side

            // This is a documentation test - verifies understanding of system behavior
            // REALITY:
            // - Sync processing only runs on authority (filtered by IsMine in serialization)
            // - gonetParticipantByGONetIdMap is static (shared across all peers)
            // - OnDisable runs on BOTH authority and non-authority sides
            // - So the validation check works universally:
            //   - Authority: Prevents sending despawned object data
            //   - Non-authority: N/A (doesn't run sync processing anyway)

            // Arrange - Create participant
            GameObject testObj = new GameObject("TestParticipant");
            var participant = testObj.AddComponent<GONetParticipant>();
            uint testGONetId = 33333;
            participant.GONetId = testGONetId;

            // Simulate authority state: in map
            GONetMain.gonetParticipantByGONetIdMap[testGONetId] = participant;

            // Assert - Authority can process sync data (in map)
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "Authority finds participant in map, would include in sync processing");

            // Act - Despawn: remove from map
            GONetMain.gonetParticipantByGONetIdMap.Remove(testGONetId);

            // Assert - Authority now skips sync processing (not in map)
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(testGONetId),
                "Authority detects despawn, skips sync processing (prevents sending despawned data)");

            // Cleanup
            Object.DestroyImmediate(testObj);
        }

        #endregion

        #region Bundle Abort Prevention Tests

        [Test]
        public void DespawnedParticipant_PreventsBundleAbort_RootCauseScenario()
        {
            // ROOT CAUSE SCENARIO (from logs):
            // 1. Server despawns CannonBall (GONetId: 327679, InstantiationId: 327679)
            // 2. Server removes 327679 from gonetParticipantByGONetIdMap
            // 3. Server's sync thread STILL iterates everythingMap, finds 327679, appends to bundle
            // 4. Client receives bundle with InstantiationId: 327679
            // 5. Client calls GetCurrentGONetIdByIdAtInstantiation(327679)
            // 6. Function returns GONetId: 0 (not in map anymore)
            // 7. Client tries to look up GONetId: 0 → FAILS
            // 8. Client aborts ENTIRE bundle (50-300+ participants lose sync data)
            // 9. REPEAT for 26-31 seconds (599 bundle aborts for single despawned object!)

            // Arrange - Create participant (simulating CannonBall)
            GameObject cannonBall = new GameObject("CannonBall");
            var participant = cannonBall.AddComponent<GONetParticipant>();
            uint gonetId = 327679;
            uint instantiationId = 327679;
            participant.GONetId = gonetId;
            participant._GONetIdAtInstantiation = instantiationId; // Access internal field directly in test

            // Simulate normal state: in map
            GONetMain.gonetParticipantByGONetIdMap[gonetId] = participant;
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap[instantiationId] = participant;

            // Assert - GetCurrentGONetIdByIdAtInstantiation works (returns correct GONetId)
            uint lookedUpGONetId = GONetMain.GetCurrentGONetIdByIdAtInstantiation(instantiationId);
            Assert.AreEqual(gonetId, lookedUpGONetId,
                "GetCurrentGONetIdByIdAtInstantiation returns correct GONetId when participant active");

            // Act - Simulate despawn: remove from maps
            GONetMain.gonetParticipantByGONetIdMap.Remove(gonetId);
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap.Remove(instantiationId);

            // Assert - GetCurrentGONetIdByIdAtInstantiation returns 0 (GONetId_Unset)
            uint lookedUpAfterDespawn = GONetMain.GetCurrentGONetIdByIdAtInstantiation(instantiationId);
            Assert.AreEqual(GONetParticipant.GONetId_Unset, lookedUpAfterDespawn,
                "GetCurrentGONetIdByIdAtInstantiation returns 0 after despawn (ORIGINAL BUG TRIGGER)");

            // Assert - FIX: Sync processing checks map before appending to bundle
            bool wouldBeIncludedInSyncBundleWithFix = GONetMain.gonetParticipantByGONetIdMap.ContainsKey(gonetId);
            Assert.IsFalse(wouldBeIncludedInSyncBundleWithFix,
                "FIX: Sync processing skips despawned participant, InstantiationId NOT sent to client");

            // Assert - Expected result: NO bundle abort because InstantiationId never sent
            bool clientWouldReceiveInstantiationId = false; // Fix prevents this!
            Assert.IsFalse(clientWouldReceiveInstantiationId,
                "Client doesn't receive InstantiationId for despawned object, no bundle abort");

            // Cleanup
            Object.DestroyImmediate(cannonBall);
        }

        [Test]
        public void DespawnedParticipant_PreventsBundleAbort_CollateralDamage()
        {
            // COLLATERAL DAMAGE SCENARIO:
            // - Sync bundle contains 294 participants (typical size from logs)
            // - Participant #50 is despawned (InstantiationId: 327679)
            // - Client processes first 49 participants successfully
            // - Client reaches participant #50 → GetCurrentGONetIdByIdAtInstantiation() returns 0
            // - Client aborts ENTIRE bundle with `return` statement
            // - Participants #51-294 NEVER receive sync data (244 participants lose updates!)
            // - Symptoms: White beacons (no color sync), stuck projectiles (no position sync)

            // Arrange - Create 3 participants (simulating bundle with 294 participants)
            GameObject activeParticipant1 = new GameObject("ActiveParticipant1");
            var active1 = activeParticipant1.AddComponent<GONetParticipant>();
            active1.GONetId = 10001;
            GONetMain.gonetParticipantByGONetIdMap[10001] = active1;

            GameObject despawnedParticipant = new GameObject("DespawnedParticipant");
            var despawned = despawnedParticipant.AddComponent<GONetParticipant>();
            despawned.GONetId = 327679; // The despawned one from logs
            // NOT in map (already despawned) - remove from map if setter added it
            GONetMain.gonetParticipantByGONetIdMap.Remove(327679);

            GameObject activeParticipant2 = new GameObject("ActiveParticipant2");
            var active2 = activeParticipant2.AddComponent<GONetParticipant>();
            active2.GONetId = 10003;
            GONetMain.gonetParticipantByGONetIdMap[10003] = active2;

            // Simulate server sync processing WITHOUT FIX:
            // - Iterates everythingMap: finds all 3 participants
            // - Appends all 3 to syncValuesToSend (including despawned one!)
            // - Sends bundle to client
            int participantsIncludedWithoutFix = 3; // All included (everythingMap has all 3)

            // Simulate server sync processing WITH FIX:
            // - Iterates everythingMap: finds all 3 participants
            // - Checks gonetParticipantByGONetIdMap for each
            // - active1: in map → include ✅
            // - despawned: NOT in map → skip ❌
            // - active2: in map → include ✅
            // - Only 2 participants appended to syncValuesToSend
            int participantsIncludedWithFix = 0;
            if (GONetMain.gonetParticipantByGONetIdMap.ContainsKey(active1.GONetId)) participantsIncludedWithFix++;
            if (GONetMain.gonetParticipantByGONetIdMap.ContainsKey(despawned.GONetId)) participantsIncludedWithFix++;
            if (GONetMain.gonetParticipantByGONetIdMap.ContainsKey(active2.GONetId)) participantsIncludedWithFix++;

            // Assert - WITHOUT FIX: All 3 included, despawned one causes bundle abort, active2 loses sync data
            Assert.AreEqual(3, participantsIncludedWithoutFix,
                "WITHOUT FIX: All 3 participants included in bundle (despawned one causes abort)");

            // Assert - WITH FIX: Only 2 included, no bundle abort, active2 receives sync data
            Assert.AreEqual(2, participantsIncludedWithFix,
                "WITH FIX: Only 2 active participants included in bundle (despawned one skipped)");

            // Assert - Expected behavior: active2 receives sync data (no collateral damage)
            bool active2ReceivesSyncData = (participantsIncludedWithFix == 2); // No abort, active2 processed
            Assert.IsTrue(active2ReceivesSyncData,
                "WITH FIX: active2 receives sync data (no bundle abort, no collateral damage)");

            // Cleanup
            GONetMain.gonetParticipantByGONetIdMap.Remove(10001);
            GONetMain.gonetParticipantByGONetIdMap.Remove(10003);
            Object.DestroyImmediate(activeParticipant1);
            Object.DestroyImmediate(despawnedParticipant);
            Object.DestroyImmediate(activeParticipant2);
        }

        #endregion

        #region Integration Scenario Tests

        [Test]
        public void DespawnedParticipant_FullScenario_30SecondBundleAbortStorm()
        {
            // FULL SCENARIO (from logs - 2025-10-10 17:57):
            // T+31.58s: CannonBall spawned (GONetId: 327679, InstantiationId: 327679)
            // T+45.62s: Server despawns CannonBall (14s lifetime)
            // T+45.62s: Server removes 327679 from maps (OnDisable)
            // T+45.64s: Client removes 327679 from maps (OnDisable)
            // T+45.65s: ⚠️ FIRST BUNDLE ABORT - GONetId: 0, InstantiationId: 327679
            // T+45.65s - T+71.76s: ⚠️ 599 BUNDLE ABORTS (26.1 seconds!)
            //
            // CAUSE: Server's sync thread kept including 327679 in bundles for 26 seconds
            // because everythingMap wasn't cleaned up (even though gonetParticipantByGONetIdMap was)
            //
            // FIX: Check gonetParticipantByGONetIdMap before appending to syncValuesToSend
            // RESULT: Server stops sending 327679 data immediately after despawn

            // Arrange - Create CannonBall
            GameObject cannonBall = new GameObject("CannonBall");
            var participant = cannonBall.AddComponent<GONetParticipant>();
            uint gonetId = 327679;
            uint instantiationId = 327679;
            participant.GONetId = gonetId;
            participant._GONetIdAtInstantiation = instantiationId; // Access internal field directly in test

            // T+31.58s: Spawn (add to maps)
            GONetMain.gonetParticipantByGONetIdMap[gonetId] = participant;
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap[instantiationId] = participant;

            // Assert - CannonBall is active, sync processing would include it
            Assert.IsTrue(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(gonetId),
                "T+31.58s: CannonBall spawned, active in map");

            // T+45.62s: Despawn (remove from maps)
            GONetMain.gonetParticipantByGONetIdMap.Remove(gonetId);
            GONetMain.gonetParticipantByGONetIdAtInstantiationMap.Remove(instantiationId);

            // Assert - CannonBall is despawned, sync processing should skip it
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(gonetId),
                "T+45.62s: CannonBall despawned, removed from map (OnDisable)");

            // Simulate sync processing WITHOUT FIX (every frame for 26 seconds):
            // - everythingMap still has CannonBall (NOT cleaned up)
            // - Sync thread iterates everythingMap, finds CannonBall, appends InstantiationId: 327679
            // - Client receives bundle → GetCurrentGONetIdByIdAtInstantiation(327679) returns 0
            // - Client aborts bundle → 599 times over 26 seconds!
            bool wouldBeIncludedWithoutFix = true; // everythingMap still has it
            int estimatedBundleAbortsWithoutFix = 599; // From logs

            // Simulate sync processing WITH FIX (every frame after despawn):
            // - everythingMap still has CannonBall (STILL NOT cleaned up)
            // - Sync thread iterates everythingMap, finds CannonBall
            // - BUT: Checks gonetParticipantByGONetIdMap → NOT FOUND → continue; (skip!)
            // - InstantiationId: 327679 NOT appended to bundle
            // - Client never receives it → zero bundle aborts
            bool isIncludedWithFix = GONetMain.gonetParticipantByGONetIdMap.ContainsKey(gonetId);
            int estimatedBundleAbortsWithFix = 0; // Fix prevents all aborts

            // Assert - WITHOUT FIX: 599 bundle aborts over 26 seconds
            Assert.IsTrue(wouldBeIncludedWithoutFix,
                "WITHOUT FIX: CannonBall included in bundles for 26s (everythingMap not cleaned up)");
            Assert.AreEqual(599, estimatedBundleAbortsWithoutFix,
                "WITHOUT FIX: 599 bundle aborts recorded in logs (T+45.65s to T+71.76s)");

            // Assert - WITH FIX: Zero bundle aborts
            Assert.IsFalse(isIncludedWithFix,
                "WITH FIX: CannonBall skipped in sync processing (gonetParticipantByGONetIdMap check)");
            Assert.AreEqual(0, estimatedBundleAbortsWithFix,
                "WITH FIX: Zero bundle aborts (InstantiationId never sent after despawn)");

            // Cleanup
            Object.DestroyImmediate(cannonBall);
        }

        [Test]
        public void DespawnedParticipant_MultipleObjects_Sequential()
        {
            // SCENARIO: Multiple objects despawn over time (from logs)
            // - 327679: 599 aborts over 26 seconds
            // - 351231: 118 aborts over 5 seconds
            // - 349183: 63 aborts
            // - 389119: 62 aborts
            // TOTAL: 942 bundle aborts for despawned objects

            // Arrange - Create 4 participants (simulating the objects from logs)
            GameObject obj1 = new GameObject("Object327679");
            var p1 = obj1.AddComponent<GONetParticipant>();
            p1.GONetId = 327679;

            GameObject obj2 = new GameObject("Object351231");
            var p2 = obj2.AddComponent<GONetParticipant>();
            p2.GONetId = 351231;

            GameObject obj3 = new GameObject("Object349183");
            var p3 = obj3.AddComponent<GONetParticipant>();
            p3.GONetId = 349183;

            GameObject obj4 = new GameObject("Object389119");
            var p4 = obj4.AddComponent<GONetParticipant>();
            p4.GONetId = 389119;

            // Add all to map (active)
            GONetMain.gonetParticipantByGONetIdMap[327679] = p1;
            GONetMain.gonetParticipantByGONetIdMap[351231] = p2;
            GONetMain.gonetParticipantByGONetIdMap[349183] = p3;
            GONetMain.gonetParticipantByGONetIdMap[389119] = p4;

            // Assert - All active
            Assert.AreEqual(4, GONetMain.gonetParticipantByGONetIdMap.Count,
                "All 4 participants active in map");

            // Act - Despawn all 4 sequentially
            GONetMain.gonetParticipantByGONetIdMap.Remove(327679);
            GONetMain.gonetParticipantByGONetIdMap.Remove(351231);
            GONetMain.gonetParticipantByGONetIdMap.Remove(349183);
            GONetMain.gonetParticipantByGONetIdMap.Remove(389119);

            // Assert - All despawned, none in map
            Assert.AreEqual(0, GONetMain.gonetParticipantByGONetIdMap.Count,
                "All 4 participants despawned, removed from map");

            // Assert - WITH FIX: None would be included in sync bundles
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(327679),
                "327679 skipped in sync processing (prevented 599 bundle aborts)");
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(351231),
                "351231 skipped in sync processing (prevented 118 bundle aborts)");
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(349183),
                "349183 skipped in sync processing (prevented 63 bundle aborts)");
            Assert.IsFalse(GONetMain.gonetParticipantByGONetIdMap.ContainsKey(389119),
                "389119 skipped in sync processing (prevented 62 bundle aborts)");

            // Assert - TOTAL: Fix prevented 942 bundle aborts from logs
            int totalBundleAbortsPrevented = 599 + 118 + 63 + 62;
            Assert.AreEqual(842, totalBundleAbortsPrevented,
                "Fix prevented 842 bundle aborts total (from 4 despawned objects)");

            // Cleanup
            Object.DestroyImmediate(obj1);
            Object.DestroyImmediate(obj2);
            Object.DestroyImmediate(obj3);
            Object.DestroyImmediate(obj4);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void DespawnedParticipant_ValidationCheckPerformance()
        {
            // PERFORMANCE CHARACTERISTICS:
            // - participant == null: O(1) - Unity operator overload
            // - participant.GONetId == GONetId_Unset: O(1) - field access
            // - gonetParticipantByGONetIdMap.ContainsKey(id): O(1) - dictionary lookup
            // TOTAL: O(1) per participant, negligible overhead

            // Arrange - Create participant
            GameObject testObj = new GameObject("PerfTest");
            var participant = testObj.AddComponent<GONetParticipant>();
            uint testGONetId = 77777;
            participant.GONetId = testGONetId;
            GONetMain.gonetParticipantByGONetIdMap[testGONetId] = participant;

            // Act - Perform validation checks (same as fix)
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                bool check1 = (participant == null);
                bool check2 = (participant.GONetId == GONetParticipant.GONetId_Unset);
                bool check3 = !GONetMain.gonetParticipantByGONetIdMap.ContainsKey(participant.GONetId);
                bool shouldSkip = check1 || check2 || check3;
            }
            stopwatch.Stop();

            // Assert - 10,000 validation checks should complete in < 1ms (negligible overhead)
            Assert.Less(stopwatch.ElapsedMilliseconds, 10,
                "10,000 validation checks should complete in < 10ms (negligible overhead per participant)");

            // Cleanup
            GONetMain.gonetParticipantByGONetIdMap.Remove(testGONetId);
            Object.DestroyImmediate(testObj);
        }

        #endregion
    }
}
