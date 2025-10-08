using System;
using System.Runtime.CompilerServices; // Required for AggressiveInlining
using System.Runtime.InteropServices; // Required for StructLayout/FieldOffset
using System.Threading; // Required for volatile (though implicit via volatile keyword)

namespace GONet.Utils // Assuming the original namespace
{
    /// <summary>
    /// A ring buffer optimized for single-producer, single-consumer (SPSC) scenarios.
    /// It ensures the buffer capacity is a power of two to enable fast bitwise indexing
    /// and includes padding to mitigate false sharing between read/write indices.
    ///
    /// IMPORTANT: This class is NOT thread-safe for multiple producers or multiple consumers
    /// accessing it concurrently. It relies on volatile reads/writes for memory visibility
    /// between ONE producer and ONE consumer thread.
    ///
    /// DYNAMIC RESIZING: When the buffer reaches 75% capacity, it automatically doubles in size
    /// up to a maximum of 16384 entries. This ensures the buffer "just works" for high-load
    /// scenarios without requiring manual configuration.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the buffer.</typeparam>
    public class RingBuffer<T>
    {
        private T[] _buffer;
        private int _mask; // Used for fast bitwise indexing (Capacity - 1)

        // Struct to hold the index and padding to ensure cache line separation
        // Cache lines are typically 64 bytes.
        // Can't combine generic types with explicit memory layout because the runtime needs
        // flexibility in how it arranges generic type data in memory.
        [StructLayout(LayoutKind.Sequential)]
        private struct PaddedIndex
        {
            public volatile int Value;
            private readonly int _p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8;
            private readonly int _p9, _p10, _p11, _p12, _p13, _p14, _p15;
        }

        // Indices are NOT readonly, allowing their .Value to be modified.
        // Padding is handled by the struct layout.
        private PaddedIndex _readIndex;
        private PaddedIndex _writeIndex;

        // Dynamic resizing constants and state
        private const int INITIAL_SIZE = 2048; // Start at 2048 (handles most games without resizing)
        private const int MAX_SIZE = 16384; // Hard cap to prevent runaway memory usage (~1.4 MB)
        private const float RESIZE_THRESHOLD = 0.75f; // Resize when 75% full

        // Metrics tracking (thread-safe via volatile)
        private volatile int _peakCount; // Highest count ever reached
        private volatile int _resizeCount; // Number of times buffer has been resized
        private volatile bool _hasLoggedMaxCapacityWarning; // Only warn once at max capacity

        /// <summary>
        /// Action invoked when the buffer is resized. Parameters: (oldCapacity, newCapacity, currentCount)
        /// Used by GONet for logging and metrics tracking.
        /// </summary>
        public Action<int, int, int> OnResized { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingBuffer{T}"/> class.
        /// Starts with a capacity of 2048 and automatically grows as needed up to 16384.
        /// </summary>
        /// <param name="requestedSize">The desired initial minimum capacity (default: 2048). Clamped to range [1024-16384].</param>
        /// <exception cref="ArgumentException">Thrown if requestedSize is not positive.</exception>
        public RingBuffer(int requestedSize = INITIAL_SIZE)
        {
            if (requestedSize <= 0)
            {
                throw new ArgumentException("Requested size must be positive.", nameof(requestedSize));
            }

            // Clamp to reasonable range and ensure power of two
            requestedSize = Math.Max(1024, Math.Min(MAX_SIZE, requestedSize));
            int capacity = CeilingPowerOfTwo(requestedSize);

            _buffer = new T[capacity];
            _mask = capacity - 1; // Mask for bitwise AND, works only for power-of-two sizes

            // Initialize indices (Value is volatile)
            _readIndex = new PaddedIndex { Value = 0 };
            _writeIndex = new PaddedIndex { Value = 0 };

            // Initialize metrics
            _peakCount = 0;
            _resizeCount = 0;
            _hasLoggedMaxCapacityWarning = false;
        }

        /// <summary>
        /// Gets the actual capacity of the ring buffer (which is a power of two).
        /// </summary>
        public int Capacity => _buffer.Length;

        /// <summary>
        /// Gets the current number of items in the ring buffer.
        /// This property involves volatile reads and is suitable for diagnostics or
        /// scenarios where an approximate count is sufficient when read by the opposing thread.
        /// </summary>
        public int Count
        {
            get
            {
                // Reading volatile fields establishes memory barriers, ensuring we see up-to-date values
                // relative to other volatile reads/writes on other threads.
                int writeIdx = _writeIndex.Value;
                int readIdx = _readIndex.Value;

                // Calculation handles wrap-around
                return writeIdx >= readIdx ? writeIdx - readIdx : Capacity - readIdx + writeIdx;
            }
        }

        /// <summary>
        /// Gets the peak (maximum) number of items that have been in the buffer at once.
        /// </summary>
        public int PeakCount => _peakCount;

        /// <summary>
        /// Gets the number of times the buffer has been resized.
        /// </summary>
        public int ResizeCount => _resizeCount;

        /// <summary>
        /// Gets the current fill percentage (0.0 to 1.0).
        /// </summary>
        public float FillPercentage => Capacity > 0 ? (float)Count / Capacity : 0f;

        /// <summary>
        /// Attempts to write an item to the buffer. To be called only by the producer thread.
        /// Automatically resizes the buffer if it reaches 75% capacity (up to max size of 16384).
        /// </summary>
        /// <param name="item">The item to write.</param>
        /// <returns>True if the item was successfully written, false if the buffer is full and cannot resize.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(T item)
        {
            int currentWriteIndex = _writeIndex.Value;
            // Calculate the next index using bitwise AND (faster than modulo for power-of-two sizes)
            int nextWriteIndex = (currentWriteIndex + 1) & _mask;

            // Check if buffer is full (next write position would be current read position)
            // Volatile read of _readIndex ensures we see the latest value from the consumer
            if (nextWriteIndex == _readIndex.Value)
            {
                // Buffer is full - try to resize if possible
                if (!TryResize())
                {
                    return false; // Cannot resize, buffer genuinely full
                }

                // Buffer was resized, recalculate indices with new mask
                currentWriteIndex = _writeIndex.Value;
                nextWriteIndex = (currentWriteIndex + 1) & _mask;
            }

            // Place item in buffer *before* updating the index
            _buffer[currentWriteIndex] = item;

            // Update write index with a volatile write (ensures visibility to consumer)
            _writeIndex.Value = nextWriteIndex;

            // Track peak count for metrics
            int currentCount = Count;
            if (currentCount > _peakCount)
            {
                _peakCount = currentCount;
            }

            // Check if we should preemptively resize (at 75% capacity)
            if (currentCount >= Capacity * RESIZE_THRESHOLD && Capacity < MAX_SIZE)
            {
                TryResize();
            }

            return true;
        }

        /// <summary>
        /// Attempts to read an item from the buffer. To be called only by the consumer thread.
        /// </summary>
        /// <param name="item">When this method returns, contains the item read from the buffer,
        /// or the default value of T if the buffer was empty.</param>
        /// <returns>True if an item was successfully read, false if the buffer is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out T item)
        {
            int currentReadIndex = _readIndex.Value;

            // Check if buffer is empty (read position matches write position)
            // Volatile read of _writeIndex ensures we see the latest value from the producer
            if (currentReadIndex == _writeIndex.Value)
            {
                item = default;
                return false; // Buffer empty
            }

            // Retrieve item *before* updating the index
            item = _buffer[currentReadIndex];

            // Calculate the next index using bitwise AND
            int nextReadIndex = (currentReadIndex + 1) & _mask;

            // Update read index with a volatile write (ensures visibility to producer)
            _readIndex.Value = nextReadIndex;

            return true;
        }

        /// <summary>
        /// Attempts to resize the buffer to double its current capacity.
        /// Called automatically when buffer reaches 75% capacity or is full.
        /// </summary>
        /// <returns>True if resize succeeded, false if already at max capacity.</returns>
        private bool TryResize()
        {
            int currentCapacity = Capacity;

            // Already at max size?
            if (currentCapacity >= MAX_SIZE)
            {
                // Log warning only once when hitting max capacity
                if (!_hasLoggedMaxCapacityWarning)
                {
                    _hasLoggedMaxCapacityWarning = true;
                    OnResized?.Invoke(currentCapacity, currentCapacity, Count); // Signal "failed to resize"
                }
                return false;
            }

            // Calculate new capacity (double current, capped at MAX_SIZE)
            int newCapacity = Math.Min(currentCapacity * 2, MAX_SIZE);

            // Allocate new buffer
            T[] newBuffer = new T[newCapacity];
            int newMask = newCapacity - 1;

            // Copy all existing items from old buffer to new buffer
            int readIdx = _readIndex.Value;
            int writeIdx = _writeIndex.Value;
            int itemCount = Count;
            int newWriteIdx = 0;

            // Copy items maintaining order
            for (int i = 0; i < itemCount; i++)
            {
                newBuffer[newWriteIdx++] = _buffer[readIdx];
                readIdx = (readIdx + 1) & _mask;
            }

            // Atomically swap buffers and update indices
            _buffer = newBuffer;
            _mask = newMask;
            _readIndex.Value = 0;
            _writeIndex.Value = newWriteIdx;

            // Update metrics
            _resizeCount++;

            // Notify listeners (used by GONet for logging)
            OnResized?.Invoke(currentCapacity, newCapacity, itemCount);

            return true;
        }

        /// <summary>
        /// Calculates the smallest power of two integer that is greater than or equal to v.
        /// </summary>
        /// <param name="v">The input integer.</param>
        /// <returns>The smallest power of two greater than or equal to v.</returns>
        private static int CeilingPowerOfTwo(int v)
        {
            if (v <= 0) return 1; // Handle edge case

            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }
    }
}