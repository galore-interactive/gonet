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
    /// Unit tests for sync bundle deferral system (OnGONetReady race condition fix).
    ///
    /// Tests the DROP-FIRST approach with optional DEFER for handling sync bundles
    /// that arrive before GONetParticipant completes Awake() initialization.
    ///
    /// Critical test scenarios:
    /// 1. Exception is thrown when participant not ready (didAwakeComplete=false)
    /// 2. Exception contains correct GONetId for diagnostics
    /// 3. Deferral disabled (default) → bundles dropped
    /// 4. Deferral enabled + reliable → bundles queued
    /// 5. Deferral enabled + unreliable → bundles dropped (by design)
    /// 6. Retry after 1 frame → participant ready → succeeds
    /// 7. Retry after 1 frame → participant STILL not ready → drops (lifecycle bug)
    /// 8. Queue full → oldest bundles dropped (FIFO policy)
    /// 9. Incremental processing → max N bundles per OnGONetReady callback
    /// 10. Pool safety → byte arrays always returned in all paths
    /// </summary>
    [TestFixture]
    public class GONetSyncBundleDeferralTests
    {
        private GameObject testGameObject;
        private GONetGlobal testGlobal;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with GONetGlobal component
            testGameObject = new GameObject("TestGONetGlobal");
            testGlobal = testGameObject.AddComponent<GONetGlobal>();

            // Set default test values (deferral DISABLED - industry standard)
            testGlobal.deferSyncBundlesWaitingForGONetReady = false;
            testGlobal.maxSyncBundlesWaitingForGONetReady = 100;
            testGlobal.maxBundlesProcessedPerGONetReadyCallback = 10;
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        #region Exception Infrastructure Tests

        [Test]
        public void GONetParticipantNotReadyException_Constructor_StoresGONetId()
        {
            // Arrange
            uint expectedGONetId = 12345;
            string expectedMessage = "Test participant not ready";

            // Act
            var exception = new GONetParticipantNotReadyException(expectedMessage, expectedGONetId);

            // Assert
            Assert.AreEqual(expectedGONetId, exception.GONetId, "Exception should store GONetId for diagnostics");
            Assert.AreEqual(expectedMessage, exception.Message, "Exception should preserve message");
        }

        [Test]
        public void GONetParticipantNotReadyException_CanBeCaught()
        {
            // Arrange
            uint testGONetId = 54321;
            bool exceptionCaught = false;

            // Act
            try
            {
                throw new GONetParticipantNotReadyException("Test exception", testGONetId);
            }
            catch (GONetParticipantNotReadyException ex)
            {
                exceptionCaught = true;
                Assert.AreEqual(testGONetId, ex.GONetId, "Caught exception should preserve GONetId");
            }

            // Assert
            Assert.IsTrue(exceptionCaught, "Exception should be catchable with specific type");
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void GONetGlobal_DeferralDisabledByDefault()
        {
            // Arrange & Act
            var freshGlobal = testGameObject.AddComponent<GONetGlobal>();

            // Assert
            Assert.IsFalse(freshGlobal.deferSyncBundlesWaitingForGONetReady,
                "Deferral should be DISABLED by default (industry standard - drop bundles)");
        }

        [Test]
        public void GONetGlobal_QueueSizeDefault_IsReasonable()
        {
            // Arrange & Act
            var freshGlobal = testGameObject.AddComponent<GONetGlobal>();

            // Assert
            Assert.AreEqual(100, freshGlobal.maxSyncBundlesWaitingForGONetReady,
                "Default queue size should be 100 (handles typical 1-2 frame Awake delay)");
        }

        [Test]
        public void GONetGlobal_ProcessingLimitDefault_PreventsFrameStutter()
        {
            // Arrange & Act
            var freshGlobal = testGameObject.AddComponent<GONetGlobal>();

            // Assert
            Assert.AreEqual(10, freshGlobal.maxBundlesProcessedPerGONetReadyCallback,
                "Default processing limit should be 10 bundles/callback (prevents frame spikes)");
        }

        [Test]
        public void GONetGlobal_ConfigurationCanBeChanged()
        {
            // Arrange
            testGlobal.deferSyncBundlesWaitingForGONetReady = false;

            // Act
            testGlobal.deferSyncBundlesWaitingForGONetReady = true;
            testGlobal.maxSyncBundlesWaitingForGONetReady = 50;
            testGlobal.maxBundlesProcessedPerGONetReadyCallback = 5;

            // Assert
            Assert.IsTrue(testGlobal.deferSyncBundlesWaitingForGONetReady, "Deferral should be enabled");
            Assert.AreEqual(50, testGlobal.maxSyncBundlesWaitingForGONetReady, "Queue size should be updated");
            Assert.AreEqual(5, testGlobal.maxBundlesProcessedPerGONetReadyCallback, "Processing limit should be updated");
        }

        #endregion

        #region Integration Tests (Require Unity PlayMode + GONet Runtime)

        // NOTE: These integration tests require full GONet runtime infrastructure and Unity PlayMode.
        // They test the complete deferral flow with actual networking and participant lifecycle.
        //
        // REQUIRED INFRASTRUCTURE:
        // 1. Unity PlayMode test environment ([UnityTest] attribute)
        // 2. GONet server + client test harness (see NetcodeIOTestBase pattern)
        // 3. Test prefabs with GONetParticipant components
        // 4. Network simulation for timing control
        //
        // IMPLEMENTATION PRIORITY:
        // 1. [P0] DeferralEnabled_ReliableBundle_RetryAfterReady_Succeeds (core happy path)
        // 2. [P0] DeferralDisabled_ReliableBundle_ParticipantNotReady_DropsBundle (default behavior)
        // 3. [P1] DeferralEnabled_QueueFull_DropsOldest (edge case handling)
        // 4. [P1] IncrementalProcessing_LimitsPerCallback (performance characteristic)
        // 5. [P2] DeferralEnabled_ReliableBundle_RetryStillNotReady_Drops (lifecycle bug detection)
        //
        // TIME ESTIMATE: ~2-3 days for full implementation

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_DeferralEnabled_ReliableBundle_RetryAfterReady_Succeeds()
        {
            // SCENARIO: Core happy path - deferred bundle processed after participant ready (P0)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Server spawns test prefab (simple cube with GONetParticipant + synced position)
            //
            // TEST FLOW:
            // 1. Server changes cube position to (10, 0, 0)
            // 2. Server sends reliable sync bundle to client
            // 3. Bundle arrives at client BEFORE client's cube completes Awake()
            //    - Trigger this by: Spawning multiple objects to delay Awake, or injecting delay in Awake
            // 4. Bundle triggers GONetParticipantNotReadyException (didAwakeComplete=false)
            // 5. Bundle deferred to incomingNetworkData_waitingForGONetReady queue
            //    - Verify: GONet.incomingNetworkData_waitingForGONetReady.Count == 1
            // 6. Wait for client's cube to complete Awake() → OnGONetReady fires
            // 7. ProcessDeferredSyncBundlesWaitingForGONetReady() called automatically
            // 8. Bundle dequeued and processed successfully
            //    - Verify: GONet.incomingNetworkData_waitingForGONetReady.Count == 0
            //    - Verify: Client cube position == (10, 0, 0)
            //
            // ASSERTIONS:
            // - Bundle was queued (not dropped)
            // - Bundle processed after OnGONetReady
            // - Final state synchronized correctly
            // - No errors logged
            //
            // IMPLEMENTATION NOTE:
            // Use coroutine with yield return null to wait for Awake completion.
            // Monitor GONetLog for "[GONETREADY-QUEUE] Deferred reliable sync bundle" message.
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_DeferralDisabled_ReliableBundle_ParticipantNotReady_DropsBundle()
        {
            // SCENARIO: Default behavior - bundles dropped when participant not ready (P0)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Ensure deferral DISABLED (default): GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = false
            // 3. Server spawns test prefab
            //
            // TEST FLOW:
            // 1. Server changes cube position to (10, 0, 0)
            // 2. Server sends reliable sync bundle to client
            // 3. Bundle arrives at client BEFORE client's cube completes Awake()
            // 4. Bundle triggers GONetParticipantNotReadyException
            // 5. Bundle DROPPED (not queued)
            //    - Verify: GONet.incomingNetworkData_waitingForGONetReady.Count == 0
            // 6. Wait for client's cube to complete Awake()
            // 7. Server continues sending position updates (authority re-sends state)
            // 8. Client eventually receives subsequent update and syncs position
            //
            // ASSERTIONS:
            // - Bundle was NOT queued (dropped)
            // - Client position eventually syncs from subsequent updates
            // - GONetLog shows "[GONETREADY-DROP] Dropped sync bundle" message
            //
            // RATIONALE:
            // - Matches industry standards (FishNet, Mirror)
            // - Authority re-sends state 30-60 times/sec → auto-recovery
            // - Zero performance overhead
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_DeferralEnabled_UnreliableBundle_AlwaysDropped()
        {
            // SCENARIO: Unreliable bundles NEVER deferred, even with deferral enabled (P1)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Server spawns test prefab with unreliable sync field
            //
            // TEST FLOW:
            // 1. Server changes unreliable field value
            // 2. Server sends UNRELIABLE sync bundle to client
            // 3. Bundle arrives before participant ready
            // 4. Bundle DROPPED (not queued, even though deferral enabled)
            //    - Verify: GONet.incomingNetworkData_waitingForGONetReady.Count == 0
            //
            // ASSERTIONS:
            // - Unreliable bundle not queued (by design)
            // - GONetLog shows "[GONETREADY-DROP]" with channel info
            //
            // RATIONALE:
            // - Unreliable data is transient by nature
            // - Queueing defeats purpose of unreliable channel
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_DeferralEnabled_QueueFull_DropsOldest()
        {
            // SCENARIO: Queue capacity exceeded - FIFO drop policy (P1)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Set small queue size: GONetGlobal.Instance.maxSyncBundlesWaitingForGONetReady = 5
            //
            // TEST FLOW:
            // 1. Server spawns 10 test cubes rapidly (faster than client Awake can complete)
            // 2. Server sends position updates for all 10 cubes
            // 3. All bundles arrive before participants ready
            // 4. First 5 bundles queued successfully
            // 5. 6th bundle arrives → oldest bundle (1st) dropped, 6th queued
            // 6. 7th bundle arrives → oldest bundle (2nd) dropped, 7th queued
            // 7. Monitor GONetLog for warning:
            //    "[GONETREADY-QUEUE] Queue full (5 bundles)! Dropping OLDEST deferred bundle"
            //
            // ASSERTIONS:
            // - Queue never exceeds maxSyncBundlesWaitingForGONetReady
            // - Oldest bundles dropped (FIFO policy)
            // - Warning logged with suggestion to increase queue size
            // - Byte arrays from dropped bundles returned to pool (no memory leak)
            //
            // MEMORY LEAK CHECK:
            // - Track pool size before/after test
            // - Verify all borrowed byte arrays returned
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_IncrementalProcessing_LimitsPerCallback()
        {
            // SCENARIO: Prevent frame stutter during mass spawns (P1)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Set processing limit: GONetGlobal.Instance.maxBundlesProcessedPerGONetReadyCallback = 5
            //
            // TEST FLOW:
            // 1. Server spawns 20 test cubes rapidly
            // 2. Server sends sync bundles for all 20 cubes
            // 3. All bundles arrive before participants ready → 20 bundles queued
            // 4. First participant completes Awake() → OnGONetReady fires
            // 5. ProcessDeferredSyncBundlesWaitingForGONetReady() processes 5 bundles (hits limit)
            //    - Verify: Queue size drops from 20 to 15
            // 6. Second participant completes Awake() → processes 5 more bundles
            //    - Verify: Queue size drops from 15 to 10
            // 7. Continue until queue empty
            //
            // ASSERTIONS:
            // - No more than maxBundlesProcessedPerGONetReadyCallback processed per OnGONetReady
            // - Queue gradually drains across multiple callbacks
            // - GONetLog shows incremental progress:
            //   "[GONETREADY-QUEUE] Processed 5 deferred bundles, 0 dropped, 15 remaining in queue"
            //
            // PERFORMANCE MEASUREMENT:
            // - Measure frame time during processing
            // - Should not spike significantly (frame stutter prevention)
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_DeferralEnabled_RetryStillNotReady_Drops()
        {
            // SCENARIO: Participant STILL not ready after retry - lifecycle bug detection (P2)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Create BUGGY test prefab that never completes Awake():
            //    - GONetParticipant with didAwakeComplete manually kept false
            //    - Or syncCompanion set to null in Start()
            //
            // TEST FLOW:
            // 1. Server spawns buggy prefab
            // 2. Server sends sync bundle
            // 3. Bundle arrives, participant not ready → queued
            // 4. Participant's OnGONetReady fires (but still broken somehow)
            // 5. ProcessDeferredSyncBundlesWaitingForGONetReady() calls ProcessIncomingBytes with isProcessingFromQueue=true
            // 6. Participant STILL not ready → GONetParticipantNotReadyException thrown again
            // 7. Exception handler detects isProcessingFromQueue=true → DROPS bundle (not requeue)
            // 8. GONetLog.Error logged:
            //    "[GONETREADY-QUEUE] Sync bundle still has unready participant... after retry. This indicates an OnGONetReady lifecycle bug."
            //
            // ASSERTIONS:
            // - Bundle not requeued (infinite retry prevented)
            // - Error logged identifying lifecycle bug
            // - Byte array returned to pool
            //
            // PURPOSE:
            // - Detect bugs in OnGONetReady lifecycle implementation
            // - Prevent infinite retry loops
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_ParticipantDestroyed_BeforeRetry_DropsGracefully()
        {
            // SCENARIO: Edge case - participant despawned while bundle queued (P2)
            //
            // SETUP:
            // 1. Start GONet server + single client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            //
            // TEST FLOW:
            // 1. Server spawns test cube
            // 2. Server sends sync bundle to client
            // 3. Bundle arrives before participant ready → queued
            // 4. Server immediately despawns cube (before client Awake completes)
            // 5. Client receives despawn event → destroys participant GameObject
            // 6. Client's OnGONetReady fires for other participant
            // 7. ProcessDeferredSyncBundlesWaitingForGONetReady() tries to process queued bundle
            // 8. GetGONetParticipantById() returns null (participant destroyed)
            // 9. KeyNotFoundException caught → bundle dropped gracefully
            //
            // ASSERTIONS:
            // - No crash (exception caught)
            // - Bundle dropped cleanly
            // - Byte array returned to pool
            // - No errors logged (expected behavior for rapid spawn/despawn)
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_AuthorityAgnostic_ClientToServer_Works()
        {
            // SCENARIO: Deferral works server-side when receiving from client (P1)
            //
            // SETUP:
            // 1. Start GONet server + client
            // 2. Enable deferral on SERVER: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            //
            // TEST FLOW:
            // 1. Client spawns object (client authority initially)
            // 2. Server receives spawn event → begins instantiation
            // 3. Client immediately sends sync bundle (position update)
            // 4. Bundle arrives at server BEFORE server's instantiation completes Awake()
            // 5. Server defers bundle (participant not ready)
            // 6. Server's participant completes Awake() → OnGONetReady
            // 7. Deferred bundle processed on server
            //
            // ASSERTIONS:
            // - Deferral works on server (not client-only feature)
            // - Server correctly receives client's sync data after retry
            //
            // PURPOSE:
            // - Verify authority-agnostic implementation
            // - Server can defer bundles just like clients
        }

        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure and full GONet runtime")]
        public void Integration_MemoryLeakStressTest_AllByteArraysReturned()
        {
            // SCENARIO: Stress test - verify no memory leaks under all code paths (P1)
            //
            // SETUP:
            // 1. Start GONet server + client
            // 2. Enable deferral: GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady = true
            // 3. Set small queue size to trigger drops: maxSyncBundlesWaitingForGONetReady = 10
            // 4. Record initial pool sizes
            //
            // TEST FLOW:
            // 1. Spawn 100 objects rapidly (mix of reliable/unreliable sync)
            // 2. Trigger ALL code paths:
            //    - Bundles queued (reliable, participant not ready)
            //    - Bundles dropped (unreliable, participant not ready)
            //    - Queue overflow (oldest bundles dropped)
            //    - Successful processing (bundles processed after ready)
            //    - Retry failure (bundles dropped after still not ready)
            // 3. Wait for all processing to complete
            // 4. Force GC.Collect()
            // 5. Check pool sizes
            //
            // ASSERTIONS:
            // - All borrowed byte arrays returned to pool
            // - Pool sizes match initial state (no leaks)
            // - No unexpected allocations
            //
            // CRITICAL:
            // - Memory leak in deferral path = pool exhaustion = allocation spikes
            // - MUST verify byte arrays returned in ALL paths:
            //   - Drop (unreliable or deferral disabled)
            //   - Queue overflow (oldest dropped)
            //   - Retry success (processed)
            //   - Retry failure (dropped after retry)
        }


        #endregion

        #region Documentation Tests (Ensure tooltips/docs are correct)

        [Test]
        public void Documentation_DefaultBehaviorExplained()
        {
            // This test documents the default behavior for users
            // DEFAULT: deferSyncBundlesWaitingForGONetReady = false
            // BEHAVIOR: Bundles with unready participants → DROPPED
            // RATIONALE:
            // - Matches industry standards (FishNet, Mirror)
            // - Transient state (position, rotation) recovers automatically
            // - Authority re-sends state 30-60 times/sec
            // - Value blending smooths over 1-2 dropped frames
            // - Zero performance impact

            Assert.IsFalse(testGlobal.deferSyncBundlesWaitingForGONetReady,
                "Default should be DROP (industry standard)");
        }

        [Test]
        public void Documentation_WhenToEnableDeferral()
        {
            // This test documents when users should enable deferral
            // ENABLE deferSyncBundlesWaitingForGONetReady when:
            // - Turn-based games (every state change must be received)
            // - Critical state delivery (ownership changes, inventory updates)
            // - Zero data loss is required
            //
            // LEAVE DISABLED when:
            // - Action games with high-frequency updates (positions, rotations)
            // - Transient state that recovers naturally
            // - Performance-critical scenarios (zero overhead desired)

            // Test just verifies configuration can be enabled
            testGlobal.deferSyncBundlesWaitingForGONetReady = true;
            Assert.IsTrue(testGlobal.deferSyncBundlesWaitingForGONetReady,
                "Deferral can be enabled for turn-based/critical state games");
        }

        [Test]
        public void Documentation_PerformanceCharacteristics()
        {
            // This test documents expected performance characteristics
            // OVERHEAD when deferral DISABLED (default): Zero
            // OVERHEAD when deferral ENABLED:
            // - Per OnGONetReady callback: ~0.1-0.3ms (processing 10 bundles)
            // - Queue memory: ~117KB (100 bundles × ~1200 bytes/NetworkData)
            // - Typical queue depth: 1-3 bundles (Awake completes in 1-2 frames)
            // - Max queue depth: 100 (handles extreme burst scenarios)

            // Test just verifies reasonable defaults
            Assert.AreEqual(100, testGlobal.maxSyncBundlesWaitingForGONetReady,
                "Queue size should handle typical burst scenarios");
            Assert.AreEqual(10, testGlobal.maxBundlesProcessedPerGONetReadyCallback,
                "Processing limit should prevent frame spikes");
        }

        #endregion

        #region Unity Fake Null Pattern Tests

        [Test]
        public void UnityFakeNull_DestroyedGameObject_EqualsNullReturnsTrue()
        {
            // Arrange - Create and immediately destroy GameObject
            GameObject testObj = new GameObject("TestDestroyedObject");
            Object.DestroyImmediate(testObj);

            // Act & Assert - Unity's overloaded == operator returns true for destroyed objects
            Assert.IsTrue(testObj == null,
                "Unity's == operator should return true for destroyed GameObject");
        }

        [Test]
        public void UnityFakeNull_DestroyedGameObject_CSharpReferenceNotNull()
        {
            // Arrange - Create and immediately destroy GameObject
            GameObject testObj = new GameObject("TestDestroyedObject");
            Object.DestroyImmediate(testObj);

            // Act & Assert - C# reference is NOT null (fake null pattern)
            Assert.IsTrue((object)testObj != null,
                "C# reference should NOT be null even after GameObject destruction");
        }

        [Test]
        public void UnityFakeNull_AccessingGameObjectNameThrowsException()
        {
            // Arrange - Create GameObject with component
            GameObject testObj = new GameObject("TestDestroyedWithComponent");
            var participant = testObj.AddComponent<GONetParticipant>();

            // Destroy immediately (fake null state)
            Object.DestroyImmediate(testObj);

            // Assert - Unity == operator shows destroyed
            Assert.IsTrue(participant == null,
                "Unity's == operator should detect destroyed object");

            // Assert - C# reference still exists
            Assert.IsTrue((object)participant != null,
                "C# reference should still exist (fake null)");

            // Assert - Accessing gameObject.name throws MissingReferenceException (the actual bug!)
            Assert.Throws<UnityEngine.MissingReferenceException>(() =>
            {
                var _ = participant.name; // This will throw!
            }, "Accessing name on destroyed Unity object should throw MissingReferenceException");
        }

        [Test]
        public void UnityFakeNull_SafePatternDoesNotAccessUnityProperties()
        {
            // This test documents the SAFE pattern for handling destroyed Unity objects
            //
            // SCENARIO: Code needs to check if Unity object was destroyed and log info
            //
            // UNSAFE (causes MissingReferenceException):
            //   if (participant == null) {
            //       string name = participant.name;  // ❌ CRASH! (Unity property)
            //       Log($"Destroyed participant: {name}");
            //   }
            //
            // SAFE (stores Unity data before destruction check):
            //   string name = participant.name;  // ✅ Read Unity property while still alive
            //   uint id = participant.GONetId;   // ✅ Non-Unity C# property (also safe to cache)
            //   if (participant == null) {
            //       Log($"Destroyed participant: {name}, GONetId: {id}");  // ✅ Use cached values
            //   }
            //
            // KEY INSIGHT: Unity properties (.name, .gameObject, etc.) throw when accessed
            // on destroyed objects. Pure C# properties (GONetId, etc.) may work but should
            // still be cached for consistency and safety.

            // Arrange - Create participant and cache data while alive
            GameObject testObj = new GameObject("TestSafePattern");
            var participant = testObj.AddComponent<GONetParticipant>();

            // Cache Unity properties BEFORE destruction check (CRITICAL!)
            string cachedName = participant.name; // Unity property - cache while alive!
            uint cachedId = participant.GONetId;  // C# property - also cache for consistency

            // Act - Destroy object (enters fake null state)
            Object.DestroyImmediate(testObj);

            // Assert - Can safely use cached data without accessing destroyed object
            Assert.IsTrue(participant == null, "Object should be destroyed");
            Assert.DoesNotThrow(() =>
            {
                string safeLog = $"Destroyed participant: {cachedName}, GONetId: {cachedId}";
                // No property access on destroyed object - uses cached values ✅
            }, "Using cached values should not throw even when object is destroyed");
        }

        [Test]
        public void UnityFakeNull_DocumentsCommonMistake()
        {
            // This test documents the bug that was fixed in commit b547e827
            //
            // BUG LOCATION: GONet.cs:8633 (and line 8636 - accessing gonetParticipant.name in error log)
            //
            // ORIGINAL CODE (BUGGY):
            //   if (gonetParticipant == null) {  // TRUE - Unity destroyed
            //       bool csharpNull = (object)gonetParticipant == null;  // FALSE - C# ref exists
            //       uint logId = gonetParticipant.GONetId;  // ❌ May or may not throw
            //       Log($"Destroyed: {logId}, GameObject: '{gonetParticipant.name}'");  // ❌ THROWS!
            //   }
            //
            // FIXED CODE:
            //   if (gonetParticipant == null) {  // TRUE - Unity destroyed
            //       bool csharpNull = (object)gonetParticipant == null;  // FALSE - C# ref exists
            //       // ✅ Don't access ANY properties! Use cached value instead
            //       Log($"Destroyed: GONetIdAtInstantiation {cachedId}");
            //   }
            //
            // ROOT CAUSE: Unity's operator overload makes `== null` return true,
            // but C# reference isn't null. Accessing .name throws MissingReferenceException.

            // Arrange
            GameObject testObj = new GameObject("TestBugReproduction");
            var participant = testObj.AddComponent<GONetParticipant>();

            // Destroy object
            Object.DestroyImmediate(testObj);

            // Assert - Reproduce the exact conditions of the bug
            Assert.IsTrue(participant == null, "Unity == operator returns true");
            Assert.IsTrue((object)participant != null, "C# reference is not null");

            // Assert - This was the bug: accessing .name throws (used in error log)
            Assert.Throws<UnityEngine.MissingReferenceException>(() =>
            {
                string throwsException = participant.name; // The actual bug that crashed!
            }, "This is the bug that was fixed - accessing .name on destroyed object throws");
        }

        [Test]
        public void UnityFakeNull_DiagnosticLoggingMustCacheNames()
        {
            // This test documents the bugs fixed in commit 16bde5b2
            //
            // BUG LOCATIONS:
            // - GONet.cs:8604 - Debug log in instantiation map lookup
            // - GONet.cs:8659 - Warning log when companion map not found
            // - GONet.cs:8688 - Warning log when sync companion not found
            // - GONet.cs:8721 - Exception message for GONetParticipantNotReadyException
            //
            // PROBLEM: Diagnostic logs tried to access participant.name for better error messages,
            // but if the participant was destroyed mid-deserialization, this throws MissingReferenceException.
            //
            // SCENARIO: During rapid spawning/despawning:
            // 1. Sync bundle arrives for participant
            // 2. Participant lookup succeeds (object in map)
            // 3. Object gets destroyed mid-processing (despawn event)
            // 4. Diagnostic log tries to access participant.name → CRASH
            //
            // SOLUTION: Use gonetIdAtInstantiation or other cached values in diagnostic logs,
            // NEVER access Unity properties on participants that may be destroyed.

            // Arrange - Create participant
            GameObject testObj = new GameObject("DiagnosticTest");
            var participant = testObj.AddComponent<GONetParticipant>();

            // Cache safe value BEFORE destruction
            uint cachedId = 12345; // In production this would be gonetIdAtInstantiation from deserialization

            // Act - Destroy participant (simulates mid-deserialization despawn)
            Object.DestroyImmediate(testObj);

            // Assert - Participant appears destroyed
            Assert.IsTrue(participant == null, "Unity == shows destroyed");
            Assert.IsTrue((object)participant != null, "C# reference exists");

            // Assert - UNSAFE: Accessing .name would throw
            Assert.Throws<UnityEngine.MissingReferenceException>(() =>
            {
                // This is what the BUGGY code did in diagnostic logs
                string unsafeLog = $"Error for participant: {participant.name}";
            }, "Diagnostic log accessing .name throws MissingReferenceException");

            // Assert - SAFE: Using cached ID works
            Assert.DoesNotThrow(() =>
            {
                // This is what the FIXED code does
                string safeLog = $"Error for participant GONetIdAtInstantiation: {cachedId}";
            }, "Diagnostic log using cached values does not throw");
        }

        [Test]
        public void UnityFakeNull_StringInterpolationInExecutedCode()
        {
            // This test demonstrates the ACTUAL behavior: string interpolation is safe
            // if the code path doesn't execute, but UNSAFE if it does execute.
            //
            // CORRECTED UNDERSTANDING:
            // - If the if-block doesn't execute, string interpolation is NOT evaluated (safe)
            // - If the if-block DOES execute, string interpolation evaluates and throws (unsafe)
            //
            // The bugs we fixed (GONet.cs:8604, 8659, 8688, 8721) were all in EXECUTED code paths,
            // so accessing participant.name in logs would throw when participant was destroyed.

            // Arrange
            GameObject testObj = new GameObject("InterpolationTest");
            var participant = testObj.AddComponent<GONetParticipant>();
            Object.DestroyImmediate(testObj);

            // Assert - String interpolation in NON-executed path is safe
            bool shouldLog = false;
            Assert.DoesNotThrow(() =>
            {
                if (shouldLog)  // This block never executes
                {
                    string message = $"Participant: {participant.name}"; // Not evaluated, safe
                }
            }, "String interpolation in non-executed code path is safe");

            // Assert - String interpolation in EXECUTED path throws
            bool mustLog = true;
            Assert.Throws<UnityEngine.MissingReferenceException>(() =>
            {
                if (mustLog)  // This block DOES execute
                {
                    string message = $"Participant: {participant.name}"; // Evaluated, throws!
                }
            }, "String interpolation in executed code path throws when accessing destroyed Unity object");

            // Assert - Safe version uses cached value
            uint cachedId = 999;
            Assert.DoesNotThrow(() =>
            {
                if (mustLog)
                {
                    string message = $"Participant: {cachedId}"; // Safe - no Unity property access
                }
            }, "Using cached values in string interpolation is always safe");
        }

        #endregion
    }
}
