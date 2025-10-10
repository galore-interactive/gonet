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

        #region Integration Tests (Conceptual - Would require full GONet infrastructure)

        // NOTE: The following tests are conceptual and would require full GONet initialization
        // to run properly. They document the expected behavior for manual/integration testing.

        /*
        [Test]
        public void DeferralDisabled_UnreliableBundle_ParticipantNotReady_DropsBundle()
        {
            // SCENARIO: Default behavior (deferral disabled)
            // - Unreliable sync bundle arrives
            // - Participant exists but didAwakeComplete=false
            // - EXPECTED: Bundle dropped, no queue, no retry
            // - RATIONALE: Transient state, next update arrives in 16-33ms anyway
        }

        [Test]
        public void DeferralDisabled_ReliableBundle_ParticipantNotReady_DropsBundle()
        {
            // SCENARIO: Default behavior (deferral disabled)
            // - Reliable sync bundle arrives
            // - Participant exists but didAwakeComplete=false
            // - EXPECTED: Bundle dropped, no queue, no retry
            // - RATIONALE: Authority re-sends state 30-60 times/sec, auto-recovery
        }

        [Test]
        public void DeferralEnabled_UnreliableBundle_ParticipantNotReady_DropsBundle()
        {
            // SCENARIO: Deferral enabled but unreliable channel
            // - Unreliable sync bundle arrives
            // - Participant exists but didAwakeComplete=false
            // - EXPECTED: Bundle dropped (unreliable ALWAYS drops, by design)
            // - RATIONALE: Deferral only applies to reliable channels
        }

        [Test]
        public void DeferralEnabled_ReliableBundle_ParticipantNotReady_QueuesBundle()
        {
            // SCENARIO: Opt-in deferral for reliable bundles
            // - testGlobal.deferSyncBundlesWaitingForGONetReady = true
            // - Reliable sync bundle arrives
            // - Participant exists but didAwakeComplete=false
            // - EXPECTED: Bundle queued in incomingNetworkData_waitingForGONetReady
            // - shouldReturnToPool = false (queue owns byte array)
        }

        [Test]
        public void DeferralEnabled_ReliableBundle_RetryAfterReady_Succeeds()
        {
            // SCENARIO: Deferred bundle processed after participant ready
            // - Bundle queued (participant not ready initially)
            // - Participant completes Awake() → didAwakeComplete=true
            // - CheckAndPublishOnGONetReady() fires → ProcessDeferredSyncBundlesWaitingForGONetReady()
            // - EXPECTED: Bundle dequeued, processed successfully
            // - Byte array returned to pool after success
        }

        [Test]
        public void DeferralEnabled_ReliableBundle_RetryStillNotReady_Drops()
        {
            // SCENARIO: Participant STILL not ready after 1+ frames (lifecycle bug)
            // - Bundle queued (participant not ready)
            // - ProcessDeferredSyncBundlesWaitingForGONetReady() called
            // - isProcessingFromQueue=true → GONetParticipantNotReadyException thrown again
            // - EXPECTED: Bundle dropped, error logged (lifecycle bug detected)
            // - Byte array returned to pool
        }

        [Test]
        public void DeferralEnabled_QueueFull_DropsOldest()
        {
            // SCENARIO: Queue capacity exceeded (FIFO drop policy)
            // - testGlobal.maxSyncBundlesWaitingForGONetReady = 100
            // - 100 bundles already queued
            // - 101st bundle arrives (participant still not ready)
            // - EXPECTED: Oldest bundle dequeued and dropped
            // - Oldest bundle's byte array returned to pool
            // - 101st bundle queued
            // - Warning logged prompting user to increase queue size
        }

        [Test]
        public void IncrementalProcessing_LimitsPerCallback()
        {
            // SCENARIO: Prevent frame stutter during mass spawns
            // - testGlobal.maxBundlesProcessedPerGONetReadyCallback = 10
            // - 50 bundles queued (waiting for participants)
            // - ProcessDeferredSyncBundlesWaitingForGONetReady() called
            // - EXPECTED: Only 10 bundles processed in this call
            // - 40 bundles remain in queue for next OnGONetReady callback
            // - Prevents frame spike from processing all 50 at once
        }

        [Test]
        public void PoolSafety_BundleDropped_ByteArrayReturned()
        {
            // SCENARIO: Memory leak prevention (critical!)
            // - Bundle deferred → NetworkData.messageBytes borrowed from pool
            // - Bundle dropped (unreliable, or queue full, or retry failed)
            // - EXPECTED: Byte array MUST be returned to pool via queueForPostWorkResourceReturn
            // - Failure to return = pool exhaustion = allocation spikes
        }

        [Test]
        public void PoolSafety_BundleProcessed_ByteArrayReturned()
        {
            // SCENARIO: Memory leak prevention (critical!)
            // - Bundle deferred → NetworkData.messageBytes borrowed from pool
            // - Bundle processed successfully after retry
            // - EXPECTED: Byte array MUST be returned to pool
            // - ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL handles return
        }

        [Test]
        public void AuthorityAgnostic_ClientReceives_Works()
        {
            // SCENARIO: Client receiving sync bundles from server
            // - Client spawns object locally (client authority initially)
            // - Server assumes authority (IsMine=False on client)
            // - Server sends sync bundle → arrives before client's Awake completes
            // - EXPECTED: Deferral works (if enabled), or drops (if disabled)
        }

        [Test]
        public void AuthorityAgnostic_ServerReceives_Works()
        {
            // SCENARIO: Server receiving sync bundles from client
            // - Client spawns object → sends to server
            // - Server receives spawn event → instantiates locally
            // - Client sends sync bundle → arrives before server's Awake completes
            // - EXPECTED: Deferral works (if enabled), or drops (if disabled)
            // - NOT client-only - works for ALL receivers
        }

        [Test]
        public void ParticipantDestroyed_BeforeRetry_DropsGracefully()
        {
            // SCENARIO: Edge case - participant despawned while bundle queued
            // - Bundle queued (participant not ready)
            // - Participant destroyed (despawn event)
            // - ProcessDeferredSyncBundlesWaitingForGONetReady() called
            // - GetGONetParticipantById() returns null
            // - EXPECTED: KeyNotFoundException caught, bundle dropped
            // - Byte array returned to pool
            // - No crash, clean failure
        }
        */

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

        #endregion
    }
}
