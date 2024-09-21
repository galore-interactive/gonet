﻿using System;
using System.Runtime.CompilerServices;
using Disruptor.Dsl;
using Disruptor.Processing;
using Disruptor.Util;

namespace Disruptor
{

    /// <summary>
    /// Ring based store of reusable entries containing the data representing
    /// an event being exchanged between event producer and <see cref="IEventProcessor"/>s.
    /// </summary>
    /// <typeparam name="T">implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class ValueRingBuffer<T> : RingBuffer, IValueRingBuffer<T>
        where T : struct
    {
        private static readonly int _bufferPad = InternalUtil.GetRingBufferPaddingEventCount(InternalUtil.SizeOf<T>());

        /// <summary>
        /// Construct a ValueRingBuffer with the full option set.
        /// </summary>
        /// <param name="sequencer">sequencer to handle the ordering of events moving through the ring buffer.</param>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public ValueRingBuffer(ISequencer sequencer)
            : this(() => default(T), sequencer)
        {
        }

        /// <summary>
        /// Construct a ValueRingBuffer with the full option set.
        /// </summary>
        /// <param name="eventFactory">eventFactory to create entries for filling the ring buffer</param>
        /// <param name="sequencer">sequencer to handle the ordering of events moving through the ring buffer.</param>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public ValueRingBuffer(Func<T> eventFactory, ISequencer sequencer)
            : base(sequencer, typeof(T), _bufferPad)
        {
            Fill(eventFactory);
        }

        private void Fill(Func<T> eventFactory)
        {
            for (var index = 0; index < _bufferSize; index++)
            {
                this[index] = eventFactory.Invoke();
            }
        }

        /// <summary>
        /// Construct a ValueRingBuffer with a <see cref="SequencerFactory.DefaultProducerType"/> sequencer.
        /// </summary>
        /// <param name="eventFactory"> eventFactory to create entries for filling the ring buffer</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        public ValueRingBuffer(Func<T> eventFactory, int bufferSize)
            : this(eventFactory, SequencerFactory.Create(SequencerFactory.DefaultProducerType, bufferSize))
        {
        }

        /// <summary>
        /// Create a new multiple producer ValueRingBuffer with the specified wait strategy.
        /// </summary>
        /// <param name="factory">used to create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <returns>a constructed ring buffer.</returns>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public static ValueRingBuffer<T> CreateMultiProducer(Func<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            MultiProducerSequencer sequencer = new MultiProducerSequencer(bufferSize, waitStrategy);

            return new ValueRingBuffer<T>(factory, sequencer);
        }

        /// <summary>
        /// Create a new multiple producer ValueRingBuffer using <see cref="SequencerFactory.DefaultWaitStrategy"/>.
        /// </summary>
        /// <param name="factory">used to create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        /// <returns>a constructed ring buffer.</returns>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public static ValueRingBuffer<T> CreateMultiProducer(Func<T> factory, int bufferSize)
        {
            return CreateMultiProducer(factory, bufferSize, SequencerFactory.DefaultWaitStrategy());
        }

        /// <summary>
        /// Create a new single producer ValueRingBuffer with the specified wait strategy.
        /// </summary>
        /// <param name="factory">used to create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <returns>a constructed ring buffer.</returns>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public static ValueRingBuffer<T> CreateSingleProducer(Func<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            SingleProducerSequencer sequencer = new SingleProducerSequencer(bufferSize, waitStrategy);

            return new ValueRingBuffer<T>(factory, sequencer);
        }

        /// <summary>
        /// Create a new single producer ValueRingBuffer using <see cref="SequencerFactory.DefaultWaitStrategy"/>.
        /// </summary>
        /// <param name="factory">used to create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        /// <returns>a constructed ring buffer.</returns>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public static ValueRingBuffer<T> CreateSingleProducer(Func<T> factory, int bufferSize)
        {
            return CreateSingleProducer(factory, bufferSize, SequencerFactory.DefaultWaitStrategy());
        }

        /// <summary>
        /// Create a new ValueRingBuffer with the specified producer type.
        /// </summary>
        /// <param name="producerType">producer type to use <see cref="ProducerType" /></param>
        /// <param name="factory">used to create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <returns>a constructed ring buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the producer type is invalid</exception>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        public static ValueRingBuffer<T> Create(ProducerType producerType, Func<T> factory, int bufferSize, IWaitStrategy waitStrategy)
        {
            switch (producerType)
            {
                case ProducerType.Single:
                    return CreateSingleProducer(factory, bufferSize, waitStrategy);
                case ProducerType.Multi:
                    return CreateMultiProducer(factory, bufferSize, waitStrategy);
                default:
                    throw new ArgumentOutOfRangeException(producerType.ToString());
            }
        }

        /// <summary>
        /// Gets the event for a given sequence in the ring buffer.
        /// </summary>
        /// <param name="sequence">sequence for the event</param>
        /// <remarks>
        /// This method should be used for publishing events to the ring buffer:
        /// <code>
        /// long sequence = ringBuffer.Next();
        /// try
        /// {
        ///     ref var eventToPublish = ref ringBuffer[sequence];
        ///     // Configure the event
        /// }
        /// finally
        /// {
        ///     ringBuffer.Publish(sequence);
        /// }
        /// </code>
        ///
        /// This method can also be used for event processing but in most cases the processing is performed
        /// in the provided <see cref="IEventProcessor"/> types or in the event pollers.
        /// </remarks>
        public ref T this[long sequence]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref InternalUtil.ReadValue<T>(_entries, _bufferPad + (int)(sequence & _indexMask));
            }
        }

        /// <summary>
        /// Sets the cursor to a specific sequence and returns the preallocated entry that is stored there.  This
        /// can cause a data race and should only be done in controlled circumstances, e.g. during initialisation.
        /// </summary>
        /// <param name="sequence">the sequence to claim.</param>
        /// <returns>the preallocated event.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ClaimAndGetPreallocated(long sequence)
        {
            _sequencerDispatcher.Sequencer.Claim(sequence);
            return ref this[sequence];
        }

        public override string ToString()
        {
            return $"ValueRingBuffer {{Type={typeof(T).Name}, BufferSize={_bufferSize}, Sequencer={_sequencerDispatcher.Sequencer.GetType().Name}}}";
        }

        /// <summary>
        /// Creates an event poller for this ring buffer gated on the supplied sequences.
        /// </summary>
        /// <param name="gatingSequences">gatingSequences to be gated on.</param>
        /// <returns>A poller that will gate on this ring buffer and the supplied sequences.</returns>
        public ValueEventPoller<T> NewPoller(params Sequence[] gatingSequences)
        {
            return _sequencerDispatcher.Sequencer.NewPoller(this, gatingSequences);
        }

        /// <summary>
        /// Increment the ring buffer sequence and return a scope that will publish the sequence on disposing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If there is not enough space available in the ring buffer, this method will block and spin-wait using <see cref="AggressiveSpinWait"/>, which can generate high CPU usage.
        /// Consider using <see cref="TryPublishEvent()"/> with your own waiting policy if you need to change this behavior.
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// using (var scope = _ringBuffer.PublishEvent())
        /// {
        ///     ref var e = ref scope.Event();
        ///     // Do some work with the event.
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnpublishedEventScope PublishEvent()
        {
            var sequence = Next();
            return new UnpublishedEventScope(this, sequence);
        }

        /// <summary>
        /// Try to increment the ring buffer sequence and return a scope that will publish the sequence on disposing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method will not block if there is not enough space available in the ring buffer.
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// using (var scope = _ringBuffer.TryPublishEvent())
        /// {
        ///     if (!scope.TryGetEvent(out var eventRef))
        ///         return;
        ///
        ///     ref var e = ref eventRef.Event();
        ///     // Do some work with the event.
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullableUnpublishedEventScope TryPublishEvent()
        {
            var success = TryNext(out var sequence);
            return new NullableUnpublishedEventScope(success ? this : null, sequence);
        }

        /// <summary>
        /// Increment the ring buffer sequence by <paramref name="count"/> and return a scope that will publish the sequences on disposing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If there is not enough space available in the ring buffer, this method will block and spin-wait using <see cref="AggressiveSpinWait"/>, which can generate high CPU usage.
        /// Consider using <see cref="TryPublishEvents(int)"/> with your own waiting policy if you need to change this behavior.
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// using (var scope = _ringBuffer.PublishEvents(2))
        /// {
        ///     ref var e1 = ref scope.Event(0);
        ///     ref var e2 = ref scope.Event(1);
        ///     // Do some work with the events.
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnpublishedEventBatchScope PublishEvents(int count)
        {
            var endSequence = Next(count);
            return new UnpublishedEventBatchScope(this, endSequence + 1 - count, endSequence);
        }

        /// <summary>
        /// Try to increment the ring buffer sequence by <paramref name="count"/> and return a scope that will publish the sequences on disposing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method will not block when there is not enough space available in the ring buffer.
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// using (var scope = _ringBuffer.TryPublishEvent(2))
        /// {
        ///     if (!scope.TryGetEvents(out var eventsRef))
        ///         return;
        ///
        ///     ref var e1 = ref eventRefs.Event(0);
        ///     ref var e2 = ref eventRefs.Event(1);
        ///     // Do some work with the events.
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NullableUnpublishedEventBatchScope TryPublishEvents(int count)
        {
            var success = TryNext(count, out var endSequence);
            return new NullableUnpublishedEventBatchScope(success ? this : null, endSequence + 1 - count, endSequence);
        }

        /// <summary>
        /// Holds an unpublished sequence number.
        /// Publishes the sequence number on disposing.
        /// </summary>
        public readonly struct UnpublishedEventScope : IDisposable
        {
            private readonly ValueRingBuffer<T> _ringBuffer;
            private readonly long _sequence;

            public UnpublishedEventScope(ValueRingBuffer<T> ringBuffer, long sequence)
            {
                _ringBuffer = ringBuffer;
                _sequence = sequence;
            }

            public long Sequence => _sequence;

            /// <summary>
            /// Gets the event at the claimed sequence number.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T Event() => ref _ringBuffer[_sequence];

            /// <summary>
            /// Publishes the sequence number.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _ringBuffer.Publish(_sequence);
        }

        /// <summary>
        /// Holds an unpublished sequence number batch.
        /// Publishes the sequence numbers on disposing.
        /// </summary>
        public readonly struct UnpublishedEventBatchScope : IDisposable
        {
            private readonly ValueRingBuffer<T> _ringBuffer;
            private readonly long _startSequence;
            private readonly long _endSequence;

            public UnpublishedEventBatchScope(ValueRingBuffer<T> ringBuffer, long startSequence, long endSequence)
            {
                _ringBuffer = ringBuffer;
                _startSequence = startSequence;
                _endSequence = endSequence;
            }

            public long StartSequence => _startSequence;
            public long EndSequence => _endSequence;

            /// <summary>
            /// Gets the event at the specified index in the claimed sequence batch.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T Event(int index) => ref _ringBuffer[_startSequence + index];

            /// <summary>
            /// Publishes the sequence number batch.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _ringBuffer.Publish(_startSequence, _endSequence);
        }

        /// <summary>
        /// Holds an unpublished sequence number.
        /// Publishes the sequence number on disposing.
        /// </summary>
        public readonly struct NullableUnpublishedEventScope : IDisposable
        {
            private readonly ValueRingBuffer<T>? _ringBuffer;
            private readonly long _sequence;

            public NullableUnpublishedEventScope(ValueRingBuffer<T>? ringBuffer, long sequence)
            {
                _ringBuffer = ringBuffer;
                _sequence = sequence;
            }

            /// <summary>
            /// Returns a value indicating whether the sequence was successfully claimed.
            /// </summary>
            public bool HasEvent => _ringBuffer != null;

            /// <summary>
            /// Gets the event at the claimed sequence number.
            /// </summary>
            /// <returns>
            /// true if the sequence number was successfully claimed, false otherwise.
            /// </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetEvent(out EventRef eventRef)
            {
                eventRef = new EventRef(_ringBuffer!, _sequence);
                return _ringBuffer != null;
            }

            /// <summary>
            /// Publishes the sequence number if it was successfully claimed.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (_ringBuffer != null)
                    _ringBuffer.Publish(_sequence);
            }
        }

        /// <summary>
        /// Holds an unpublished sequence number.
        /// </summary>
        public readonly struct EventRef
        {
            private readonly ValueRingBuffer<T> _ringBuffer;
            private readonly long _sequence;

            public EventRef(ValueRingBuffer<T> ringBuffer, long sequence)
            {
                _ringBuffer = ringBuffer;
                _sequence = sequence;
            }

            public long Sequence => _sequence;

            /// <summary>
            /// Gets the event at the claimed sequence number.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T Event() => ref _ringBuffer[_sequence];
        }

        /// <summary>
        /// Holds an unpublished sequence number batch.
        /// Publishes the sequence numbers on disposing.
        /// </summary>
        public readonly struct NullableUnpublishedEventBatchScope : IDisposable
        {
            private readonly ValueRingBuffer<T>? _ringBuffer;
            private readonly long _startSequence;
            private readonly long _endSequence;

            public NullableUnpublishedEventBatchScope(ValueRingBuffer<T>? ringBuffer, long startSequence, long endSequence)
            {
                _ringBuffer = ringBuffer;
                _startSequence = startSequence;
                _endSequence = endSequence;
            }

            /// <summary>
            /// Returns a value indicating whether the sequence batch was successfully claimed.
            /// </summary>
            public bool HasEvents => _ringBuffer != null;

            /// <summary>
            /// Gets the events for the associated sequence number batch.
            /// </summary>
            /// <returns>
            /// true if the sequence batch was successfully claimed, false otherwise.
            /// </returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGetEvents(out EventBatchRef eventRef)
            {
                eventRef = new EventBatchRef(_ringBuffer!, _startSequence, _endSequence);
                return _ringBuffer != null;
            }

            /// <summary>
            /// Publishes the sequence batch if it was successfully claimed.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (_ringBuffer != null)
                    _ringBuffer.Publish(_startSequence, _endSequence);
            }
        }

        /// <summary>
        /// Holds an unpublished sequence number batch.
        /// </summary>
        public readonly struct EventBatchRef
        {
            private readonly ValueRingBuffer<T> _ringBuffer;
            private readonly long _startSequence;
            private readonly long _endSequence;

            public EventBatchRef(ValueRingBuffer<T> ringBuffer, long startSequence, long endSequence)
            {
                _ringBuffer = ringBuffer;
                _startSequence = startSequence;
                _endSequence = endSequence;
            }

            public long StartSequence => _startSequence;
            public long EndSequence => _endSequence;

            /// <summary>
            /// Gets the event at the specified index in the claimed sequence batch.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T Event(int index) => ref _ringBuffer[_startSequence + index];
        }
    }
}