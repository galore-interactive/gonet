﻿using System.Threading;

namespace Disruptor
{

    /// <summary>
    /// Blocking wait strategy that uses <c>Monitor.Wait</c> and <c>Monitor.PulseAll</c>.
    /// </summary>
    /// <remarks>
    /// This strategy can be used when throughput and low-latency are not as important as CPU resources.
    /// This strategy uses an <see cref="AggressiveSpinWait"/> when waiting for the dependent sequence, which can generate CPU spikes.
    ///
    /// Consider using <see cref="BlockingSpinWaitWaitStrategy"/> to avoid CPU spikes.
    /// </remarks>
    public sealed class BlockingWaitStrategy : IWaitStrategy
    {
        private readonly object _gate = new();

        public bool IsBlockingStrategy => true;

        public SequenceWaitResult WaitFor(long sequence, DependentSequenceGroup dependentSequences, CancellationToken cancellationToken)
        {
            if (dependentSequences.CursorValue < sequence)
            {
                lock (_gate)
                {
                    while (dependentSequences.CursorValue < sequence)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        Monitor.Wait(_gate);
                    }
                }
            }

            return dependentSequences.AggressiveSpinWaitFor(sequence, cancellationToken);
        }

        public void SignalAllWhenBlocking()
        {
            lock (_gate)
            {
                Monitor.PulseAll(_gate);
            }
        }
    }
}