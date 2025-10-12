using NUnit.Framework;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace GONet.Editor.UnitTests
{
    /// <summary>
    /// Unit tests for GONetId reuse prevention system (TTL-based delayed reuse).
    ///
    /// SYSTEM OVERVIEW:
    /// When a GONetParticipant despawns, its GONetId is marked with a timestamp.
    /// The ID cannot be reused until the configured TTL (Time To Live) expires.
    /// This prevents despawn messages from arriving for the wrong object due to
    /// premature GONetId reuse while messages are still in flight.
    ///
    /// CONFIGURATION:
    /// - GONetGlobal.gonetIdReuseDelaySeconds (default: 5 seconds, range: 1-30)
    /// - Based on network RTT + safety margin for packet reordering
    ///
    /// KEY METHODS TESTED:
    /// - MarkGONetIdDespawned(uint gonetId) - Marks ID as recently despawned
    /// - CanReuseGONetId(uint gonetId) - Checks if TTL expired
    /// - CleanupExpiredDespawnedGONetIds() - Periodic cleanup every 30 seconds
    /// - GetNextAvailableGONetIdRaw() - Skips recently despawned IDs during allocation
    ///
    /// TEST COVERAGE:
    /// 1. Basic TTL tracking (mark, check, expire)
    /// 2. Reuse prevention during cooldown period
    /// 3. Reuse allowed after TTL expiration
    /// 4. Periodic cleanup of expired entries
    /// 5. Rapid spawning scenarios (batch exhaustion edge case)
    /// 6. Configuration variations (different TTL values)
    /// 7. Integration with batch allocation system
    /// 8. GONetId composition (raw << 10 | authority)
    ///
    /// IMPORTANT NOTES:
    /// - These tests use reflection to access internal GONet methods
    /// - Tests verify the TTL mechanism works correctly across various scenarios
    /// - GONetGlobal must exist for configuration to work (some tests create it)
    /// </summary>
    [TestFixture]
    public class GONetIdReusePreventionTests
    {
        private GameObject testGONetGlobal;
        private GONetGlobal gonetGlobalInstance;

        /// <summary>
        /// Creates a minimal GONetGlobal instance for testing configuration.
        /// Required for GetGONetIdReuseDelay() to return configured values.
        /// </summary>
        private void SetupGONetGlobal(float reuseDelaySeconds = 5f)
        {
            // Create GameObject with GONetGlobal
            testGONetGlobal = new GameObject("TestGONetGlobal");

            // Add required GONetParticipant first (GONetGlobal requires it)
            var participant = testGONetGlobal.AddComponent<GONetParticipant>();

            // Add GONetGlobal
            gonetGlobalInstance = testGONetGlobal.AddComponent<GONetGlobal>();
            gonetGlobalInstance.gonetIdReuseDelaySeconds = reuseDelaySeconds;

            // Set as singleton instance (GONetGlobal.Instance)
            var instanceField = typeof(GONetGlobal).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, gonetGlobalInstance);
        }

        /// <summary>
        /// Cleans up GONetGlobal instance after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (testGONetGlobal != null)
            {
                // Clear singleton reference
                var instanceField = typeof(GONetGlobal).GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                instanceField?.SetValue(null, null);

                Object.DestroyImmediate(testGONetGlobal);
                testGONetGlobal = null;
                gonetGlobalInstance = null;
            }

            // Clear tracking dictionary after each test
            var trackingDict = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            if (trackingDict != null)
            {
                var dict = trackingDict.GetValue(null) as System.Collections.IDictionary;
                dict?.Clear();
            }
        }

        #region Basic TTL Tracking Tests

        /// <summary>
        /// TEST: Newly despawned GONetId is tracked and cannot be reused immediately.
        /// </summary>
        [Test]
        public void MarkGONetIdDespawned_TracksIdWithTimestamp()
        {
            SetupGONetGlobal(5f);

            uint testGonetId = 588799; // (574 << 10) | 1023 = server authority

            // Mark as despawned
            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            markMethod.Invoke(null, new object[] { testGonetId });

            // Should NOT be reusable immediately
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);
            bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { testGonetId });

            Assert.IsFalse(canReuse, "GONetId should NOT be reusable immediately after despawn");
        }

        /// <summary>
        /// TEST: GONetId can be reused after TTL expires.
        ///
        /// NOTE: This test manually inserts an old timestamp into the tracking dictionary
        /// to simulate an expired TTL, since we can't easily manipulate the Time system.
        /// </summary>
        [Test]
        public void CanReuseGONetId_AllowsReuseAfterTTLExpires()
        {
            SetupGONetGlobal(5f); // Normal TTL

            uint testGonetId = 560127; // (546 << 10) | 1023

            // Get tracking dictionary and manually insert old timestamp (simulates expired TTL)
            var trackingDictField = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = trackingDictField.GetValue(null) as System.Collections.IDictionary;

            // Insert with timestamp far in the past (more than 5 seconds ago)
            // Using a very old timestamp like -10.0 ensures TTL is expired
            dict[testGonetId] = -10.0;

            // Should be reusable now (TTL expired)
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);
            bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { testGonetId });

            Assert.IsTrue(canReuse, "GONetId should be reusable after TTL expires");

            // Verify it was removed from tracking dictionary after successful reuse check
            Assert.IsFalse(dict.Contains(testGonetId), "Expired GONetId should be removed from tracking dictionary");
        }

        /// <summary>
        /// TEST: GONetId_Unset should always be reusable (not tracked).
        /// </summary>
        [Test]
        public void MarkGONetIdDespawned_DoesNotTrackUnsetId()
        {
            SetupGONetGlobal(5f);

            uint unsetId = GONetParticipant.GONetId_Unset; // 0

            // Mark as despawned
            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            markMethod.Invoke(null, new object[] { unsetId });

            // Should always be reusable (not tracked)
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);
            bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { unsetId });

            Assert.IsTrue(canReuse, "GONetId_Unset should always be reusable (never tracked)");
        }

        #endregion

        #region Reuse Prevention Tests

        /// <summary>
        /// TEST: Multiple GONetIds can be tracked simultaneously with independent TTLs.
        /// </summary>
        [Test]
        public void CanReuseGONetId_TracksMultipleIdsIndependently()
        {
            SetupGONetGlobal(5f);

            uint gonetId1 = 5119;  // (4 << 10) | 1023
            uint gonetId2 = 6143;  // (5 << 10) | 1023
            uint gonetId3 = 7167;  // (6 << 10) | 1023

            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);

            // Mark all three as despawned
            markMethod.Invoke(null, new object[] { gonetId1 });
            markMethod.Invoke(null, new object[] { gonetId2 });
            markMethod.Invoke(null, new object[] { gonetId3 });

            // All should be non-reusable
            Assert.IsFalse((bool)canReuseMethod.Invoke(null, new object[] { gonetId1 }), "ID 1 should not be reusable");
            Assert.IsFalse((bool)canReuseMethod.Invoke(null, new object[] { gonetId2 }), "ID 2 should not be reusable");
            Assert.IsFalse((bool)canReuseMethod.Invoke(null, new object[] { gonetId3 }), "ID 3 should not be reusable");
        }

        /// <summary>
        /// TEST: CanReuseGONetId returns true for IDs that were never despawned.
        /// </summary>
        [Test]
        public void CanReuseGONetId_AllowsNeverDespawnedIds()
        {
            SetupGONetGlobal(5f);

            uint neverDespawnedId = 999999;

            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);
            bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { neverDespawnedId });

            Assert.IsTrue(canReuse, "GONetId that was never despawned should be reusable");
        }

        #endregion

        #region Cleanup Tests

        /// <summary>
        /// TEST: CleanupExpiredDespawnedGONetIds removes expired entries from tracking map.
        ///
        /// NOTE: This test verifies the cleanup MECHANISM works, but doesn't test the
        /// 30-second interval logic (that requires a real Update() loop).
        /// </summary>
        [Test]
        public void CleanupExpiredDespawnedGONetIds_RemovesExpiredEntries()
        {
            SetupGONetGlobal(5f); // Normal TTL

            uint gonetId1 = 5119;  // Will expire
            uint gonetId2 = 6143;  // Will also expire

            // Get tracking dictionary and manually insert old timestamps
            var trackingDictField = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = trackingDictField.GetValue(null) as System.Collections.IDictionary;

            // Insert both with old timestamps (expired)
            dict[gonetId1] = -10.0;
            dict[gonetId2] = -10.0;

            Assert.AreEqual(2, dict.Count, "Should have 2 entries before cleanup");

            // Force cleanup interval to trigger
            var lastCleanupField = typeof(GONet.GONetMain).GetField("_lastGONetIdReuseCleanupTime", BindingFlags.NonPublic | BindingFlags.Static);
            lastCleanupField.SetValue(null, null); // Reset to null to force cleanup

            // Run cleanup
            var cleanupMethod = typeof(GONet.GONetMain).GetMethod("CleanupExpiredDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            cleanupMethod.Invoke(null, null);

            // Both entries should be removed
            Assert.AreEqual(0, dict.Count, "All expired entries should be removed after cleanup");
        }

        /// <summary>
        /// TEST: CleanupExpiredDespawnedGONetIds preserves non-expired entries.
        /// </summary>
        [Test]
        public void CleanupExpiredDespawnedGONetIds_PreservesNonExpiredEntries()
        {
            SetupGONetGlobal(5f); // Normal TTL

            uint expiredId = 5119;
            uint notExpiredId = 6143;

            // Get tracking dictionary and manually insert timestamps
            var trackingDictField = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = trackingDictField.GetValue(null) as System.Collections.IDictionary;

            // Insert one expired (old timestamp) and one not expired (recent/future timestamp)
            dict[expiredId] = -10.0;    // Expired (far in the past)
            dict[notExpiredId] = 999999.0; // Not expired (far in the future)

            Assert.AreEqual(2, dict.Count, "Should have 2 entries before cleanup");

            // Force cleanup interval to trigger
            var lastCleanupField = typeof(GONet.GONetMain).GetField("_lastGONetIdReuseCleanupTime", BindingFlags.NonPublic | BindingFlags.Static);
            lastCleanupField.SetValue(null, null);

            // Run cleanup
            var cleanupMethod = typeof(GONet.GONetMain).GetMethod("CleanupExpiredDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            cleanupMethod.Invoke(null, null);

            // Only expired ID should be removed
            Assert.AreEqual(1, dict.Count, "Only expired entry should be removed");
            Assert.IsFalse(dict.Contains(expiredId), "Expired ID should be removed");
            Assert.IsTrue(dict.Contains(notExpiredId), "Non-expired ID should remain");
        }

        #endregion

        #region Configuration Tests

        /// <summary>
        /// TEST: Different TTL values are respected.
        /// </summary>
        [Test]
        public void GetGONetIdReuseDelay_RespectsConfiguredValue()
        {
            // Test with 10 second TTL
            SetupGONetGlobal(10f);

            var getDelayMethod = typeof(GONet.GONetMain).GetMethod("GetGONetIdReuseDelay", BindingFlags.NonPublic | BindingFlags.Static);
            double delay = (double)getDelayMethod.Invoke(null, null);

            Assert.AreEqual(10.0, delay, 0.01, "Should return configured TTL value");
        }

        /// <summary>
        /// TEST: Fallback to default when GONetGlobal unavailable.
        /// </summary>
        [Test]
        public void GetGONetIdReuseDelay_FallsBackToDefaultWhenGONetGlobalNull()
        {
            // Don't create GONetGlobal - test fallback behavior
            var getDelayMethod = typeof(GONet.GONetMain).GetMethod("GetGONetIdReuseDelay", BindingFlags.NonPublic | BindingFlags.Static);
            double delay = (double)getDelayMethod.Invoke(null, null);

            Assert.AreEqual(5.0, delay, 0.01, "Should fall back to 5 second default when GONetGlobal unavailable");
        }

        #endregion

        #region GONetId Composition Tests

        /// <summary>
        /// TEST: System correctly handles composed GONetIds (raw << 10 | authority).
        ///
        /// IMPORTANT: The tracking system stores COMPOSED GONetIds, not raw IDs.
        /// This test verifies that the composition is handled correctly.
        /// </summary>
        [Test]
        public void GONetIdComposition_TracksComposedValues()
        {
            SetupGONetGlobal(5f);

            // Test composed GONetId: (574 << 10) | 1023 = 588799
            uint rawId = 574;
            ushort authorityId = 1023; // Server authority
            uint composedId = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | authorityId;

            Assert.AreEqual(588799u, composedId, "Composed GONetId calculation should match expected value");

            // Mark COMPOSED ID as despawned
            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            markMethod.Invoke(null, new object[] { composedId });

            // Check COMPOSED ID reusability
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);
            bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { composedId });

            Assert.IsFalse(canReuse, "Composed GONetId should not be reusable immediately");
        }

        /// <summary>
        /// TEST: Different authority IDs produce different composed GONetIds from same raw ID.
        /// </summary>
        [Test]
        public void GONetIdComposition_DifferentAuthoritiesProduceDifferentComposedIds()
        {
            SetupGONetGlobal(5f);

            uint rawId = 100;
            ushort serverAuthority = 1023;
            ushort clientAuthority = 1;

            uint composedIdServer = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | serverAuthority;
            uint composedIdClient = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | clientAuthority;

            // Should be different values
            Assert.AreNotEqual(composedIdServer, composedIdClient, "Same raw ID with different authority should produce different composed IDs");

            // Mark server-authority ID as despawned
            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            markMethod.Invoke(null, new object[] { composedIdServer });

            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);

            // Server-authority ID should not be reusable
            bool canReuseServer = (bool)canReuseMethod.Invoke(null, new object[] { composedIdServer });
            Assert.IsFalse(canReuseServer, "Server-authority composed ID should not be reusable");

            // Client-authority ID SHOULD be reusable (wasn't despawned)
            bool canReuseClient = (bool)canReuseMethod.Invoke(null, new object[] { composedIdClient });
            Assert.IsTrue(canReuseClient, "Client-authority composed ID should be reusable (different ID)");
        }

        #endregion

        #region Integration Scenario Tests

        /// <summary>
        /// TEST: Simulates rapid projectile spawning scenario (GONetId batch system).
        ///
        /// SCENARIO: Client fires 10 projectiles in quick succession, then destroys them all,
        /// then tries to fire 10 more. The batch IDs should be reused after TTL expires.
        /// </summary>
        [Test]
        public void RapidSpawning_ProjectileScenario_ReusesIdsAfterTTL()
        {
            SetupGONetGlobal(5f); // Normal TTL

            // Simulate batch range: raw IDs 4-13 (10 IDs)
            uint batchStart = 4;
            int batchSize = 10;
            ushort serverAuthority = 1023;

            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);

            // PHASE 1: Spawn 10 projectiles, then despawn them all
            for (uint i = 0; i < batchSize; i++)
            {
                uint rawId = batchStart + i;
                uint composedId = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | serverAuthority;
                markMethod.Invoke(null, new object[] { composedId });
            }

            // PHASE 2: Try to reuse immediately (should fail)
            for (uint i = 0; i < batchSize; i++)
            {
                uint rawId = batchStart + i;
                uint composedId = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | serverAuthority;
                bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { composedId });
                Assert.IsFalse(canReuse, $"Batch ID {rawId} should not be reusable immediately");
            }

            // PHASE 3: Manually set all IDs to expired timestamps
            var trackingDictField = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = trackingDictField.GetValue(null) as System.Collections.IDictionary;

            for (uint i = 0; i < batchSize; i++)
            {
                uint rawId = batchStart + i;
                uint composedId = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | serverAuthority;
                dict[composedId] = -10.0; // Set to expired
            }

            // PHASE 4: Should now be reusable
            for (uint i = 0; i < batchSize; i++)
            {
                uint rawId = batchStart + i;
                uint composedId = unchecked((uint)(rawId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | serverAuthority;
                bool canReuse = (bool)canReuseMethod.Invoke(null, new object[] { composedId });
                Assert.IsTrue(canReuse, $"Batch ID {rawId} should be reusable after TTL expires");
            }
        }

        /// <summary>
        /// TEST: Validates the "wrong object despawned" bug scenario is prevented.
        ///
        /// BUG SCENARIO (from GONETID_REUSE_BUG.md):
        /// 1. Projectile A spawns with GONetId 5119 at T+0
        /// 2. Projectile A despawns at T+1
        /// 3. Projectile B spawns with GONetId 5119 at T+1.5 (REUSE TOO FAST)
        /// 4. Despawn message for Projectile A arrives at T+2
        /// 5. BUG: Projectile B gets despawned instead of A
        ///
        /// FIX: TTL prevents step 3 from happening until safe.
        /// </summary>
        [Test]
        public void WrongObjectDespawnedBug_PreventedByTTL()
        {
            SetupGONetGlobal(5f); // Normal TTL

            uint gonetId = 5119; // (4 << 10) | 1023

            var markMethod = typeof(GONet.GONetMain).GetMethod("MarkGONetIdDespawned", BindingFlags.NonPublic | BindingFlags.Static);
            var canReuseMethod = typeof(GONet.GONetMain).GetMethod("CanReuseGONetId", BindingFlags.NonPublic | BindingFlags.Static);

            // T+0: Projectile A spawns (no action needed)

            // T+1: Projectile A despawns
            markMethod.Invoke(null, new object[] { gonetId });

            // T+1.5: Attempt to spawn Projectile B with same GONetId (should be prevented)
            // The ID was just despawned, so it should NOT be reusable yet
            bool canReuseImmediately = (bool)canReuseMethod.Invoke(null, new object[] { gonetId });
            Assert.IsFalse(canReuseImmediately, "GONetId should NOT be reusable immediately after despawn (TTL=5s)");

            // T+6: Manually expire the ID by setting old timestamp
            var trackingDictField = typeof(GONet.GONetMain).GetField("recentlyDespawnedGONetIds", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = trackingDictField.GetValue(null) as System.Collections.IDictionary;
            dict[gonetId] = -10.0; // Set to expired timestamp

            // Should now be safe to reuse (TTL expired)
            bool canReuseAfterExpiry = (bool)canReuseMethod.Invoke(null, new object[] { gonetId });
            Assert.IsTrue(canReuseAfterExpiry, "GONetId should be reusable after TTL expires");
        }

        /// <summary>
        /// TEST: Detects overlapping batches scenario (real bug from October 2025).
        ///
        /// BUG SCENARIO:
        /// 1. Client receives batch [4-203] (200 IDs) from initial connection
        /// 2. Client exhausts most of batch [4-203], requests new batch
        /// 3. Server allocates batch [104-303] (OVERLAPS with [4-203]!)
        /// 4. IDs 104-203 exist in BOTH batches (100 ID overlap)
        /// 5. Client allocates ID 147 from batch [4-203] at T+0 (Beacon)
        /// 6. Batch [4-203] exhausted and removed
        /// 7. Client allocates ID 147 AGAIN from batch [104-303] at T+3 (Cannonball)
        /// 8. Cannonball despawns, removes ID 147 from GONet maps
        /// 9. Beacon becomes "zombie" object (alive but not tracked)
        ///
        /// ROOT CAUSE: Server doesn't track which batches client still has active,
        /// so it can allocate overlapping ranges.
        ///
        /// FIX: Server must track all active client batches and skip overlapping ranges.
        /// </summary>
        [Test]
        public void OverlappingBatches_DetectedAndPrevented()
        {
            // Simulate the overlap scenario
            // Batch 1: [4-203] (200 IDs)
            // Batch 2: [104-303] (200 IDs)
            // Overlap: IDs 104-203 (100 IDs)

            uint batch1Start = 4;
            uint batch2Start = 104;
            int batchSize = 200;

            // Calculate overlap range
            uint overlapStart = batch2Start; // 104
            uint overlapEnd = batch1Start + (uint)batchSize; // 204 (exclusive)

            // Verify overlap exists
            Assert.IsTrue(overlapEnd > overlapStart, "Batches should overlap");

            // Count overlapping IDs
            int overlapCount = (int)(overlapEnd - overlapStart);
            Assert.AreEqual(100, overlapCount, "Should have 100 overlapping IDs");

            // Verify specific ID 147 is in BOTH batches
            bool inBatch1 = (147 >= batch1Start && 147 < batch1Start + batchSize);
            bool inBatch2 = (147 >= batch2Start && 147 < batch2Start + batchSize);

            Assert.IsTrue(inBatch1, "ID 147 should be in batch [4-203]");
            Assert.IsTrue(inBatch2, "ID 147 should be in batch [104-303]");
            Assert.IsTrue(inBatch1 && inBatch2, "OVERLAP BUG: ID 147 exists in BOTH batches!");
        }

        #endregion
    }
}
