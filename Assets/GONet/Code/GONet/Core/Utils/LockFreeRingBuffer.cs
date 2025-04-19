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
    /// </summary>
    /// <typeparam name="T">The type of items stored in the buffer.</typeparam>
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _mask; // Used for fast bitwise indexing (Capacity - 1)

        // Struct to hold the index and padding to ensure cache line separation
        // Cache lines are typically 64 bytes.
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct PaddedIndex
        {
            [FieldOffset(0)]
            public volatile int Value;
        }

        // Indices are NOT readonly, allowing their .Value to be modified.
        // Padding is handled by the struct layout.
        private PaddedIndex _readIndex;
        private PaddedIndex _writeIndex;


        /// <summary>
        /// Initializes a new instance of the <see cref="RingBuffer{T}"/> class.
        /// The actual capacity will be the smallest power of two that is greater than or equal to the requested size.
        /// </summary>
        /// <param name="requestedSize">The desired minimum capacity of the ring buffer. Must be positive.</param>
        /// <exception cref="ArgumentException">Thrown if requestedSize is not positive.</exception>
        public RingBuffer(int requestedSize)
        {
            if (requestedSize <= 0)
            {
                throw new ArgumentException("Requested size must be positive.", nameof(requestedSize));
            }

            // Ensure capacity is a power of two for fast bitwise & masking
            int capacity = CeilingPowerOfTwo(requestedSize);

            _buffer = new T[capacity];
            _mask = capacity - 1; // Mask for bitwise AND, works only for power-of-two sizes

            // Initialize indices (Value is volatile)
            _readIndex = new PaddedIndex { Value = 0 };
            _writeIndex = new PaddedIndex { Value = 0 };
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
        /// Attempts to write an item to the buffer. To be called only by the producer thread.
        /// </summary>
        /// <param name="item">The item to write.</param>
        /// <returns>True if the item was successfully written, false if the buffer is full.</returns>
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
                return false; // Buffer full
            }

            // Place item in buffer *before* updating the index
            _buffer[currentWriteIndex] = item;

            // Update write index with a volatile write (ensures visibility to consumer)
            _writeIndex.Value = nextWriteIndex;

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