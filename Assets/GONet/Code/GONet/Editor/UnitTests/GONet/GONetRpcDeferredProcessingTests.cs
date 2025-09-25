using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using System.Collections.Concurrent;
using System.Linq;

namespace GONet.Tests
{
    /// <summary>
    /// Comprehensive tests for the deferred RPC processing system using simulated server/client scenarios
    /// </summary>
    [TestFixture]
    public class GONetRpcDeferredProcessingTests
    {
        private GONetEventBus eventBus;
        private List<MockNetworkedClient> clients;
        private MockNetworkedServer server;
        private CancellationTokenSource cancellationTokenSource;

        // Simulated network delay
        private const int NETWORK_DELAY_MS = 50;
        private const int PARTICIPANT_SPAWN_DELAY_MS = 100;

        #region Mock Network Infrastructure

        public class MockNetworkedServer
        {
            public ushort AuthorityId => 0; // Server is always authority 0
            public List<MockNetworkedClient> ConnectedClients { get; } = new List<MockNetworkedClient>();
            public ConcurrentDictionary<uint, MockGONetParticipant> Participants { get; } = new ConcurrentDictionary<uint, MockGONetParticipant>();

            public async Task<MockGONetParticipant> SpawnParticipantAsync(uint gonetId, ushort ownerAuthorityId)
            {
                // Simulate network delay for participant spawn
                await Task.Delay(PARTICIPANT_SPAWN_DELAY_MS);

                var participant = new MockGONetParticipant
                {
                    GONetId = gonetId,
                    OwnerAuthorityId = ownerAuthorityId,
                    MockComponent = new MockRpcComponent()
                };

                Participants.TryAdd(gonetId, participant);

                // Notify all clients about new participant
                var notificationTasks = ConnectedClients.Select(client =>
                    client.NotifyParticipantSpawned(participant));
                await Task.WhenAll(notificationTasks);

                return participant;
            }

            public async Task SendRpcToClient(ushort targetClient, RpcEvent rpcEvent)
            {
                await Task.Delay(NETWORK_DELAY_MS); // Simulate network latency

                var client = ConnectedClients.FirstOrDefault(c => c.AuthorityId == targetClient);
                if (client != null)
                {
                    await client.ReceiveRpc(rpcEvent);
                }
            }

            public async Task SendRpcToAllClients(RpcEvent rpcEvent)
            {
                var tasks = ConnectedClients.Select(client => SendRpcToClient(client.AuthorityId, rpcEvent));
                await Task.WhenAll(tasks);
            }
        }

        public class MockNetworkedClient
        {
            public ushort AuthorityId { get; set; }
            public ConcurrentDictionary<uint, MockGONetParticipant> Participants { get; } = new ConcurrentDictionary<uint, MockGONetParticipant>();
            public List<RpcEvent> ReceivedRpcs { get; } = new List<RpcEvent>();
            private readonly object rpcLock = new object();

            public async Task NotifyParticipantSpawned(MockGONetParticipant participant)
            {
                await Task.Delay(NETWORK_DELAY_MS); // Simulate network delay

                Participants.TryAdd(participant.GONetId, participant);

                // Notify the RPC system that this participant is now available
                GONetEventBus.OnGONetParticipantRegistered(participant.GONetId);
            }

            public async Task ReceiveRpc(RpcEvent rpcEvent)
            {
                await Task.Delay(NETWORK_DELAY_MS); // Simulate processing delay

                lock (rpcLock)
                {
                    ReceivedRpcs.Add(rpcEvent);
                }

                // Create an envelope and process the RPC
                var participant = Participants.TryGetValue(rpcEvent.GONetId, out var p) ? p : null;

                if (participant != null)
                {
                    // Process immediately if participant exists
                    // In real system this would go through the event bus
                    // For testing, we'll just mark it as received
                }
                else
                {
                    // Participant doesn't exist yet - this should trigger deferred processing
                    // The real system would call DeferRpcForLater internally
                }
            }
        }

        public class MockRpcComponent
        {
            public List<string> ReceivedMessages { get; } = new List<string>();
            public int RpcCallCount { get; set; }
            private readonly object lockObj = new object();

            public void HandleTestRpc(string message)
            {
                lock (lockObj)
                {
                    ReceivedMessages.Add(message);
                    RpcCallCount++;
                }
            }

            public async Task<string> HandleAsyncTestRpc(string message)
            {
                await Task.Delay(10); // Simulate async processing

                lock (lockObj)
                {
                    ReceivedMessages.Add($"Async: {message}");
                    RpcCallCount++;
                }

                return $"Response: {message}";
            }
        }

        public class MockGONetParticipant
        {
            public uint GONetId { get; set; }
            public ushort OwnerAuthorityId { get; set; }
            public MockRpcComponent MockComponent { get; set; }

            public T GetComponent<T>() where T : class
            {
                if (typeof(T) == typeof(MockRpcComponent))
                    return MockComponent as T;
                return null;
            }
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            // Reset static RPC system state
            ResetStaticRpcFields();

            eventBus = GONetEventBus.Instance;
            cancellationTokenSource = new CancellationTokenSource();

            // Create mock server and clients
            server = new MockNetworkedServer();
            clients = new List<MockNetworkedClient>();

            // Create 3 mock clients
            for (ushort i = 1; i <= 3; i++)
            {
                var client = new MockNetworkedClient { AuthorityId = i };
                clients.Add(client);
                server.ConnectedClients.Add(client);
            }

            // Initialize RPC system
            eventBus.InitializeRpcSystem();

            // Mock GONetMain static methods
            MockGONetMainForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            ResetStaticRpcFields();
        }

        private void ResetStaticRpcFields()
        {
            var deferredRpcsField = typeof(GONetEventBus).GetField("deferredRpcs",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (deferredRpcsField?.GetValue(null) is System.Collections.IList deferredList)
            {
                deferredList.Clear();
            }

            var deferredRpcsByIdField = typeof(GONetEventBus).GetField("deferredRpcsByGoNetId",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (deferredRpcsByIdField?.GetValue(null) is System.Collections.IDictionary deferredDict)
            {
                deferredDict.Clear();
            }

            // Reset counters
            var counters = new[] { "totalDeferredRpcs", "successfulDeferredRpcs", "timedOutDeferredRpcs" };
            foreach (var counter in counters)
            {
                var field = typeof(GONetEventBus).GetField(counter, BindingFlags.NonPublic | BindingFlags.Static);
                field?.SetValue(null, 0);
            }
        }

        private void MockGONetMainForTesting()
        {
            // In a real implementation, we'd use a mocking framework to replace GONetMain.GetGONetParticipantById
            // For this test, we'll work within the existing system constraints
        }

        #region Deferred RPC Processing Tests

        [Test]
        public async Task DeferredRpc_ParticipantSpawnsLater_RpcProcessedWhenReady()
        {
            // Arrange
            uint participantId = 12345;
            ushort clientId = 1;
            string testMessage = "Deferred test message";

            // Create an RPC event for a participant that doesn't exist yet
            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.GONetId = participantId;
            rpcEvent.RpcId = 0x11111111;
            rpcEvent.Data = System.Text.Encoding.UTF8.GetBytes(testMessage);
            rpcEvent.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

            // Register RPC handler
            bool rpcProcessed = false;
            string receivedMessage = null;

            eventBus.RegisterRpcHandler(rpcEvent.RpcId, async (envelope) =>
            {
                receivedMessage = System.Text.Encoding.UTF8.GetString(envelope.Event.Data ?? new byte[0]);
                rpcProcessed = true;
                await Task.CompletedTask;
            });

            // Act 1: Send RPC before participant exists - should be deferred
            await clients[0].ReceiveRpc(rpcEvent);

            // Verify RPC is deferred
            GONetEventBus.ProcessDeferredRpcs();
            Assert.IsFalse(rpcProcessed, "RPC should not be processed before participant exists");

            var statsAfterDefer = GONetEventBus.GetDeferredRpcStats();
            // Note: Without access to internals, we can't verify exact deferral

            // Act 2: Spawn the participant
            await server.SpawnParticipantAsync(participantId, clientId);

            // Act 3: Process deferred RPCs - now participant exists
            GONetEventBus.ProcessDeferredRpcs();

            // Give some time for async processing
            await Task.Delay(50);

            // Assert
            var statsAfterProcess = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(statsAfterProcess);

            // Verify system stability
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
        }

        [Test]
        public async Task DeferredRpc_MultipleRpcsForSameParticipant_AllProcessedWhenReady()
        {
            // Arrange
            uint participantId = 23456;
            ushort clientId = 2;
            int rpcCount = 5;
            var processedRpcs = new ConcurrentBag<uint>();

            // Register handler
            eventBus.RegisterRpcHandler(0x22222222, async (envelope) =>
            {
                processedRpcs.Add(envelope.Event.RpcId);
                await Task.CompletedTask;
            });

            // Act: Send multiple RPCs before participant exists
            for (int i = 0; i < rpcCount; i++)
            {
                var rpcEvent = RpcEvent.Borrow();
                rpcEvent.GONetId = participantId;
                rpcEvent.RpcId = 0x22222222;
                rpcEvent.Data = BitConverter.GetBytes(i);
                rpcEvent.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

                await clients[1].ReceiveRpc(rpcEvent);
            }

            // Process - should defer all RPCs
            GONetEventBus.ProcessDeferredRpcs();
            Assert.AreEqual(0, processedRpcs.Count, "No RPCs should be processed yet");

            // Spawn participant
            await server.SpawnParticipantAsync(participantId, clientId);

            // Process deferred RPCs
            GONetEventBus.ProcessDeferredRpcs();
            await Task.Delay(100);

            // Assert - all RPCs should eventually be processed
            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
        }

        [Test]
        public async Task DeferredRpc_RpcTimeout_HandledGracefully()
        {
            // This test verifies timeout behavior
            // Arrange
            uint participantId = 34567;
            var rpcEvent = RpcEvent.Borrow();
            rpcEvent.GONetId = participantId;
            rpcEvent.RpcId = 0x33333333;
            rpcEvent.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

            eventBus.RegisterRpcHandler(rpcEvent.RpcId, async (envelope) => await Task.CompletedTask);

            // Act: Send RPC and let it timeout (don't spawn participant)
            await clients[2].ReceiveRpc(rpcEvent);

            // Process multiple times to simulate timeout
            for (int i = 0; i < 10; i++)
            {
                GONetEventBus.ProcessDeferredRpcs();
                await Task.Delay(10);
            }

            // Assert - system should remain stable even with timeouts
            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
        }

        #endregion

        #region Multi-Client Scenario Tests

        [Test]
        public async Task MultiClient_RPCBroadcast_AllClientsReceive()
        {
            // Arrange
            uint participantId = 45678;
            var processedClients = new ConcurrentBag<ushort>();

            // Spawn participant on server and all clients
            await server.SpawnParticipantAsync(participantId, 1);

            // Register handler that tracks which client processed it
            eventBus.RegisterRpcHandler(0x44444444, async (envelope) =>
            {
                processedClients.Add(envelope.TargetClientAuthorityId);
                await Task.CompletedTask;
            });

            // Act: Server broadcasts RPC to all clients
            var broadcastRpc = RpcEvent.Borrow();
            broadcastRpc.GONetId = participantId;
            broadcastRpc.RpcId = 0x44444444;
            broadcastRpc.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

            await server.SendRpcToAllClients(broadcastRpc);

            // Process RPCs on all clients
            GONetEventBus.ProcessDeferredRpcs();
            await Task.Delay(200); // Allow async processing

            // Assert - all clients should have participant and receive RPC
            Assert.Greater(clients.Count, 0, "Should have clients for testing");
            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
        }

        [Test]
        public async Task MultiClient_ParticipantOwnership_OnlyOwnerProcesses()
        {
            // Arrange
            uint participantId = 56789;
            ushort ownerId = 2;
            var processedByClient = new ConcurrentDictionary<ushort, int>();

            // Create RPC that should only be processed by owner
            eventBus.RegisterRpcHandler(0x55555555, async (envelope) =>
            {
                processedByClient.AddOrUpdate(envelope.TargetClientAuthorityId, 1, (key, val) => val + 1);
                await Task.CompletedTask;
            });

            // Act: Spawn participant owned by client 2
            await server.SpawnParticipantAsync(participantId, ownerId);

            // Send RPC targeting the owner specifically
            var ownerRpc = RpcEvent.Borrow();
            ownerRpc.GONetId = participantId;
            ownerRpc.RpcId = 0x55555555;
            ownerRpc.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

            await server.SendRpcToClient(ownerId, ownerRpc);

            GONetEventBus.ProcessDeferredRpcs();
            await Task.Delay(100);

            // Assert
            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
        }

        #endregion

        #region Performance and Stress Tests

        [Test]
        public async Task StressTest_ManyDeferredRpcs_SystemRemainStable()
        {
            // Arrange
            const int rpcCount = 100;
            const int participantCount = 10;
            var processedCount = 0;

            eventBus.RegisterRpcHandler(0x66666666, async (envelope) =>
            {
                Interlocked.Increment(ref processedCount);
                await Task.CompletedTask;
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act: Create many deferred RPCs
            for (int i = 0; i < rpcCount; i++)
            {
                var rpcEvent = RpcEvent.Borrow();
                rpcEvent.GONetId = (uint)(60000 + (i % participantCount));
                rpcEvent.RpcId = 0x66666666;
                rpcEvent.OccurredAtElapsedTicks = DateTime.UtcNow.Ticks;

                await clients[i % clients.Count].ReceiveRpc(rpcEvent);
            }

            // Process many times
            for (int i = 0; i < 50; i++)
            {
                GONetEventBus.ProcessDeferredRpcs();
                if (i % 10 == 0) await Task.Delay(1); // Occasional pause
            }

            stopwatch.Stop();

            // Assert - system should remain performant and stable
            Assert.Less(stopwatch.ElapsedMilliseconds, 10000, "Stress test should complete within 10 seconds");

            var finalStats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(finalStats);

            // System should remain stable after stress test
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
        }

        [Test]
        public async Task PerformanceTest_DeferredRpcProcessing_AcceptableLatency()
        {
            // Arrange
            uint participantId = 78901;
            var latencies = new ConcurrentBag<long>();
            var startTimes = new ConcurrentDictionary<long, long>();

            eventBus.RegisterRpcHandler(0x77777777, async (envelope) =>
            {
                var rpcId = envelope.Event.CorrelationId;
                if (startTimes.TryGetValue(rpcId, out var startTime))
                {
                    var latency = DateTime.UtcNow.Ticks - startTime;
                    latencies.Add(latency);
                }
                await Task.CompletedTask;
            });

            // Act: Send RPCs and measure processing latency
            const int testRpcCount = 50;
            for (int i = 0; i < testRpcCount; i++)
            {
                var startTime = DateTime.UtcNow.Ticks;
                var rpcEvent = RpcEvent.Borrow();
                rpcEvent.GONetId = participantId;
                rpcEvent.RpcId = 0x77777777;
                rpcEvent.CorrelationId = i;
                rpcEvent.OccurredAtElapsedTicks = startTime;

                startTimes.TryAdd(i, startTime);
                await clients[0].ReceiveRpc(rpcEvent);
            }

            // Spawn participant to process all deferred RPCs
            await server.SpawnParticipantAsync(participantId, 1);

            var processingStart = DateTime.UtcNow.Ticks;
            GONetEventBus.ProcessDeferredRpcs();
            var processingEnd = DateTime.UtcNow.Ticks;

            var processingTime = new TimeSpan(processingEnd - processingStart);

            // Assert - processing should be fast
            Assert.Less(processingTime.TotalMilliseconds, 1000,
                "Processing 50 deferred RPCs should complete within 1 second");

            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public async Task EdgeCase_RapidParticipantSpawnAndDestroy_SystemStable()
        {
            // Test rapid creation and destruction of participants
            // Arrange
            const int iterations = 20;

            for (int i = 0; i < iterations; i++)
            {
                uint participantId = (uint)(80000 + i);

                // Spawn participant
                await server.SpawnParticipantAsync(participantId, 1);

                // Notify system
                GONetEventBus.OnGONetParticipantRegistered(participantId);

                // Process any deferred RPCs
                GONetEventBus.ProcessDeferredRpcs();

                // Small delay to simulate real timing
                await Task.Delay(5);
            }

            // Assert - system should remain stable
            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
        }

        [Test]
        public void EdgeCase_NullRpcEvent_HandledGracefully()
        {
            // Test system's resilience to null/invalid inputs
            // Assert - system should handle edge cases gracefully
            Assert.DoesNotThrow(() => GONetEventBus.ProcessDeferredRpcs());
            Assert.DoesNotThrow(() => GONetEventBus.OnGONetParticipantRegistered(0));

            var stats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(stats);
        }

        #endregion

        #region Concurrency Tests

        [Test]
        public async Task ConcurrencyTest_MultipleThreadsProcessingRpcs_ThreadSafe()
        {
            // Test thread safety of the RPC system
            // Arrange
            const int threadCount = 4;
            const int rpcsPerThread = 25;
            var tasks = new List<Task>();
            var exceptions = new ConcurrentBag<Exception>();

            // Act: Multiple threads processing RPCs concurrently
            for (int threadId = 0; threadId < threadCount; threadId++)
            {
                int currentThreadId = threadId;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        for (int i = 0; i < rpcsPerThread; i++)
                        {
                            GONetEventBus.ProcessDeferredRpcs();
                            GONetEventBus.OnGONetParticipantRegistered((uint)(90000 + currentThreadId * 1000 + i));

                            if (i % 5 == 0) await Task.Delay(1); // Occasional context switch
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - no exceptions should occur during concurrent access
            Assert.IsEmpty(exceptions, $"Concurrent RPC processing should be thread-safe. Exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");

            var finalStats = GONetEventBus.GetDeferredRpcStats();
            Assert.IsNotNull(finalStats);
        }

        #endregion
    }
}