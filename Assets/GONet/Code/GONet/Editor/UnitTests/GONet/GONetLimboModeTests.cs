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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace GONet
{
    /// <summary>
    /// Unit tests for CLIENT LIMBO MODE - the RARE edge case handling for GONetId batch exhaustion.
    ///
    /// Limbo mode only occurs when:
    /// 1. Client exhausts all available batch IDs (100-1000 IDs per batch)
    /// 2. Client spawns more objects before server's next batch arrives
    /// 3. Objects exist locally but have no GONetId (waiting for batch from server)
    ///
    /// These tests ensure bulletproof operation across:
    /// - Limbo instantiation (objects spawn locally without GONetId)
    /// - Three limbo modes (DisableAll, DisableRenderingAndPhysics, NoDisable)
    /// - Graduation process (objects assigned GONetId when batch arrives)
    /// - OnGONetReady blocking (not called during limbo, called after graduation)
    /// - Inspector integration (limbo count, diagnostics, participant list)
    /// </summary>
    [TestFixture]
    [Ignore("Requires Play Mode testing with full GONet initialization - deferred for real-world validation")]
    public class GONetLimboModeTests
    {
        private GameObject testPrefab;
        private GONetParticipant testPrefabGNP;

        [SetUp]
        public void Setup()
        {
            // Reset all state before each test
            GONetIdBatchManager.Server_ResetAllBatches();
            GONetIdBatchManager.Client_ResetAllBatches();

            // Create a simple test prefab with GONetParticipant
            testPrefab = new GameObject("TestPrefab");
            testPrefabGNP = testPrefab.AddComponent<GONetParticipant>();

            // Add a test component to verify disable/enable behavior
            testPrefab.AddComponent<BoxCollider>(); // For physics testing
            testPrefab.AddComponent<MeshRenderer>(); // For rendering testing
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            GONetIdBatchManager.Server_ResetAllBatches();
            GONetIdBatchManager.Client_ResetAllBatches();

            if (testPrefab != null)
            {
                Object.DestroyImmediate(testPrefab);
            }
        }

        #region Limbo Instantiation Tests

        [UnityTest]
        public IEnumerator Client_TryInstantiate_SucceedsWhenBatchExhausted_EntersLimbo()
        {
            // Arrange - NO batches available
            // This simulates the RARE edge case where client exhausted all batch IDs
            // Override prefab to use InstantiateInLimbo mode instead of ReturnFailure
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            // Act - attempt to spawn with NoDisable mode
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert
            Assert.IsTrue(success, "Spawn should succeed even without batch IDs (enters limbo)");
            Assert.IsNotNull(spawnedGNP, "Should return spawned GONetParticipant");
            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should be in limbo state");
            Assert.AreEqual(GONetParticipant.GONetIdRaw_Unset, spawnedGNP.GONetId, "Should have no GONetId yet");

            // Verify limbo count
            int limboCount = GONetMain.Client_GetLimboCount();
            Assert.AreEqual(1, limboCount, "Should have 1 participant in limbo");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_TryInstantiate_SucceedsWhenBatchAvailable_DoesNotEnterLimbo()
        {
            // Arrange - add a batch
            GONetIdBatchManager.Client_AddBatch(5000);

            // Act - spawn with normal behavior (not forcing limbo)
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert
            Assert.IsTrue(success, "Spawn should succeed with batch IDs available");
            Assert.IsNotNull(spawnedGNP, "Should return spawned GONetParticipant");
            Assert.IsFalse(spawnedGNP.Client_IsInLimbo, "Should NOT be in limbo when batch available");
            Assert.AreNotEqual(GONetParticipant.GONetIdRaw_Unset, spawnedGNP.GONetId, "Should have GONetId assigned");

            // Verify limbo count is zero
            int limboCount = GONetMain.Client_GetLimboCount();
            Assert.AreEqual(0, limboCount, "Should have 0 participants in limbo");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_TryInstantiate_ForceLimboMode_EntersLimboEvenWithBatchAvailable()
        {
            // Arrange - add a batch (IDs available), but FORCE limbo mode with override
            GONetIdBatchManager.Client_AddBatch(5000);
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            // Act - FORCE limbo mode with override (for testing/debugging)
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert
            Assert.IsTrue(success, "Spawn should succeed when forcing limbo");
            Assert.IsNotNull(spawnedGNP, "Should return spawned GONetParticipant");
            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should be in limbo even with batch available (forced)");
            Assert.AreEqual(GONetParticipant.GONetIdRaw_Unset, spawnedGNP.GONetId, "Should have no GONetId yet");

            // Verify limbo count
            int limboCount = GONetMain.Client_GetLimboCount();
            Assert.AreEqual(1, limboCount, "Should have 1 participant in limbo");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        #endregion

        #region Limbo Mode Behavior Tests

        [UnityTest]
        public IEnumerator Client_LimboMode_DisableAll_FreezesAllComponents()
        {
            // Arrange - NO batches available
            testPrefab.AddComponent<TestMonoBehaviour>(); // Custom test component
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableAll;

            // Act - spawn with DisableAll mode (FROZEN state)
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify all MonoBehaviours are disabled (except GONetParticipant)
            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should be in limbo");
            Assert.IsNotNull(spawnedGNP.client_limboDisabledComponents, "Should have disabled components list");
            Assert.Greater(spawnedGNP.client_limboDisabledComponents.Count, 0, "Should have disabled some components");

            // Verify BoxCollider, MeshRenderer, and TestMonoBehaviour are disabled
            var collider = spawnedGNP.GetComponent<BoxCollider>();
            var renderer = spawnedGNP.GetComponent<MeshRenderer>();
            var testComponent = spawnedGNP.GetComponent<TestMonoBehaviour>();

            Assert.IsFalse(collider.enabled, "BoxCollider should be disabled in FROZEN mode");
            Assert.IsFalse(renderer.enabled, "MeshRenderer should be disabled in FROZEN mode");
            Assert.IsFalse(testComponent.enabled, "TestMonoBehaviour should be disabled in FROZEN mode");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_LimboMode_DisableRenderingAndPhysics_HidesObjectButAllowsScripts()
        {
            // Arrange - NO batches available
            testPrefab.AddComponent<TestMonoBehaviour>(); // Custom test component
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableRenderingAndPhysics;

            // Act - spawn with DisableRenderingAndPhysics mode (HIDDEN state)
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify renderers/colliders disabled, but MonoBehaviours still enabled
            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should be in limbo");
            Assert.IsNotNull(spawnedGNP.client_limboDisabledRenderers, "Should have disabled renderers list");

            var collider = spawnedGNP.GetComponent<BoxCollider>();
            var renderer = spawnedGNP.GetComponent<MeshRenderer>();
            var testComponent = spawnedGNP.GetComponent<TestMonoBehaviour>();

            Assert.IsFalse(collider.enabled, "BoxCollider should be disabled in HIDDEN mode");
            Assert.IsFalse(renderer.enabled, "MeshRenderer should be disabled in HIDDEN mode");
            Assert.IsTrue(testComponent.enabled, "TestMonoBehaviour should STILL BE ENABLED in HIDDEN mode");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_LimboMode_NoDisable_AllComponentsStayActive()
        {
            // Arrange - NO batches available
            testPrefab.AddComponent<TestMonoBehaviour>(); // Custom test component
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            // Act - spawn with NoDisable mode (user checks Client_IsInLimbo manually)
            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify all components are still enabled
            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should be in limbo");
            Assert.IsNull(spawnedGNP.client_limboDisabledComponents, "Should NOT have disabled components list");
            Assert.IsNull(spawnedGNP.client_limboDisabledRenderers, "Should NOT have disabled renderers list");

            var collider = spawnedGNP.GetComponent<BoxCollider>();
            var renderer = spawnedGNP.GetComponent<MeshRenderer>();
            var testComponent = spawnedGNP.GetComponent<TestMonoBehaviour>();

            Assert.IsTrue(collider.enabled, "BoxCollider should STILL BE ENABLED in NoDisable mode");
            Assert.IsTrue(renderer.enabled, "MeshRenderer should STILL BE ENABLED in NoDisable mode");
            Assert.IsTrue(testComponent.enabled, "TestMonoBehaviour should STILL BE ENABLED in NoDisable mode");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        #endregion

        #region Graduation Tests (Limbo Exit)

        [UnityTest]
        public IEnumerator Client_GraduateFromLimbo_AssignsGONetId_RestoresComponents()
        {
            // Arrange - spawn in limbo with DisableAll mode
            testPrefab.AddComponent<TestMonoBehaviour>();
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableAll;

            GONetParticipant spawnedGNP;
            bool success = GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(
                testPrefabGNP,
                Vector3.zero,
                Quaternion.identity,
                out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            Assert.IsTrue(spawnedGNP.Client_IsInLimbo, "Should start in limbo");

            var collider = spawnedGNP.GetComponent<BoxCollider>();
            var renderer = spawnedGNP.GetComponent<MeshRenderer>();
            var testComponent = spawnedGNP.GetComponent<TestMonoBehaviour>();

            Assert.IsFalse(collider.enabled, "Should be disabled before graduation");
            Assert.IsFalse(renderer.enabled, "Should be disabled before graduation");
            Assert.IsFalse(testComponent.enabled, "Should be disabled before graduation");

            // Act - simulate batch arrival and graduation (automatic via event system)
            GONetIdBatchManager.Client_AddBatch(5000); // Process limbo queue to graduate participants

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify graduation
            Assert.IsFalse(spawnedGNP.Client_IsInLimbo, "Should have graduated from limbo");
            Assert.AreNotEqual(GONetParticipant.GONetIdRaw_Unset, spawnedGNP.GONetId, "Should have GONetId assigned");
            Assert.AreEqual(5000, spawnedGNP.GONetId, "Should have first ID from batch");

            // Verify components re-enabled
            Assert.IsTrue(collider.enabled, "BoxCollider should be re-enabled after graduation");
            Assert.IsTrue(renderer.enabled, "MeshRenderer should be re-enabled after graduation");
            Assert.IsTrue(testComponent.enabled, "TestMonoBehaviour should be re-enabled after graduation");

            // Verify limbo count is zero
            int limboCount = GONetMain.Client_GetLimboCount();
            Assert.AreEqual(0, limboCount, "Should have 0 participants in limbo after graduation");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_GraduateMultipleParticipants_AssignsSequentialGONetIds()
        {
            // Arrange - spawn 3 participants in limbo
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            GONetParticipant gnp1, gnp2, gnp3;
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp1);
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp2);
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp3);

            yield return null; // Wait one frame for Unity lifecycle

            Assert.AreEqual(3, GONetMain.Client_GetLimboCount(), "Should have 3 participants in limbo");

            // Act - simulate batch arrival and graduation (automatic via event system)
            GONetIdBatchManager.Client_AddBatch(5000);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify all graduated with sequential IDs
            Assert.IsFalse(gnp1.Client_IsInLimbo, "GNP1 should have graduated");
            Assert.IsFalse(gnp2.Client_IsInLimbo, "GNP2 should have graduated");
            Assert.IsFalse(gnp3.Client_IsInLimbo, "GNP3 should have graduated");

            Assert.AreEqual(5000, gnp1.GONetId, "GNP1 should have first ID");
            Assert.AreEqual(5001, gnp2.GONetId, "GNP2 should have second ID");
            Assert.AreEqual(5002, gnp3.GONetId, "GNP3 should have third ID");

            Assert.AreEqual(0, GONetMain.Client_GetLimboCount(), "Should have 0 participants in limbo after graduation");

            // Cleanup
            Object.DestroyImmediate(gnp1.gameObject);
            Object.DestroyImmediate(gnp2.gameObject);
            Object.DestroyImmediate(gnp3.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_GraduateFromLimbo_PartialBatch_GraduatesAsManyAsPossible()
        {
            // Arrange - spawn 150 participants in limbo
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            List<GONetParticipant> limboParticipants = new List<GONetParticipant>();
            for (int i = 0; i < 150; i++)
            {
                GONetParticipant gnp;
                GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp);
                limboParticipants.Add(gnp);
            }

            yield return null; // Wait one frame for Unity lifecycle

            Assert.AreEqual(150, GONetMain.Client_GetLimboCount(), "Should have 150 participants in limbo");

            // Act - add batch with only 100 IDs (not enough for all)
            GONetIdBatchManager.Client_AddBatch(5000);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - verify first 100 graduated, last 50 still in limbo
            int graduatedCount = 0;
            int stillInLimboCount = 0;

            foreach (var gnp in limboParticipants)
            {
                if (gnp.Client_IsInLimbo)
                {
                    stillInLimboCount++;
                }
                else
                {
                    graduatedCount++;
                }
            }

            Assert.AreEqual(100, graduatedCount, "Should have graduated 100 participants (batch size)");
            Assert.AreEqual(50, stillInLimboCount, "Should still have 50 participants in limbo");
            Assert.AreEqual(50, GONetMain.Client_GetLimboCount(), "Limbo count should be 50");

            // Verify first 100 have sequential IDs
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(5000 + i, limboParticipants[i].GONetId, $"Participant {i} should have ID {5000 + i}");
            }

            // Verify last 50 still have no GONetId
            for (int i = 100; i < 150; i++)
            {
                Assert.AreEqual(GONetParticipant.GONetIdRaw_Unset, limboParticipants[i].GONetId, $"Participant {i} should still have no GONetId");
            }

            // Cleanup
            foreach (var gnp in limboParticipants)
            {
                Object.DestroyImmediate(gnp.gameObject);
            }
        }

        #endregion

        #region OnGONetReady Blocking Tests

        [UnityTest]
        public IEnumerator Client_OnGONetReady_NotCalledDuringLimbo()
        {
            // Arrange - add test component that tracks OnGONetReady calls
            var testComponent = testPrefab.AddComponent<TestGONetBehaviour>();
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            // Act - spawn in limbo
            GONetParticipant spawnedGNP;
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle
            yield return null; // Wait another frame to ensure OnGONetReady would have been called

            // Assert - OnGONetReady should NOT have been called yet
            var spawnedTestComponent = spawnedGNP.GetComponent<TestGONetBehaviour>();
            Assert.IsNotNull(spawnedTestComponent, "Test component should exist");
            Assert.IsFalse(spawnedTestComponent.wasOnGONetReadyCalled, "OnGONetReady should NOT be called during limbo");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        [UnityTest]
        public IEnumerator Client_OnGONetReady_CalledAfterGraduation()
        {
            // Arrange - add test component that tracks OnGONetReady calls
            var testComponent = testPrefab.AddComponent<TestGONetBehaviour>();
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            // Act - spawn in limbo
            GONetParticipant spawnedGNP;
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out spawnedGNP);

            yield return null; // Wait one frame for Unity lifecycle

            var spawnedTestComponent = spawnedGNP.GetComponent<TestGONetBehaviour>();
            Assert.IsFalse(spawnedTestComponent.wasOnGONetReadyCalled, "OnGONetReady should NOT be called during limbo");

            // Simulate batch arrival and graduation (automatic via event system)
            GONetIdBatchManager.Client_AddBatch(5000);

            yield return null; // Wait one frame for Unity lifecycle
            yield return null; // Wait another frame to ensure OnGONetReady gets called

            // Assert - OnGONetReady should NOW be called after graduation
            Assert.IsTrue(spawnedTestComponent.wasOnGONetReadyCalled, "OnGONetReady should be called after graduation");
            Assert.AreEqual(spawnedGNP, spawnedTestComponent.receivedGONetParticipant, "Should receive correct GONetParticipant in OnGONetReady");

            // Cleanup
            Object.DestroyImmediate(spawnedGNP.gameObject);
        }

        #endregion

        #region Inspector Integration Tests

        [Test]
        public void Client_GetLimboCount_ReturnsCorrectCount()
        {
            // Arrange - spawn 5 participants in limbo
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            List<GONetParticipant> limboParticipants = new List<GONetParticipant>();
            for (int i = 0; i < 5; i++)
            {
                GONetParticipant gnp;
                GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp);
                limboParticipants.Add(gnp);
            }

            // Act
            int limboCount = GONetMain.Client_GetLimboCount();

            // Assert
            Assert.AreEqual(5, limboCount, "Should return correct limbo count");

            // Cleanup
            foreach (var gnp in limboParticipants)
            {
                Object.DestroyImmediate(gnp.gameObject);
            }
        }

        [Test]
        public void Client_GetLimboParticipants_ReturnsCorrectParticipants()
        {
            // Arrange - spawn 3 participants in limbo
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            List<GONetParticipant> expectedLimboParticipants = new List<GONetParticipant>();
            for (int i = 0; i < 3; i++)
            {
                GONetParticipant gnp;
                GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp);
                expectedLimboParticipants.Add(gnp);
            }

            // Act
            var actualLimboParticipants = GONetMain.Client_GetLimboParticipants().ToList();

            // Assert
            Assert.AreEqual(3, actualLimboParticipants.Count, "Should return all limbo participants");
            foreach (var expectedGNP in expectedLimboParticipants)
            {
                Assert.IsTrue(actualLimboParticipants.Contains(expectedGNP), $"Should contain {expectedGNP.name}");
            }

            // Cleanup
            foreach (var gnp in expectedLimboParticipants)
            {
                Object.DestroyImmediate(gnp.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Client_GetLimboParticipants_UpdatesAfterGraduation()
        {
            // Arrange - spawn 2 participants in limbo
            testPrefabGNP.client_overrideLimboMode = true;
            testPrefabGNP.client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimbo;

            GONetParticipant gnp1, gnp2;
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp1);
            GONetMain.Client_TryInstantiateToBeRemotelyControlledByMe(testPrefabGNP, Vector3.zero, Quaternion.identity, out gnp2);

            yield return null; // Wait one frame for Unity lifecycle

            Assert.AreEqual(2, GONetMain.Client_GetLimboCount(), "Should have 2 in limbo");

            // Act - graduate participants (automatic via event system)
            GONetIdBatchManager.Client_AddBatch(5000);

            yield return null; // Wait one frame for Unity lifecycle

            // Assert - limbo list should be empty
            Assert.AreEqual(0, GONetMain.Client_GetLimboCount(), "Should have 0 in limbo after graduation");
            var limboParticipants = GONetMain.Client_GetLimboParticipants().ToList();
            Assert.AreEqual(0, limboParticipants.Count, "Limbo participants list should be empty");

            // Cleanup
            Object.DestroyImmediate(gnp1.gameObject);
            Object.DestroyImmediate(gnp2.gameObject);
        }

        #endregion

        #region Helper Components

        /// <summary>
        /// Simple test MonoBehaviour for verifying enable/disable behavior
        /// </summary>
        private class TestMonoBehaviour : MonoBehaviour
        {
            // Empty test component
        }

        /// <summary>
        /// Test GONetBehaviour that tracks OnGONetReady calls
        /// </summary>
        private class TestGONetBehaviour : GONetBehaviour
        {
            public bool wasOnGONetReadyCalled = false;
            public GONetParticipant receivedGONetParticipant = null;

            public override void OnGONetReady(GONetParticipant gonetParticipant)
            {
                base.OnGONetReady(gonetParticipant);
                wasOnGONetReadyCalled = true;
                receivedGONetParticipant = gonetParticipant;
            }
        }

        #endregion
    }
}
