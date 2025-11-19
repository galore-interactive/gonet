using GONet;
using GONet.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NetcodeIO.NET.Utils.IO
{
    public interface ISocketContext : IDisposable
    {
        int BoundPort { get; }
        int AvailableToReadCount { get; }
        void Bind(EndPoint endpoint);
        void SendTo(byte[] data, EndPoint remoteEP);
        void SendTo(byte[] data, int length, EndPoint remoteEP);
        void SendToAsync(byte[] data, int length, EndPoint remoteEP);
        bool Read(out Datagram packet);
        void Pump(); // Only used by simulator
        void Close();
    }

    internal sealed class UDPSocketContext : ISocketContext
    {
        public int BoundPort => _boundPort;
        public int AvailableToReadCount => _datagramQueue.Count;

        private Socket _socket;
        private int _boundPort = -1;
        private volatile bool _isRunning;
        private readonly AddressFamily _addressFamily;

        private readonly DatagramQueue _datagramQueue;
        private readonly ArrayPool<byte> _sendPayloadPool;
        private readonly ArrayPool<byte> _receiveBufferPool;
        private readonly ObjectPool<SendAsyncPackaging> _sendPackagingPool;
        private readonly ReceiveAsyncPackagingPool _receivePackagingPool;

        // Concurrent receives: Higher = better throughput, more memory
        // 8-16 is optimal for game servers (balances latency vs memory)
        private const int ConcurrentReceives = 12;

        // Custom pool for ReceiveAsyncPackaging (requires factory pattern)
        private sealed class ReceiveAsyncPackagingPool : ObjectPoolBase<ReceiveAsyncPackaging>
        {
            private readonly UDPSocketContext _context;

            public ReceiveAsyncPackagingPool(int initialSize, int growByCount, UDPSocketContext context)
                : base(initialSize, growByCount, null)
            {
                _context = context;
            }

            protected override ReceiveAsyncPackaging CreateSingleInstance()
            {
                return new ReceiveAsyncPackaging { Context = _context };
            }
        }

        // Global staging queue for async send completions (IOCP threads don't match caller threads)
        private readonly ConcurrentQueue<byte[]> _sendPayloadReturns = new();
        private readonly ConcurrentQueue<SendAsyncPackaging> _sendPackagingReturns = new();

        private sealed class SendAsyncPackaging : SocketAsyncEventArgs
        {
            public byte[] payload;
            public int payloadSize;
            public UDPSocketContext Context;

            public void Init(EndPoint remoteEP, byte[] payload, int size, UDPSocketContext context)
            {
                RemoteEndPoint = remoteEP;
                SetBuffer(payload, 0, size);
                this.payload = payload;
                this.payloadSize = size;
                Context = context;
            }

            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                var packaging = (SendAsyncPackaging)e;
                if (packaging.BytesTransferred != packaging.payloadSize && packaging.SocketError == SocketError.Success)
                {
                    GONetLog.Warning($"Partial send: {packaging.BytesTransferred}/{packaging.payloadSize} bytes");
                }

                // Queue for return (will be drained by SendToAsync on main thread)
                var ctx = packaging.Context;
                if (ctx != null)
                {
                    ctx._sendPayloadReturns.Enqueue(packaging.payload);
                    ctx._sendPackagingReturns.Enqueue(packaging);
                }
            }
        }

        private sealed class ReceiveAsyncPackaging : SocketAsyncEventArgs
        {
            public byte[] RentedBuffer;
            public ArrayPool<byte> BufferPool;
            public UDPSocketContext Context;

            // Parameterless constructor required by ObjectPool
            public ReceiveAsyncPackaging()
            {
            }

            public void PrepareForReceive(AddressFamily family, ArrayPool<byte> bufferPool)
            {
                // Rent buffer from pool (MUST return after use!)
                if (RentedBuffer == null)
                {
                    RentedBuffer = bufferPool.Borrow(65536);
                    BufferPool = bufferPool;
                    SetBuffer(RentedBuffer, 0, RentedBuffer.Length);
                }

                // Create NEW endpoint each time (thread-safe, no mutation)
                RemoteEndPoint = new IPEndPoint(
                    family == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any,
                    0);
            }

            protected override void OnCompleted(SocketAsyncEventArgs e)
            {
                // Null check AND running check before processing
                var ctx = Context;
                if (ctx != null && ctx._isRunning)
                {
                    ctx.ProcessReceive((ReceiveAsyncPackaging)e);
                }
            }

            public void ReturnBuffer()
            {
                if (RentedBuffer != null && BufferPool != null)
                {
                    BufferPool.Return(RentedBuffer);
                    RentedBuffer = null;
                    BufferPool = null;
                }
            }
        }

        public UDPSocketContext(AddressFamily addressFamily = AddressFamily.InterNetworkV6)
        {
            _addressFamily = addressFamily;
            _datagramQueue = new DatagramQueue();

            _socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            // Optimized buffer sizes (tuned for 100-client server @ 20Hz)
            _socket.ReceiveBufferSize = 8400 * 4;   // ~64KB client incoming
            _socket.SendBufferSize = 8400 * 200;    // ~1.6MB server outgoing

            // Pooling (GONet style)
            _sendPayloadPool = new ArrayPool<byte>(50, 25, 1024, 64 * 1024);
            _receiveBufferPool = new ArrayPool<byte>(ConcurrentReceives * 2, ConcurrentReceives, 65536, 65536);
            _sendPackagingPool = new ObjectPool<SendAsyncPackaging>(100, 50);
            _receivePackagingPool = new ReceiveAsyncPackagingPool(
                ConcurrentReceives * 2,  // Max capacity: 24
                ConcurrentReceives,      // Min capacity: 12
                this);
        }

        public void Bind(EndPoint endpoint)
        {
            _socket.Bind(endpoint);
            _boundPort = ((IPEndPoint)_socket.LocalEndPoint).Port;
            _isRunning = true;

            // Post multiple overlapping async receives (NO THREAD NEEDED!)
            for (int i = 0; i < ConcurrentReceives; i++)
            {
                var packaging = _receivePackagingPool.Borrow();

                // CRITICAL: Re-initialize Context on every borrow (pool reuse pattern)
                packaging.Context = this;
                packaging.PrepareForReceive(_addressFamily, _receiveBufferPool);

                if (!PostReceive(packaging))
                {
                    // Completed synchronously (rare but possible)
                    ProcessReceive(packaging);
                }
            }
        }

        private bool PostReceive(ReceiveAsyncPackaging packaging)
        {
            var socket = _socket; // Snapshot to avoid race
            if (!_isRunning || socket == null)
            {
                _receivePackagingPool.Return(packaging);
                return false;
            }

            try
            {
                return socket.ReceiveFromAsync(packaging);
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed during call - graceful shutdown
                _receivePackagingPool.Return(packaging);
                return false;
            }
            catch (SocketException)
            {
                // Socket error (e.g., closed) - graceful shutdown
                _receivePackagingPool.Return(packaging);
                return false;
            }
            catch (Exception ex)
            {
                GONetLog.Error($"Unexpected PostReceive error: {ex}");
                _receivePackagingPool.Return(packaging);
                return false;
            }
        }

        private void ProcessReceive(ReceiveAsyncPackaging packaging)
        {
            // Check for successful receive
            if (packaging.SocketError == SocketError.Success && packaging.BytesTransferred > 0)
            {
                // Copy from rented buffer to GONet's BufferPool (MUST copy before reposting!)
                var payload = BufferPool.GetBuffer(packaging.BytesTransferred);
                Buffer.BlockCopy(packaging.RentedBuffer, packaging.Offset, payload, 0, packaging.BytesTransferred);

                _datagramQueue.Enqueue(new Datagram
                {
                    payload = payload,
                    payloadSize = packaging.BytesTransferred,
                    sender = packaging.RemoteEndPoint
                });
            }
            else if (packaging.SocketError != SocketError.OperationAborted && packaging.SocketError != SocketError.Success)
            {
                // Log non-fatal errors (except normal shutdown)
                GONetLog.Warning($"UDP receive error: {packaging.SocketError}");
            }

            // Repost receive if socket still alive
            if (_isRunning && _socket != null)
            {
                packaging.PrepareForReceive(_addressFamily, _receiveBufferPool);

                if (!PostReceive(packaging))
                {
                    // Rare: synchronous completion on repost (tail recursion)
                    ProcessReceive(packaging);
                }
            }
            else
            {
                // Shutdown: return buffer to pool, then return packaging
                packaging.ReturnBuffer();
                _receivePackagingPool.Return(packaging);
            }
        }

        public void SendTo(byte[] data, EndPoint remoteEP) => _socket.SendTo(data, remoteEP);
        public void SendTo(byte[] data, int length, EndPoint remoteEP) => _socket.SendTo(data, length, SocketFlags.None, remoteEP);

        public void SendToAsync(byte[] data, int length, EndPoint remoteEP)
        {
            // Drain completed send returns FIRST (thread-safe drain from IOCP completions)
            DrainSendReturns();

            var payload = _sendPayloadPool.Borrow(length);
            Buffer.BlockCopy(data, 0, payload, 0, length);

            var packaging = _sendPackagingPool.Borrow();
            packaging.Init(remoteEP, payload, length, this);

            try
            {
                if (!_socket.SendToAsync(packaging))
                {
                    // Completed synchronously (rare)
                    _sendPayloadReturns.Enqueue(packaging.payload);
                    _sendPackagingReturns.Enqueue(packaging);
                }
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                // Error: return to pools immediately
                _sendPayloadPool.Return(payload);
                _sendPackagingPool.Return(packaging);
                throw;
            }

            // Drain again (may have completed during SendToAsync call)
            DrainSendReturns();
        }

        private void DrainSendReturns()
        {
            // Return completed payloads to pool
            while (_sendPayloadReturns.TryDequeue(out var payload))
                _sendPayloadPool.Return(payload);

            // Return completed packagings to pool
            while (_sendPackagingReturns.TryDequeue(out var packaging))
                _sendPackagingPool.Return(packaging);
        }

        public bool Read(out Datagram packet) => _datagramQueue.TryDequeue(out packet);

        public void Pump() { /* Real socket doesn't need pump */ }

        public void Close()
        {
            _isRunning = false;
            _socket?.Close();
        }

        public void Dispose()
        {
            Close();
            _socket?.Dispose();
            _socket = null;
        }
    }

    // =============================================================================
    // Network Simulator – Now Efficient, Deterministic, and Fully Compatible
    // =============================================================================

    internal sealed class NetworkSimulatorSocketManager
    {
        public int LatencyMS { get; set; } = 0;
        public int JitterMS { get; set; } = 0;
        public int PacketLossChance { get; set; } = 0;      // 0-99
        public int DuplicatePacketChance { get; set; } = 0;  // 0-99
        public bool AutoTime { get; set; } = true;

        public double Time => AutoTime ? Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency : _manualTime;
        private double _manualTime;

        private readonly Dictionary<EndPoint, NetworkSimulatorSocketContext> _sockets = new();
        private readonly Random _rnd = new();

        public void Update(double time) => _manualTime = time;

        public NetworkSimulatorSocketContext CreateContext(EndPoint endpoint)
        {
            var ctx = new NetworkSimulatorSocketContext(this);
            lock (_sockets) _sockets[endpoint] = ctx;
            ctx._localEndpoint = endpoint;
            return ctx;
        }

        internal NetworkSimulatorSocketContext FindContext(EndPoint ep)
        {
            NetworkSimulatorSocketContext ctx;
            lock (_sockets) _sockets.TryGetValue(ep, out ctx);
            return ctx;
        }

        internal void Remove(EndPoint ep)
        {
            lock (_sockets) _sockets.Remove(ep);
        }

        internal void Deliver(byte[] data, EndPoint from, EndPoint to)
        {
            if (_rnd.Next(100) < PacketLossChance) return;

            double delay = LatencyMS / 1000.0;
            if (JitterMS > 0)
                delay += (_rnd.NextDouble() * 2 - 1) * (JitterMS / 1000.0);

            var target = FindContext(to);
            if (target == null) return;

            target.Enqueue(data, from, Time + delay);

            if (_rnd.Next(100) < DuplicatePacketChance)
                target.Enqueue(data, from, Time + delay + _rnd.NextDouble() * 0.05);
        }
    }

    internal sealed class NetworkSimulatorSocketContext : ISocketContext
    {
        public int BoundPort => ((IPEndPoint)_localEndpoint).Port;
        public int AvailableToReadCount => _readyQueue.Count;

        internal EndPoint _localEndpoint;
        private readonly NetworkSimulatorSocketManager _manager;
        private readonly DatagramQueue _readyQueue = new();
        private readonly List<(double arrival, byte[] data, EndPoint sender)> _pending = new();
        private readonly object _lock = new();

        internal NetworkSimulatorSocketContext(NetworkSimulatorSocketManager manager) => _manager = manager;

        public void Bind(EndPoint endpoint) => _localEndpoint = endpoint;

        public void SendTo(byte[] data, EndPoint remoteEP) => SendTo(data, data.Length, remoteEP);
        public void SendTo(byte[] data, int length, EndPoint remoteEP)
        {
            var copy = new byte[length];
            Buffer.BlockCopy(data, 0, copy, 0, length);
            _manager.Deliver(copy, _localEndpoint, remoteEP);
        }

        public void SendToAsync(byte[] data, int length, EndPoint remoteEP) => SendTo(data, length, remoteEP);

        internal void Enqueue(byte[] data, EndPoint sender, double arrival)
        {
            lock (_lock)
                _pending.Add((arrival, data, sender));
        }

        public void Pump()
        {
            var now = _manager.Time;
            lock (_lock)
            {
                for (int i = _pending.Count - 1; i >= 0; i--)
                {
                    if (_pending[i].arrival <= now)
                    {
                        var pkt = _pending[i];
                        _pending.RemoveAt(i);

                        var buffer = BufferPool.GetBuffer(pkt.data.Length);
                        Buffer.BlockCopy(pkt.data, 0, buffer, 0, pkt.data.Length);

                        _readyQueue.Enqueue(new Datagram
                        {
                            payload = buffer,
                            payloadSize = pkt.data.Length,
                            sender = pkt.sender
                        });
                    }
                }
            }
        }

        public bool Read(out Datagram packet) => _readyQueue.TryDequeue(out packet);

        public void Close() => _manager.Remove(_localEndpoint);
        public void Dispose() => Close();
    }
}