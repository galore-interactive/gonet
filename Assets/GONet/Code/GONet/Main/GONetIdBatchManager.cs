using System;
using System.Collections.Generic;
using GONet.Utils;

namespace GONet
{
    /// <summary>
    /// Manages GONetId batch allocation for client-spawned, server-controlled objects.
    /// This system ensures that clients can predictively assign GONetIds that match
    /// what the server will assign, enabling seamless authority transfer.
    ///
    /// CRITICAL INVARIANTS:
    /// 1. Each batch is configurable sequential IDs (default 200, range 100-1000)
    /// 2. Batches are assigned to clients on connection
    /// 3. Clients consume batches sequentially (6000, 6001, 6002...)
    /// 4. Batches are exhausted and removed when all IDs used
    /// 5. New batches requested automatically when 50% IDs remain (threshold = batchSize / 2)
    /// 6. Scene changes reset client batch state completely
    ///
    /// BATCH SIZE CONFIGURATION:
    /// - Configured in GONet Project Settings
    /// - Range: 100-1000 IDs per batch
    /// - Default: 200 (good for typical games with projectiles)
    /// - Larger batches = fewer limbo occurrences but more ID space used
    /// - Smaller batches = more limbo occurrences but more efficient ID usage
    /// </summary>
    internal static class GONetIdBatchManager
    {
        // Batch size limits
        public const int MIN_BATCH_SIZE = 100;
        public const int MAX_BATCH_SIZE = 1000;
        public const int DEFAULT_BATCH_SIZE = 200;

        // Threshold is always 50% of batch size (hardcoded - user cannot configure)
        private const float BATCH_REQUEST_THRESHOLD_PERCENT = 0.5f;

        /// <summary>
        /// Represents a single batch of GONetIds allocated for client use.
        /// </summary>
        private class GONetIdBatch
        {
            public readonly uint BatchStart;
            public readonly uint BatchEnd; // Exclusive
            public readonly int BatchSize;
            public uint NextAvailableId;
            public int RemainingCount;

            public GONetIdBatch(uint batchStart, int batchSize)
            {
                BatchStart = batchStart;
                BatchSize = batchSize;
                BatchEnd = batchStart + (uint)batchSize;
                NextAvailableId = batchStart;
                RemainingCount = batchSize;
            }

            public bool TryAllocateNext(out uint gonetId)
            {
                if (RemainingCount > 0 && NextAvailableId < BatchEnd)
                {
                    gonetId = NextAvailableId++;
                    RemainingCount--;
                    return true;
                }
                gonetId = GONetParticipant.GONetIdRaw_Unset;
                return false;
            }

            public bool Contains(uint gonetIdRaw)
            {
                return gonetIdRaw >= BatchStart && gonetIdRaw < BatchEnd;
            }

            public bool IsExhausted => RemainingCount == 0;
        }

        // SERVER STATE
        private static readonly List<uint> server_allocatedBatchStarts = new List<uint>();

        // CLIENT STATE
        private static readonly List<GONetIdBatch> client_activeBatches = new List<GONetIdBatch>();
        private static uint client_totalIdsAllocated = 0;
        private static uint client_totalIdsUsed = 0;
        private static bool client_hasRequestedBatch = false; // Track if we've already requested a batch for current low state

        /// <summary>
        /// Gets the configured batch size from GONetGlobal (runtime settings).
        /// Falls back to DEFAULT_BATCH_SIZE if GONetGlobal unavailable.
        /// </summary>
        private static int GetBatchSize()
        {
            var gonetGlobal = GONetGlobal.Instance;
            if (gonetGlobal != null)
            {
                return gonetGlobal.client_GONetIdBatchSize;
            }

            return DEFAULT_BATCH_SIZE;
        }

        /// <summary>
        /// Gets the batch request threshold (50% of batch size).
        /// </summary>
        private static int GetBatchRequestThreshold()
        {
            int batchSize = GetBatchSize();
            return (int)(batchSize * BATCH_REQUEST_THRESHOLD_PERCENT);
        }

        /// <summary>
        /// CLIENT: Returns number of IDs remaining across all active batches.
        /// </summary>
        public static uint Client_GetRemainingIds()
        {
            return client_totalIdsAllocated - client_totalIdsUsed;
        }

        /// <summary>
        /// CLIENT: Returns true if at least one ID is available for allocation.
        /// </summary>
        public static bool Client_HasAvailableIds()
        {
            return Client_GetRemainingIds() > 0;
        }

        #region SERVER API

        /// <summary>
        /// SERVER: Allocates a new batch for a connecting client.
        /// Called during client connection handshake.
        /// </summary>
        public static uint Server_AllocateNewBatch(uint lastAssignedGONetIdRaw)
        {
            int batchSize = GetBatchSize();
            uint batchStart = lastAssignedGONetIdRaw + 1;

            // Ensure we never collide with existing batches
            while (server_allocatedBatchStarts.Contains(batchStart))
            {
                GONetLog.Warning($"[GONetIdBatch] Batch collision detected at {batchStart}, incrementing by batchSize");
                batchStart += (uint)batchSize;
            }

            server_allocatedBatchStarts.Add(batchStart);
            GONetLog.Info($"[GONetIdBatch] SERVER allocated batch [{batchStart} - {batchStart + batchSize - 1}] (size: {batchSize}) to client");

            return batchStart;
        }

        /// <summary>
        /// SERVER: Checks if a GONetId is within any allocated batch.
        /// Used to skip these IDs when assigning server-owned objects.
        /// </summary>
        public static bool Server_IsIdInAnyBatch(uint gonetIdRaw)
        {
            int batchSize = GetBatchSize();
            foreach (uint batchStart in server_allocatedBatchStarts)
            {
                if (gonetIdRaw >= batchStart && gonetIdRaw < batchStart + batchSize)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// SERVER: Removes a batch from tracking (e.g., client disconnected or batch exhausted).
        /// </summary>
        public static void Server_ReleaseBatch(uint batchStart)
        {
            if (server_allocatedBatchStarts.Remove(batchStart))
            {
                GONetLog.Info($"[GONetIdBatch] SERVER released batch starting at {batchStart}");
            }
        }

        /// <summary>
        /// SERVER: Clears all batch allocations (called on scene change or shutdown).
        /// </summary>
        public static void Server_ResetAllBatches()
        {
            int count = server_allocatedBatchStarts.Count;
            server_allocatedBatchStarts.Clear();
            GONetLog.Info($"[GONetIdBatch] SERVER reset - cleared {count} batch allocations");
        }

        #endregion

        #region CLIENT API

        /// <summary>
        /// CLIENT: Adds a new batch received from server.
        /// </summary>
        public static void Client_AddBatch(uint batchStart)
        {
            int batchSize = GetBatchSize();

            // Validate no duplicates
            foreach (var batch in client_activeBatches)
            {
                if (batch.BatchStart == batchStart)
                {
                    GONetLog.Warning($"[GONetIdBatch] CLIENT received duplicate batch {batchStart} - ignoring");
                    return;
                }
            }

            var newBatch = new GONetIdBatch(batchStart, batchSize);
            client_activeBatches.Add(newBatch);
            client_totalIdsAllocated += (uint)batchSize;
            client_hasRequestedBatch = false; // Reset flag - we can request again if we get low

            GONetLog.Info($"[GONetIdBatch] CLIENT received batch [{batchStart} - {batchStart + batchSize - 1}] (size: {batchSize}) | Total batches: {client_activeBatches.Count} | Remaining IDs: {client_totalIdsAllocated - client_totalIdsUsed}");
        }

        /// <summary>
        /// CLIENT: Attempts to allocate the next GONetId from available batches.
        /// Returns true if successful, false if no batches available.
        /// </summary>
        public static bool Client_TryAllocateNextId(out uint gonetIdRaw, out bool shouldRequestNewBatch)
        {
            shouldRequestNewBatch = false;

            // Remove exhausted batches
            for (int i = client_activeBatches.Count - 1; i >= 0; i--)
            {
                if (client_activeBatches[i].IsExhausted)
                {
                    uint exhaustedStart = client_activeBatches[i].BatchStart;
                    client_activeBatches.RemoveAt(i);
                    GONetLog.Info($"[GONetIdBatch] CLIENT removed exhausted batch starting at {exhaustedStart} | Remaining batches: {client_activeBatches.Count}");
                }
            }

            // Try to allocate from first available batch
            if (client_activeBatches.Count > 0)
            {
                if (client_activeBatches[0].TryAllocateNext(out gonetIdRaw))
                {
                    client_totalIdsUsed++;

                    uint remainingIds = client_totalIdsAllocated - client_totalIdsUsed;
                    int threshold = GetBatchRequestThreshold();
                    GONetLog.Info($"[GONetIdBatch] CLIENT allocated GONetId {gonetIdRaw} | Remaining in batch: {client_activeBatches[0].RemainingCount} | Total remaining: {remainingIds}");

                    // Check if we should request more batches (only once when dropping below threshold)
                    if (remainingIds < threshold && !client_hasRequestedBatch)
                    {
                        shouldRequestNewBatch = true;
                        client_hasRequestedBatch = true; // Mark that we've requested
                        GONetLog.Warning($"[GONetIdBatch] CLIENT low on IDs ({remainingIds} remaining, threshold: {threshold}) - should request new batch");
                    }

                    return true;
                }
            }

            // No batches available
            gonetIdRaw = GONetParticipant.GONetIdRaw_Unset;
            GONetLog.Error($"[GONetIdBatch] CLIENT has NO available batch IDs! Total batches: {client_activeBatches.Count}");
            return false;
        }

        /// <summary>
        /// CLIENT: Validates if a GONetId is within any active batch.
        /// Used for debugging and validation.
        /// </summary>
        public static bool Client_IsIdInActiveBatch(uint gonetIdRaw)
        {
            foreach (var batch in client_activeBatches)
            {
                if (batch.Contains(gonetIdRaw))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// CLIENT: Resets all batch state. Call on disconnect/reconnect scenarios.
        /// NOTE: Do NOT call on scene changes - batches persist across scenes!
        /// </summary>
        public static void Client_ResetAllBatches()
        {
            int batchCount = client_activeBatches.Count;
            uint remainingIds = client_totalIdsAllocated - client_totalIdsUsed;

            client_activeBatches.Clear();
            client_totalIdsAllocated = 0;
            client_totalIdsUsed = 0;
            client_hasRequestedBatch = false; // Reset request flag

            GONetLog.Info($"[GONetIdBatch] CLIENT reset - cleared {batchCount} batches ({remainingIds} unused IDs)");
        }

        /// <summary>
        /// CLIENT: Gets diagnostic information about current batch state.
        /// </summary>
        public static string Client_GetDiagnostics()
        {
            uint remainingIds = client_totalIdsAllocated - client_totalIdsUsed;
            return $"Batches: {client_activeBatches.Count} | Allocated: {client_totalIdsAllocated} | Used: {client_totalIdsUsed} | Remaining: {remainingIds}";
        }

        #endregion

        #region VALIDATION

        /// <summary>
        /// Validates batch integrity (for unit testing and debugging).
        /// </summary>
        public static bool ValidateBatchIntegrity(out string errorMessage)
        {
            // Check for overlapping batches
            for (int i = 0; i < client_activeBatches.Count; i++)
            {
                for (int j = i + 1; j < client_activeBatches.Count; j++)
                {
                    var batch1 = client_activeBatches[i];
                    var batch2 = client_activeBatches[j];

                    if (!(batch1.BatchEnd <= batch2.BatchStart || batch2.BatchEnd <= batch1.BatchStart))
                    {
                        errorMessage = $"Overlapping batches detected: [{batch1.BatchStart}-{batch1.BatchEnd}) and [{batch2.BatchStart}-{batch2.BatchEnd})";
                        return false;
                    }
                }
            }

            // Check for invalid batch state
            foreach (var batch in client_activeBatches)
            {
                if (batch.NextAvailableId > batch.BatchEnd)
                {
                    errorMessage = $"Batch {batch.BatchStart} has invalid NextAvailableId: {batch.NextAvailableId} (max: {batch.BatchEnd})";
                    return false;
                }

                uint expectedRemaining = batch.BatchEnd - batch.NextAvailableId;
                if (batch.RemainingCount != expectedRemaining)
                {
                    errorMessage = $"Batch {batch.BatchStart} has incorrect RemainingCount: {batch.RemainingCount} (expected: {expectedRemaining})";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
