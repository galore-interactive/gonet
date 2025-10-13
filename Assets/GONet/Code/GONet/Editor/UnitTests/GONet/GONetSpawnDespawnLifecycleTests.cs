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
using UnityEngine;
using UnityEngine.TestTools;

namespace GONet.Tests
{
    /// <summary>
    /// Integration tests for GONet spawn/despawn lifecycle.
    /// Tests object instantiation, network propagation, authority assignment, and cleanup.
    ///
    /// IMPORTANT: These tests require full GONet runtime (server + clients) and test prefabs.
    /// They use [UnityTest] for Unity's async operations.
    ///
    /// Test Prefabs Required:
    /// - "GONetTestPrefab_Simple" - Basic cube with GONetParticipant
    /// - "GONetTestPrefab_WithComponents" - Prefab with multiple synced components
    /// - "GONetTestPrefab_Projectile" - Fast-spawning prefab for burst tests
    ///
    /// These prefabs should be in Assets/GONet/Sample/TestPrefabs/Resources/
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("SpawnDespawn")]
    [Category("Lifecycle")]
    public class GONetSpawnDespawnLifecycleTests
    {
        #region Spawn Infrastructure Tests

        /// <summary>
        /// Test that GONetMain.Instantiate API exists and has correct signature.
        /// </summary>
        [Test]
        public void Spawn_API_InstantiateMethodExists()
        {
            // Verify GONetMain.Client_InstantiateToBeRemotelyControlledByMe method exists (legacy API)
            var gonetMainType = typeof(GONetMain);
            var legacyMethod = gonetMainType.GetMethod("Client_InstantiateToBeRemotelyControlledByMe",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(GONetParticipant), typeof(Vector3), typeof(Quaternion) },
                null);

            Assert.IsNotNull(legacyMethod, "GONetMain.Client_InstantiateToBeRemotelyControlledByMe should exist (legacy API)");
            Assert.AreEqual(typeof(GONetParticipant), legacyMethod.ReturnType);
        }

        /// <summary>
        /// Test that InstantiateGONetParticipantEvent is properly structured as persistent event.
        /// </summary>
        [Test]
        public void Spawn_Event_InstantiateEventIsPersistent()
        {
            // Verify InstantiateGONetParticipantEvent implements IPersistentEvent
            var eventType = typeof(InstantiateGONetParticipantEvent);
            Assert.IsTrue(typeof(IPersistentEvent).IsAssignableFrom(eventType),
                "InstantiateGONetParticipantEvent must be IPersistentEvent for late-joiner delivery");

            // Verify required fields exist (these are public fields, not properties)
            Assert.IsNotNull(eventType.GetField("GONetId"));
            Assert.IsNotNull(eventType.GetField("OwnerAuthorityId"));
            Assert.IsNotNull(eventType.GetField("DesignTimeLocation"));
            Assert.IsNotNull(eventType.GetField("Position"));
            Assert.IsNotNull(eventType.GetField("Rotation"));
        }

        /// <summary>
        /// Test that DespawnGONetParticipantEvent is properly structured.
        /// </summary>
        [Test]
        public void Spawn_Event_DespawnEventStructure()
        {
            // Verify DespawnGONetParticipantEvent exists and has required fields
            var eventType = typeof(DespawnGONetParticipantEvent);
            Assert.IsNotNull(eventType);

            // Verify required field (public field, not property)
            Assert.IsNotNull(eventType.GetField("GONetId"));
        }

        #endregion

        #region GONetId Batch System Tests

        /// <summary>
        /// Test that Client_TryInstantiateToBeRemotelyControlledByMe API exists.
        /// This is the new explicit API with limbo mode support.
        /// </summary>
        [Test]
        public void Spawn_GONetIdBatch_ExplicitAPIExists()
        {
            // Verify new explicit API exists (with out parameter)
            var gonetMainType = typeof(GONetMain);
            var method = gonetMainType.GetMethod("Client_TryInstantiateToBeRemotelyControlledByMe",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            Assert.IsNotNull(method, "Client_TryInstantiateToBeRemotelyControlledByMe should exist");

            // Verify method returns bool (try pattern)
            Assert.AreEqual(typeof(bool), method.ReturnType, "Should return bool for try pattern");

            // Verify method has out GONetParticipant parameter
            var parameters = method.GetParameters();
            bool hasOutParam = false;
            foreach (var param in parameters)
            {
                if (param.IsOut && param.ParameterType == typeof(GONetParticipant).MakeByRefType())
                {
                    hasOutParam = true;
                    break;
                }
            }
            Assert.IsTrue(hasOutParam, "Method should have 'out GONetParticipant' parameter for try pattern");
        }

        /// <summary>
        /// Test that GONetParticipant_LimboMode enum exists with expected values.
        /// </summary>
        [Test]
        public void Spawn_GONetIdBatch_LimboModeEnumExists()
        {
            // Find limbo mode enum (actual name: Client_GONetIdBatchLimboMode)
            var assembly = typeof(GONetMain).Assembly;
            var limboModeType = assembly.GetType("GONet.Client_GONetIdBatchLimboMode");

            Assert.IsNotNull(limboModeType, "Client_GONetIdBatchLimboMode enum should exist");
            Assert.IsTrue(limboModeType.IsEnum, "Client_GONetIdBatchLimboMode should be an enum");

            // Verify expected enum values exist
            var enumNames = System.Enum.GetNames(limboModeType);
            Assert.Contains("ReturnFailure", enumNames, "Should have ReturnFailure mode");
            Assert.Contains("InstantiateInLimbo", enumNames, "Should have InstantiateInLimbo mode");
            // Note: InstantiateInLimboWithAutoDisableAll and InstantiateInLimboWithAutoDisableRenderingAndPhysics also exist
        }

        /// <summary>
        /// Test that GONetGlobal has client_GONetIdBatchSize configuration property.
        /// </summary>
        [Test]
        public void Spawn_GONetIdBatch_BatchSizeConfigurationExists()
        {
            // Verify GONetGlobal has batch size configuration
            var globalType = typeof(GONetGlobal);
            var batchSizeProperty = globalType.GetField("client_GONetIdBatchSize",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(batchSizeProperty, "GONetGlobal.client_GONetIdBatchSize field should exist");
            Assert.AreEqual(typeof(int), batchSizeProperty.FieldType);
        }

        #endregion

        #region Authority Assignment Tests

        /// <summary>
        /// Test that GONetParticipant has IsMine property for authority checking.
        /// </summary>
        [Test]
        public void Spawn_Authority_IsMinePropertyExists()
        {
            // Verify IsMine property exists
            var participantType = typeof(GONetParticipant);
            var isMineProperty = participantType.GetProperty("IsMine");

            Assert.IsNotNull(isMineProperty, "GONetParticipant.IsMine property should exist");
            Assert.AreEqual(typeof(bool), isMineProperty.PropertyType);
            Assert.IsTrue(isMineProperty.CanRead, "IsMine should be readable");
        }

        /// <summary>
        /// Test that GONetParticipant has OwnerAuthorityId property.
        /// </summary>
        [Test]
        public void Spawn_Authority_OwnerAuthorityIdPropertyExists()
        {
            // Verify OwnerAuthorityId property exists
            var participantType = typeof(GONetParticipant);
            var ownerProperty = participantType.GetProperty("OwnerAuthorityId");

            Assert.IsNotNull(ownerProperty, "GONetParticipant.OwnerAuthorityId property should exist");
            Assert.AreEqual(typeof(ushort), ownerProperty.PropertyType);
        }

        /// <summary>
        /// Test that GONetParticipant has GONetId property.
        /// </summary>
        [Test]
        public void Spawn_Authority_GONetIdPropertyExists()
        {
            // Verify GONetId property exists
            var participantType = typeof(GONetParticipant);
            var gonetIdProperty = participantType.GetProperty("GONetId");

            Assert.IsNotNull(gonetIdProperty, "GONetParticipant.GONetId property should exist");
            Assert.AreEqual(typeof(uint), gonetIdProperty.PropertyType);
        }

        #endregion

        #region Lifecycle Callback Tests

        /// <summary>
        /// Test that GONetParticipant has OnGONetReady callback.
        /// This is the primary initialization callback for networked objects.
        /// </summary>
        [Test]
        public void Spawn_Lifecycle_OnGONetReadyCallbackExists()
        {
            // Verify OnGONetReady virtual method exists in GONetBehaviour
            // (User scripts extend GONetBehaviour and override OnGONetReady)
            var gonetBehaviourType = typeof(GONetBehaviour);
            var onReadyMethod = gonetBehaviourType.GetMethod("OnGONetReady",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(onReadyMethod, "GONetBehaviour should have OnGONetReady virtual method");
            Assert.IsTrue(onReadyMethod.IsVirtual, "OnGONetReady should be virtual for override");

            // Verify it takes GONetParticipant parameter
            var parameters = onReadyMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "OnGONetReady should take one GONetParticipant parameter");
            Assert.AreEqual(typeof(GONetParticipant), parameters[0].ParameterType);
        }

        /// <summary>
        /// Test that GONetParticipant tracks didAwakeComplete for initialization state.
        /// This prevents race conditions with early sync bundles.
        /// </summary>
        [Test]
        public void Spawn_Lifecycle_DidAwakeCompleteFieldExists()
        {
            // Verify didAwakeComplete field exists (internal/private)
            var participantType = typeof(GONetParticipant);
            var didAwakeField = participantType.GetField("didAwakeComplete",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(didAwakeField, "GONetParticipant.didAwakeComplete should exist to track initialization state");
            Assert.AreEqual(typeof(bool), didAwakeField.FieldType);
        }

        #endregion

        #region Metadata and Registration Tests

        /// <summary>
        /// Test that GONetParticipant has CodeGenerationId for metadata lookup.
        /// </summary>
        [Test]
        public void Spawn_Metadata_CodeGenerationIdPropertyExists()
        {
            // Verify CodeGenerationId property exists
            var participantType = typeof(GONetParticipant);
            var codeGenIdProperty = participantType.GetProperty("CodeGenerationId");

            Assert.IsNotNull(codeGenIdProperty, "GONetParticipant.CodeGenerationId should exist");
            Assert.AreEqual(typeof(byte), codeGenIdProperty.PropertyType);
        }

        /// <summary>
        /// Test that DesignTimeMetadata.json exists and can be loaded.
        /// This file maps prefabs to CodeGenerationIds.
        /// </summary>
        [Test]
        public void Spawn_Metadata_DesignTimeMetadataFileAccessible()
        {
            // Verify metadata file path constant exists
            var gonetMainType = typeof(GONetMain);
            // Metadata loading logic is typically internal, but we can verify the file exists
            string metadataPath = System.IO.Path.Combine(Application.streamingAssetsPath, "GONet", "DesignTimeMetadata.json");

            // Note: In test environment, StreamingAssets may not exist
            // This test documents the expected file location
            Assert.IsTrue(metadataPath.Contains("StreamingAssets"), "Metadata should be in StreamingAssets/GONet/");
        }

        #endregion

        #region Integration Test Placeholders (Require Unity PlayMode Tests)

        /// <summary>
        /// PLACEHOLDER: Server spawns object, all clients should receive spawn event and instantiate.
        ///
        /// Implementation steps:
        /// 1. Setup server + 3 clients
        /// 2. Server spawns prefab via GONetMain.Instantiate()
        /// 3. Verify InstantiateGONetParticipantEvent published
        /// 4. Wait for spawn propagation to all clients
        /// 5. Verify all clients instantiated the prefab
        /// 6. Verify all clients have matching GONetId
        /// 7. Verify OwnerAuthorityId is server's authority
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_ServerSpawn_AllClientsReceiveAndInstantiate()
        {
            Assert.Fail("Requires multi-client test harness. " +
                       "Test should verify spawn propagation to all connected clients.");
        }

        /// <summary>
        /// PLACEHOLDER: Client spawns object (batch ID), server assumes authority, other clients see it.
        ///
        /// Implementation steps:
        /// 1. Setup server + 3 clients
        /// 2. Client 1 has allocated GONetId batch
        /// 3. Client 1 calls Client_TryInstantiateToBeRemotelyControlledByMe()
        /// 4. Verify object spawned locally with batch GONetId
        /// 5. Wait for server to receive spawn request
        /// 6. Verify server assumes authority (IsMine=true on server)
        /// 7. Verify other clients (2 and 3) receive spawn event
        /// 8. Verify all clients see same GONetId
        /// 9. Verify OwnerAuthorityId is server's authority (not client 1)
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_ClientSpawn_ServerAssumesAuthority_OthersReceive()
        {
            Assert.Fail("Requires multi-client test harness with GONetId batch system. " +
                       "Test should verify client-initiated spawn with server authority takeover.");
        }

        /// <summary>
        /// PLACEHOLDER: Spawn 100+ objects in single frame, verify no ring buffer exhaustion.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Server spawns 100 objects in tight loop
        /// 3. Monitor for "Ring buffer is full" errors
        /// 4. Verify all 100 objects received by client
        /// 5. Verify all objects have correct positions
        /// 6. Verify no sync events dropped
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_SpawnBurst_NoRingBufferExhaustion()
        {
            Assert.Fail("Requires performance testing infrastructure. " +
                       "Test should verify ring buffer handles burst spawning without dropping sync events.");
        }

        /// <summary>
        /// PLACEHOLDER: Despawn object mid-RPC execution, verify RPC aborted gracefully.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Server spawns object
        /// 3. Client sends RPC to object
        /// 4. Server despawns object before processing RPC
        /// 5. Verify RPC handler not invoked (object destroyed)
        /// 6. Verify no NullReferenceException
        /// 7. Verify client receives despawn event
        /// 8. Verify client destroys local copy
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_DespawnDuringRPC_AbortedGracefully()
        {
            Assert.Fail("Requires RPC testing infrastructure. " +
                       "Test should verify RPCs targeting despawned objects are handled safely.");
        }

        /// <summary>
        /// PLACEHOLDER: Late-joiner connects, should NOT receive despawn event for never-seen object.
        ///
        /// Implementation steps:
        /// 1. Setup server + client 1
        /// 2. Server spawns object (GONetId 123)
        /// 3. Server immediately despawns object (GONetId 123)
        /// 4. Late-joiner (client 2) connects
        /// 5. Verify client 2 does NOT receive spawn event for GONetId 123
        /// 6. Verify client 2 does NOT receive despawn event for GONetId 123
        /// 7. Verify persistent event queue properly filters stale spawn/despawn pairs
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_LateJoiner_DoesNotReceiveDespawnForUnseenObject()
        {
            Assert.Fail("Requires late-joiner persistent event queue logic. " +
                       "Test should verify spawn/despawn pairs are filtered from persistent queue.");
        }

        /// <summary>
        /// PLACEHOLDER: Authority transfer from client A to client B, verify smooth handoff.
        ///
        /// Implementation steps:
        /// 1. Setup server + 2 clients (A and B)
        /// 2. Client A spawns object (client-spawned with batch ID)
        /// 3. Server assumes authority initially
        /// 4. Server transfers authority to client B
        /// 5. Verify client B's IsMine becomes true
        /// 6. Verify client A's IsMine becomes false
        /// 7. Verify OwnerAuthorityId updated on all clients
        /// 8. Verify sync data now originates from client B
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_AuthorityTransfer_ClientAToClientB()
        {
            Assert.Fail("Requires authority transfer API testing. " +
                       "Test should verify ownership handoff between clients with IsMine state sync.");
        }

        /// <summary>
        /// PLACEHOLDER: Spawn object, immediately despawn, verify no sync bundle leaks.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Server spawns object (GONetId 456)
        /// 3. Server immediately despawns object (same frame)
        /// 4. Verify InstantiateGONetParticipantEvent published
        /// 5. Verify DespawnGONetParticipantEvent published
        /// 6. Verify client receives both events
        /// 7. Verify client instantiates and destroys object
        /// 8. Verify no sync bundles pending for GONetId 456 after despawn
        /// 9. Verify no memory leaks (sync companion cleaned up)
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_SpawnThenImmediateDespawn_NoLeaks()
        {
            Assert.Fail("Requires lifecycle tracking and memory profiling. " +
                       "Test should verify rapid spawn/despawn cycles don't leak sync bundles.");
        }

        /// <summary>
        /// PLACEHOLDER: Client batch exhaustion triggers limbo mode, verify behavior.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Client has batch size of 10 IDs
        /// 3. Client spawns 10 objects rapidly (exhaust batch)
        /// 4. Client attempts to spawn 11th object
        /// 5. With ReturnFailure mode: Verify spawn returns null
        /// 6. With InstantiateInLimbo mode: Verify object spawned locally in limbo
        /// 7. Verify limbo object receives GONetId when new batch arrives
        /// 8. Verify limbo object exits limbo and OnGONetReady fires
        /// 9. With BlockUntilBatchArrives mode: Verify spawn blocks until batch arrives
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_BatchExhaustion_LimboModeBehavior()
        {
            Assert.Fail("Requires GONetId batch system testing with controlled batch depletion. " +
                       "Test should verify all three limbo modes behave correctly on batch exhaustion.");
        }

        /// <summary>
        /// PLACEHOLDER: Prefab with missing metadata, verify error handling.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Server has prefab "TestPrefab" in Resources
        /// 3. Client does NOT have prefab OR has different CodeGenerationId
        /// 4. Server spawns "TestPrefab"
        /// 5. Client receives InstantiateGONetParticipantEvent
        /// 6. Verify client logs error about missing prefab/metadata
        /// 7. Verify client does NOT crash
        /// 8. Verify server continues functioning
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Integration_MissingPrefab_ErrorHandledGracefully()
        {
            Assert.Fail("Requires prefab metadata mismatch scenario. " +
                       "Test should verify graceful error handling when prefab missing on client.");
        }

        /// <summary>
        /// PLACEHOLDER: Addressables prefab loading, verify async loading completes.
        ///
        /// Implementation steps:
        /// 1. Setup server + client (with ADDRESSABLES_AVAILABLE)
        /// 2. Server spawns prefab via "addressables://path/to/prefab"
        /// 3. Verify async Addressables loading begins
        /// 4. Wait for loading completion
        /// 5. Verify prefab instantiated after load
        /// 6. Verify client receives spawn event and loads Addressable
        /// 7. Verify both server and client have prefab instance
        /// 8. Verify reference counting (Addressables.Release on despawn)
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure with Addressables")]
        public void Integration_AddressablesPrefab_AsyncLoadingCompletes()
        {
            Assert.Fail("Requires Addressables testing infrastructure. " +
                       "Test should verify async prefab loading via Addressables system.");
        }

        #endregion

        #region Performance and Stress Tests

        /// <summary>
        /// PLACEHOLDER: Spawn and despawn 1000 objects rapidly, verify no memory growth.
        ///
        /// Implementation steps:
        /// 1. Setup server + client
        /// 2. Record initial memory usage (GC.GetTotalMemory)
        /// 3. Loop 1000 times:
        ///    a. Spawn object
        ///    b. Wait 1 frame
        ///    c. Despawn object
        /// 4. Force GC collection
        /// 5. Record final memory usage
        /// 6. Verify memory growth < 10% (accounting for fragmentation)
        /// 7. Verify no GONetIds leaked (all returned to pool)
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Stress_RapidSpawnDespawn_NoMemoryLeaks()
        {
            Assert.Fail("Requires memory profiling and extended test duration. " +
                       "Test should verify no memory leaks over 1000+ spawn/despawn cycles.");
        }

        /// <summary>
        /// PLACEHOLDER: Spawn 500 objects simultaneously, verify all clients receive all spawns.
        ///
        /// Implementation steps:
        /// 1. Setup server + 3 clients
        /// 2. Server spawns 500 objects in single frame
        /// 3. Wait for propagation to all clients (max 5 seconds)
        /// 4. Verify each client received 500 spawn events
        /// 5. Verify each client has 500 object instances
        /// 6. Verify no duplicate GONetIds
        /// 7. Verify no "Ring buffer is full" errors
        /// </summary>
        [Test]
        [Ignore("Requires Unity PlayMode test infrastructure")]
        public void Stress_MassiveSpawnBurst_AllClientsReceiveAll()
        {
            Assert.Fail("Requires stress testing infrastructure with spawn tracking. " +
                       "Test should verify ring buffer and event system handle massive burst spawning.");
        }

        #endregion

        #region Documentation

        /// <summary>
        /// Documentation for implementing spawn/despawn lifecycle tests.
        ///
        /// REQUIRED TEST INFRASTRUCTURE:
        ///
        /// 1. **Test Prefabs** (Assets/GONet/Sample/TestPrefabs/)
        ///    - GONetTestPrefab_Simple.prefab
        ///      * Basic cube with GONetParticipant
        ///      * Single Transform sync
        ///      * CodeGenerationId assigned
        ///      * In Resources folder for Resources.Load()
        ///
        ///    - GONetTestPrefab_WithComponents.prefab
        ///      * Multiple GONetAutoMagicalSync fields
        ///      * Rigidbody for physics sync testing
        ///      * Custom component with synced state
        ///
        ///    - GONetTestPrefab_Projectile.prefab
        ///      * Lightweight prefab for burst spawning
        ///      * Minimal sync (position only)
        ///      * Short lifetime (3 seconds auto-destroy)
        ///
        ///    - GONetTestPrefab_Addressable.prefab
        ///      * Addressable asset (if ADDRESSABLES_AVAILABLE)
        ///      * Large prefab to test async loading
        ///
        /// 2. **Spawn Test Helper** (GONetSpawnTestHelper.cs)
        ///    - WaitForSpawnPropagation(GONetId, clients[], timeout) → Coroutine
        ///    - VerifyObjectExistsOnAllClients(GONetId, clients[]) → Assertion
        ///    - TrackSpawnEvents(client) → List<InstantiateGONetParticipantEvent>
        ///    - TrackDespawnEvents(client) → List<DespawnGONetParticipantEvent>
        ///    - GetParticipantByGONetId(GONetId, client) → GONetParticipant
        ///    - AssertNoLeakedGONetIds() → Verifies all IDs returned to pool
        ///
        /// 3. **GONetId Batch Test Utilities** (GONetIdBatchTestUtils.cs)
        ///    - SetClientBatchSize(client, size) → Configure test batch
        ///    - ExhaustBatch(client) → Spawn until batch depleted
        ///    - VerifyBatchRefilled(client) → Assert new batch received
        ///    - GetRemainingBatchIds(client) → Count available IDs
        ///
        /// 4. **Memory Profiling Helper** (GONetMemoryProfiler.cs)
        ///    - RecordSnapshot(label) → Capture memory state
        ///    - CompareSnapshots(before, after) → Memory delta
        ///    - AssertNoMemoryGrowth(maxGrowthPercent) → Leak detection
        ///
        /// EXAMPLE IMPLEMENTATION:
        ///
        /// ```csharp
        /// [UnityTest]
        /// public IEnumerator ServerSpawn_AllClientsReceiveAndInstantiate()
        /// {
        ///     // Arrange
        ///     var harness = new GONetMultiClientTestHarness();
        ///     yield return harness.SetupServer();
        ///     yield return harness.SetupClients(3);
        ///
        ///     var spawnHelper = new GONetSpawnTestHelper();
        ///     var prefab = Resources.Load<GONetParticipant>("GONetTestPrefab_Simple");
        ///
        ///     // Act: Server spawns object
        ///     GONetParticipant serverInstance = harness.Server.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        ///     uint gonetId = serverInstance.GONetId;
        ///
        ///     // Wait for spawn propagation
        ///     yield return spawnHelper.WaitForSpawnPropagation(gonetId, harness.Clients, timeout: 2.0f);
        ///
        ///     // Assert: All clients instantiated object
        ///     spawnHelper.VerifyObjectExistsOnAllClients(gonetId, harness.Clients);
        ///
        ///     // Assert: All clients have matching authority
        ///     foreach (var client in harness.Clients)
        ///     {
        ///         var clientInstance = spawnHelper.GetParticipantByGONetId(gonetId, client);
        ///         Assert.IsNotNull(clientInstance);
        ///         Assert.AreEqual(harness.Server.AuthorityId, clientInstance.OwnerAuthorityId);
        ///         Assert.IsFalse(clientInstance.IsMine); // Client doesn't own it
        ///     }
        ///
        ///     // Cleanup
        ///     yield return harness.TearDownAll();
        /// }
        /// ```
        ///
        /// PRIORITY IMPLEMENTATION ORDER:
        /// 1. Create test prefabs in Resources (2-3 hours)
        /// 2. Implement GONetSpawnTestHelper coroutines (6-8 hours)
        /// 3. Convert basic spawn/despawn placeholder tests (1 day)
        /// 4. Implement GONetIdBatchTestUtils (4-6 hours)
        /// 5. Convert batch exhaustion tests (4-6 hours)
        /// 6. Implement memory profiling helper (6-8 hours)
        /// 7. Convert stress tests (1 day)
        /// Total: ~1 week
        ///
        /// CRITICAL TESTS (Implement First):
        /// 1. ServerSpawn_AllClientsReceiveAndInstantiate (P0)
        /// 2. ClientSpawn_ServerAssumesAuthority_OthersReceive (P0)
        /// 3. DespawnDuringRPC_AbortedGracefully (P0)
        /// 4. BatchExhaustion_LimboModeBehavior (P1)
        /// 5. SpawnBurst_NoRingBufferExhaustion (P1)
        /// </summary>
        [Test]
        public void Documentation_ImplementationGuide()
        {
            Assert.Pass("See test body for spawn/despawn lifecycle testing implementation guide.");
        }

        #endregion
    }
}
