using System;
using System.Collections.Concurrent;
using System.Threading;
using NUnit.Framework;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils;
using NetcodeIO.NET.Utils.IO;
using System.Net;

namespace GONet.Tests.Netcode_IO
{
    /// <summary>
    /// Base class for netcode.io integration tests that need actual client/server threads
    /// and network simulation. Based on GONet's TimeSyncTestBase pattern.
    /// </summary>
    public abstract class NetcodeIOTestBase
    {
        protected const ulong TEST_PROTOCOL_ID = 0x1122334455667788L;
        protected const int TEST_CONNECT_TOKEN_EXPIRY = 30;
        protected const int TEST_SERVER_PORT = 40000;

        protected static readonly byte[] PrivateKey = new byte[]
        {
            0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
            0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
            0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
            0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1,
        };

        protected CancellationTokenSource cts;
        protected Thread clientThread;
        protected Thread serverThread;
        protected BlockingCollection<Action> clientActions;
        protected BlockingCollection<Action> serverActions;
        internal NetworkSimulatorSocketManager socketMgr;
        protected double currentTime = 0.0;
        protected readonly object timeLock = new object();

        [SetUp]
        public virtual void BaseSetUp()
        {
            cts = new CancellationTokenSource();
            currentTime = 0.0;

            // Initialize blocking collections for thread-safe action queuing
            clientActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            serverActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());

            // Create network simulator
            socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            // Start client thread
            clientThread = new Thread(() =>
            {
                try
                {
                    UnityEngine.Debug.Log("Client thread initialized.");

                    foreach (var action in clientActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Client Thread Action Failed: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("Client thread canceled.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Client Thread Failed: {ex.Message}\n{ex.StackTrace}");
                }
            })
            {
                IsBackground = true,
                Name = "NetcodeIO-ClientThread"
            };
            clientThread.Start();

            // Start server thread
            serverThread = new Thread(() =>
            {
                try
                {
                    UnityEngine.Debug.Log("Server thread initialized.");

                    foreach (var action in serverActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Server Thread Action Failed: {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("Server thread canceled.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Server Thread Failed: {ex.Message}\n{ex.StackTrace}");
                }
            })
            {
                IsBackground = true,
                Name = "NetcodeIO-ServerThread"
            };
            serverThread.Start();

            // Wait for threads to initialize
            Thread.Sleep(100);
        }

        [TearDown]
        public virtual void BaseTearDown()
        {
            try
            {
                // Cancel threads
                cts?.Cancel();
                clientActions?.CompleteAdding();
                serverActions?.CompleteAdding();

                // Wait for threads to finish
                clientThread?.Join(2000);
                serverThread?.Join(2000);

                if (clientThread != null && clientThread.IsAlive)
                {
                    UnityEngine.Debug.LogWarning("Client thread did not terminate cleanly.");
                }
                if (serverThread != null && serverThread.IsAlive)
                {
                    UnityEngine.Debug.LogWarning("Server thread did not terminate cleanly.");
                }
            }
            finally
            {
                // Cleanup
                cts?.Dispose();
                clientActions?.Dispose();
                serverActions?.Dispose();
            }
        }

        /// <summary>
        /// Helper method to run an action on the client thread with proper error handling
        /// </summary>
        protected T RunOnClientThread<T>(Func<T> func, int timeoutMs = 5000)
        {
            return RunOnThread(func, clientActions, timeoutMs);
        }

        /// <summary>
        /// Helper method to run an action on the client thread
        /// </summary>
        protected void RunOnClientThread(Action action, int timeoutMs = 5000)
        {
            RunOnThread<object>(() => { action(); return null; }, clientActions, timeoutMs);
        }

        /// <summary>
        /// Helper method to run an action on the server thread with proper error handling
        /// </summary>
        protected T RunOnServerThread<T>(Func<T> func, int timeoutMs = 5000)
        {
            return RunOnThread(func, serverActions, timeoutMs);
        }

        /// <summary>
        /// Helper method to run an action on the server thread
        /// </summary>
        protected void RunOnServerThread(Action action, int timeoutMs = 5000)
        {
            RunOnThread<object>(() => { action(); return null; }, serverActions, timeoutMs);
        }

        /// <summary>
        /// Core helper method to run an action on a specific thread with proper error handling
        /// </summary>
        private T RunOnThread<T>(Func<T> func, BlockingCollection<Action> actions, int timeoutMs)
        {
            if (actions == null || actions.IsAddingCompleted)
                throw new InvalidOperationException("Action collection is not available");

            if (cts == null || cts.IsCancellationRequested)
                throw new OperationCanceledException("Test is being torn down");

            T result = default(T);
            Exception taskException = null;
            ManualResetEventSlim resetEvent = null;

            try
            {
                resetEvent = new ManualResetEventSlim(false);

                actions.Add(() =>
                {
                    try
                    {
                        result = func();
                    }
                    catch (Exception ex)
                    {
                        taskException = ex;
                    }
                    finally
                    {
                        try
                        {
                            resetEvent?.Set();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed, ignore
                        }
                    }
                });
            }
            catch (InvalidOperationException)
            {
                // Collection was completed, test is ending
                try { resetEvent?.Dispose(); } catch { }
                throw new OperationCanceledException("Test is being torn down");
            }

            try
            {
                if (resetEvent != null)
                {
                    bool signaled = false;
                    try
                    {
                        if (cts != null && !cts.IsCancellationRequested)
                        {
                            signaled = resetEvent.Wait(timeoutMs, cts.Token);
                        }
                        else
                        {
                            signaled = resetEvent.Wait(timeoutMs);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Test is being cancelled
                        throw;
                    }

                    if (!signaled)
                    {
                        throw new TimeoutException($"RunOnThread timed out after {timeoutMs}ms");
                    }
                }
            }
            finally
            {
                try { resetEvent?.Dispose(); } catch { }
            }

            if (taskException != null)
                throw taskException;

            return result;
        }

        /// <summary>
        /// Advance simulation time and update network simulator
        /// </summary>
        protected void AdvanceTime(double deltaTimeSeconds)
        {
            lock (timeLock)
            {
                currentTime += deltaTimeSeconds;
                socketMgr.Update(currentTime);
            }
        }

        /// <summary>
        /// Get current simulation time (thread-safe)
        /// </summary>
        protected double GetCurrentTime()
        {
            lock (timeLock)
            {
                return currentTime;
            }
        }

        /// <summary>
        /// Create a connect token for testing
        /// </summary>
        protected byte[] CreateConnectToken(IPEndPoint[] endpoints, ulong clientID, int timeoutSeconds = 5)
        {
            var factory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] userData = new byte[256];
            KeyUtils.GenerateKey(userData);

            return factory.GenerateConnectToken(
                endpoints,
                TEST_CONNECT_TOKEN_EXPIRY,
                timeoutSeconds,
                0,
                clientID,
                userData);
        }
    }
}
