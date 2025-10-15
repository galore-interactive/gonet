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

using NUnit.Framework;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Unit tests for GONet Addressables caching system - ensures production-grade performance
    /// and memory management for addressable prefab loading.
    ///
    /// These tests validate:
    /// - Cache hit/miss behavior
    /// - Manual cache management APIs
    /// - Edge cases (null keys, null prefabs, duplicate caching)
    /// - Cache persistence and cleanup
    /// - Memory management operations
    /// - Diagnostic APIs
    ///
    /// NOTE: These tests validate the cache management infrastructure only. Full integration tests
    /// with actual Unity Addressables loading require a running GONet runtime environment and are
    /// documented separately.
    /// </summary>
    [TestFixture]
    public class GONetAddressablesCacheTests
    {
        private GONetParticipant testPrefab1;
        private GONetParticipant testPrefab2;
        private GONetParticipant testPrefab3;

        [SetUp]
        public void Setup()
        {
            // Reset cache state before each test
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();

            // Create mock GONetParticipant instances for testing
            testPrefab1 = CreateMockGONetParticipant("TestPrefab1");
            testPrefab2 = CreateMockGONetParticipant("TestPrefab2");
            testPrefab3 = CreateMockGONetParticipant("TestPrefab3");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up mock prefabs
            if (testPrefab1 != null) Object.DestroyImmediate(testPrefab1.gameObject);
            if (testPrefab2 != null) Object.DestroyImmediate(testPrefab2.gameObject);
            if (testPrefab3 != null) Object.DestroyImmediate(testPrefab3.gameObject);

            // Clear cache after each test
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();
        }

        private GONetParticipant CreateMockGONetParticipant(string name)
        {
            GameObject go = new GameObject(name);
            return go.AddComponent<GONetParticipant>();
        }

        #region Basic Cache Operations

        [Test]
        public void CacheAddressablePrefab_StoresPrefabCorrectly()
        {
            // Arrange
            const string key = "TestKey1";

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);

            // Assert
            GONetParticipant retrievedPrefab;
            bool success = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);
            Assert.IsTrue(success, "Should successfully retrieve cached prefab");
            Assert.AreEqual(testPrefab1, retrievedPrefab, "Retrieved prefab should match cached prefab");
        }

        [Test]
        public void TryGetCachedAddressablePrefab_CacheMiss_ReturnsFalse()
        {
            // Arrange
            const string key = "NonExistentKey";

            // Act
            GONetParticipant retrievedPrefab;
            bool success = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);

            // Assert
            Assert.IsFalse(success, "Should return false for cache miss");
            Assert.IsNull(retrievedPrefab, "Retrieved prefab should be null on cache miss");
        }

        [Test]
        public void CacheAddressablePrefab_MultiplePrefabs_StoresAllCorrectly()
        {
            // Arrange
            const string key1 = "TestKey1";
            const string key2 = "TestKey2";
            const string key3 = "TestKey3";

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key1, testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key2, testPrefab2);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key3, testPrefab3);

            // Assert
            GONetParticipant retrieved1, retrieved2, retrieved3;
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key1, out retrieved1), "Should retrieve prefab 1");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key2, out retrieved2), "Should retrieve prefab 2");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key3, out retrieved3), "Should retrieve prefab 3");

            Assert.AreEqual(testPrefab1, retrieved1, "Prefab 1 should match");
            Assert.AreEqual(testPrefab2, retrieved2, "Prefab 2 should match");
            Assert.AreEqual(testPrefab3, retrieved3, "Prefab 3 should match");
        }

        [Test]
        public void GetCachedAddressablePrefabCount_ReturnsCorrectCount()
        {
            // Arrange - start with empty cache
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should start empty");

            // Act - add prefabs
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key1", testPrefab1);
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 1 prefab");

            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key2", testPrefab2);
            Assert.AreEqual(2, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 2 prefabs");

            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key3", testPrefab3);
            Assert.AreEqual(3, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 3 prefabs");
        }

        #endregion

        #region Cache Removal

        [Test]
        public void RemoveCachedAddressablePrefab_RemovesSinglePrefab()
        {
            // Arrange
            const string key = "TestKey1";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 1 prefab before removal");

            // Act
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab(key);

            // Assert
            Assert.IsTrue(removed, "Should return true when removing existing prefab");
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should be empty after removal");

            GONetParticipant retrievedPrefab;
            bool found = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);
            Assert.IsFalse(found, "Should not find removed prefab");
        }

        [Test]
        public void RemoveCachedAddressablePrefab_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            const string key = "NonExistentKey";

            // Act
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab(key);

            // Assert
            Assert.IsFalse(removed, "Should return false when key doesn't exist");
        }

        [Test]
        public void RemoveCachedAddressablePrefab_LeavesOtherPrefabsIntact()
        {
            // Arrange
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key1", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key2", testPrefab2);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key3", testPrefab3);
            Assert.AreEqual(3, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 3 prefabs");

            // Act - remove middle prefab
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab("Key2");

            // Assert
            Assert.IsTrue(removed, "Should remove prefab 2");
            Assert.AreEqual(2, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 2 prefabs remaining");

            // Verify other prefabs still exist
            GONetParticipant retrieved1, retrieved3;
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Key1", out retrieved1), "Prefab 1 should still exist");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Key3", out retrieved3), "Prefab 3 should still exist");
            Assert.AreEqual(testPrefab1, retrieved1, "Prefab 1 should be unchanged");
            Assert.AreEqual(testPrefab3, retrieved3, "Prefab 3 should be unchanged");
        }

        [Test]
        public void ClearAddressablePrefabCache_RemovesAllPrefabs()
        {
            // Arrange
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key1", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key2", testPrefab2);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key3", testPrefab3);
            Assert.AreEqual(3, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 3 prefabs");

            // Act
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();

            // Assert
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should be empty after clear");

            GONetParticipant retrieved1, retrieved2, retrieved3;
            Assert.IsFalse(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Key1", out retrieved1), "Prefab 1 should be removed");
            Assert.IsFalse(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Key2", out retrieved2), "Prefab 2 should be removed");
            Assert.IsFalse(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Key3", out retrieved3), "Prefab 3 should be removed");
        }

        [Test]
        public void ClearAddressablePrefabCache_OnEmptyCache_DoesNotError()
        {
            // Arrange - cache is already empty from Setup()
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should start empty");

            // Act & Assert - should not throw
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should still be empty");
        }

        #endregion

        #region Edge Cases - Null Inputs

        [Test]
        public void CacheAddressablePrefab_NullKey_DoesNotCache()
        {
            // Arrange
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Cannot cache.*null.*empty key.*"));
            int initialCount = GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount();

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(null, testPrefab1);

            // Assert
            Assert.AreEqual(initialCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should not add prefab with null key");
        }

        [Test]
        public void CacheAddressablePrefab_EmptyKey_DoesNotCache()
        {
            // Arrange
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Cannot cache.*null.*empty key.*"));
            int initialCount = GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount();

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("", testPrefab1);

            // Assert
            Assert.AreEqual(initialCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should not add prefab with empty key");
        }

        [Test]
        public void CacheAddressablePrefab_WhitespaceKey_DoesNotCache()
        {
            // Arrange
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Cannot cache.*null.*empty key.*"));
            int initialCount = GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount();

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("   ", testPrefab1);

            // Assert
            Assert.AreEqual(initialCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should not add prefab with whitespace-only key");
        }

        [Test]
        public void CacheAddressablePrefab_NullPrefab_DoesNotCache()
        {
            // Arrange
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*Cannot cache null prefab.*"));
            int initialCount = GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount();

            // Act
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("ValidKey", null);

            // Assert
            Assert.AreEqual(initialCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should not add null prefab");
        }

        [Test]
        public void RemoveCachedAddressablePrefab_NullKey_ReturnsFalse()
        {
            // Act
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab(null);

            // Assert
            Assert.IsFalse(removed, "Should return false for null key");
        }

        [Test]
        public void RemoveCachedAddressablePrefab_EmptyKey_ReturnsFalse()
        {
            // Act
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab("");

            // Assert
            Assert.IsFalse(removed, "Should return false for empty key");
        }

        [Test]
        public void TryGetCachedAddressablePrefab_NullKey_ReturnsFalse()
        {
            // Act
            GONetParticipant retrievedPrefab;
            bool success = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(null, out retrievedPrefab);

            // Assert
            Assert.IsFalse(success, "Should return false for null key");
            Assert.IsNull(retrievedPrefab, "Retrieved prefab should be null");
        }

        [Test]
        public void TryGetCachedAddressablePrefab_EmptyKey_ReturnsFalse()
        {
            // Act
            GONetParticipant retrievedPrefab;
            bool success = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("", out retrievedPrefab);

            // Assert
            Assert.IsFalse(success, "Should return false for empty key");
            Assert.IsNull(retrievedPrefab, "Retrieved prefab should be null");
        }

        #endregion

        #region Edge Cases - Duplicate Keys

        [Test]
        public void CacheAddressablePrefab_DuplicateKeySamePrefab_IgnoresDuplicate()
        {
            // Arrange
            const string key = "DuplicateKey";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);

            // Act - try to cache same prefab with same key
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);

            // Assert
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should still have only 1 prefab");

            GONetParticipant retrievedPrefab;
            GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);
            Assert.AreEqual(testPrefab1, retrievedPrefab, "Should still retrieve original prefab");
        }

        [Test]
        public void CacheAddressablePrefab_DuplicateKeyDifferentPrefab_WarnsAndIgnoresNew()
        {
            // Arrange
            const string key = "DuplicateKey";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);

            // Act - try to cache different prefab with same key (should warn)
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(".*already cached.*different prefab.*"));
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab2);

            // Assert - should keep original prefab
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should still have only 1 prefab");

            GONetParticipant retrievedPrefab;
            GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);
            Assert.AreEqual(testPrefab1, retrievedPrefab, "Should keep original prefab, not replace with new one");
        }

        #endregion

        #region Cache Persistence and Re-adding

        [Test]
        public void CacheAddressablePrefab_AfterRemoval_CanReAdd()
        {
            // Arrange
            const string key = "TestKey";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab1);
            GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab(key);
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should be empty after removal");

            // Act - re-add prefab with same key
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(key, testPrefab2);

            // Assert
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 1 prefab after re-add");

            GONetParticipant retrievedPrefab;
            GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(key, out retrievedPrefab);
            Assert.AreEqual(testPrefab2, retrievedPrefab, "Should retrieve newly added prefab");
        }

        [Test]
        public void CacheAddressablePrefab_AfterClearAll_CanReAdd()
        {
            // Arrange
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key1", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("Key2", testPrefab2);
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should be empty after clear");

            // Act - re-add prefabs
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("NewKey1", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("NewKey2", testPrefab2);

            // Assert
            Assert.AreEqual(2, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 2 prefabs after re-add");

            GONetParticipant retrieved1, retrieved2;
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("NewKey1", out retrieved1), "Should retrieve prefab 1");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("NewKey2", out retrieved2), "Should retrieve prefab 2");
            Assert.AreEqual(testPrefab1, retrieved1, "Prefab 1 should match");
            Assert.AreEqual(testPrefab2, retrieved2, "Prefab 2 should match");
        }

        #endregion

        #region Memory Management Use Cases

        [Test]
        public void RemoveCachedAddressablePrefab_OneOffSpawn_FreesMemory()
        {
            // Simulate use case: Boss battle (one-time spawn, want to free memory after defeat)

            // Arrange - cache boss prefab
            const string bossKey = "BossPrefab";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(bossKey, testPrefab1);
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Boss prefab should be cached");

            // Act - boss defeated, remove from cache to free memory
            bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab(bossKey);

            // Assert
            Assert.IsTrue(removed, "Should successfully remove boss prefab");
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should be empty (memory freed)");

            GONetParticipant retrievedPrefab;
            Assert.IsFalse(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(bossKey, out retrievedPrefab), "Boss prefab should not be in cache");
        }

        [Test]
        public void ClearAddressablePrefabCache_LevelTransition_FreesAllMemory()
        {
            // Simulate use case: Transitioning from main menu to gameplay, clear menu prefabs

            // Arrange - cache menu prefabs
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("MenuButton", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("MenuPanel", testPrefab2);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("MenuBackground", testPrefab3);
            Assert.AreEqual(3, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Menu prefabs should be cached");

            // Act - transition to gameplay, clear all menu prefabs to free memory
            GONetSpawnSupport_Runtime.ClearAddressablePrefabCache();

            // Assert
            Assert.AreEqual(0, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "All menu prefabs should be cleared");

            // Now cache gameplay prefabs
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("PlayerPrefab", testPrefab1);
            GONetSpawnSupport_Runtime.CacheAddressablePrefab("EnemyPrefab", testPrefab2);
            Assert.AreEqual(2, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Gameplay prefabs should be cached");
        }

        #endregion

        #region Stress Tests

        [Test]
        public void CacheAddressablePrefab_ManyPrefabs_HandlesCorrectly()
        {
            // Arrange - simulate caching many prefabs (e.g., 50+ weapon types)
            const int prefabCount = 100;
            GONetParticipant[] prefabs = new GONetParticipant[prefabCount];

            // Act - cache 100 prefabs
            for (int i = 0; i < prefabCount; i++)
            {
                prefabs[i] = CreateMockGONetParticipant($"Weapon{i}");
                GONetSpawnSupport_Runtime.CacheAddressablePrefab($"Weapon{i}", prefabs[i]);
            }

            // Assert - all prefabs should be cached
            Assert.AreEqual(prefabCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 100 prefabs");

            // Verify random spot checks
            GONetParticipant retrieved0, retrieved50, retrieved99;
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Weapon0", out retrieved0), "Weapon0 should be cached");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Weapon50", out retrieved50), "Weapon50 should be cached");
            Assert.IsTrue(GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab("Weapon99", out retrieved99), "Weapon99 should be cached");

            Assert.AreEqual(prefabs[0], retrieved0, "Weapon0 should match");
            Assert.AreEqual(prefabs[50], retrieved50, "Weapon50 should match");
            Assert.AreEqual(prefabs[99], retrieved99, "Weapon99 should match");

            // Cleanup
            foreach (var prefab in prefabs)
            {
                if (prefab != null) Object.DestroyImmediate(prefab.gameObject);
            }
        }

        [Test]
        public void RemoveCachedAddressablePrefab_RemoveManyPrefabs_HandlesCorrectly()
        {
            // Arrange - cache 50 prefabs
            const int prefabCount = 50;
            GONetParticipant[] prefabs = new GONetParticipant[prefabCount];

            for (int i = 0; i < prefabCount; i++)
            {
                prefabs[i] = CreateMockGONetParticipant($"Prefab{i}");
                GONetSpawnSupport_Runtime.CacheAddressablePrefab($"Prefab{i}", prefabs[i]);
            }

            Assert.AreEqual(prefabCount, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 50 prefabs");

            // Act - remove every other prefab (25 removals)
            for (int i = 0; i < prefabCount; i += 2)
            {
                bool removed = GONetSpawnSupport_Runtime.RemoveCachedAddressablePrefab($"Prefab{i}");
                Assert.IsTrue(removed, $"Should remove Prefab{i}");
            }

            // Assert - 25 prefabs remaining
            Assert.AreEqual(25, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Should have 25 prefabs remaining");

            // Verify even-numbered prefabs removed, odd-numbered still exist
            for (int i = 0; i < prefabCount; i++)
            {
                GONetParticipant retrievedPrefab;
                bool found = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab($"Prefab{i}", out retrievedPrefab);

                if (i % 2 == 0)
                {
                    Assert.IsFalse(found, $"Prefab{i} (even) should be removed");
                }
                else
                {
                    Assert.IsTrue(found, $"Prefab{i} (odd) should still exist");
                    Assert.AreEqual(prefabs[i], retrievedPrefab, $"Prefab{i} should match original");
                }
            }

            // Cleanup
            foreach (var prefab in prefabs)
            {
                if (prefab != null) Object.DestroyImmediate(prefab.gameObject);
            }
        }

        #endregion

        #region Integration Simulation Tests

        [Test]
        public void LookupTemplateFromAddressableOrResources_UsesCache_WhenAvailable()
        {
            // This test simulates the internal spawn system's cache usage pattern
            // NOTE: Full integration test with LookupTemplateFromAddressableOrResources()
            // requires GONet runtime initialization and is documented separately.

            // Arrange - simulate cache warmup (as if PreloadGONetAddressablePrefabs() was called)
            const string addressablePath = "Assets/GONet/Sample/Projectile/AddressablesOohLaLa/Physics Cube Projectile.prefab";
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(addressablePath, testPrefab1);

            // Act - simulate spawn system lookup (cache hit path)
            GONetParticipant retrievedPrefab;
            bool cacheHit = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab);

            // Assert
            Assert.IsTrue(cacheHit, "Spawn system should hit cache");
            Assert.AreEqual(testPrefab1, retrievedPrefab, "Should retrieve cached prefab");

            // Verify cache count unchanged (no duplicate caching)
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should still have 1 prefab");
        }

        [Test]
        public void SequentialSpawns_UseCache_NotReloadingEachTime()
        {
            // Simulate ProjectileSpawner.InstantiateAddressablesPrefab() calling LoadGONetPrefabAsync_Cached()
            // 5 times per click - should only load once, then use cache

            // Arrange - simulate first call (cache miss, would load from Addressables)
            const string addressablePath = "Assets/GONet/Sample/Projectile/AddressablesOohLaLa/Physics Cube Projectile.prefab";
            GONetParticipant retrievedPrefab1;
            bool firstCall = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab1);
            Assert.IsFalse(firstCall, "First call should be cache miss");

            // Simulate loading from Addressables and caching
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(addressablePath, testPrefab1);

            // Act - simulate subsequent calls (cache hits, no Addressables load)
            GONetParticipant retrievedPrefab2, retrievedPrefab3, retrievedPrefab4, retrievedPrefab5;
            bool secondCall = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab2);
            bool thirdCall = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab3);
            bool fourthCall = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab4);
            bool fifthCall = GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressablePath, out retrievedPrefab5);

            // Assert - all subsequent calls are cache hits
            Assert.IsTrue(secondCall, "Second call should be cache hit");
            Assert.IsTrue(thirdCall, "Third call should be cache hit");
            Assert.IsTrue(fourthCall, "Fourth call should be cache hit");
            Assert.IsTrue(fifthCall, "Fifth call should be cache hit");

            // All retrieved prefabs should be the same instance
            Assert.AreEqual(testPrefab1, retrievedPrefab2, "Prefab 2 should match cached prefab");
            Assert.AreEqual(testPrefab1, retrievedPrefab3, "Prefab 3 should match cached prefab");
            Assert.AreEqual(testPrefab1, retrievedPrefab4, "Prefab 4 should match cached prefab");
            Assert.AreEqual(testPrefab1, retrievedPrefab5, "Prefab 5 should match cached prefab");

            // Cache should still have only 1 prefab (no duplicate caching)
            Assert.AreEqual(1, GONetSpawnSupport_Runtime.GetCachedAddressablePrefabCount(), "Cache should have 1 prefab");
        }

        #endregion
    }
}
