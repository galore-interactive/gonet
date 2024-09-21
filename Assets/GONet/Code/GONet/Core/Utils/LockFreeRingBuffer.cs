namespace GONet.Utils
{
    public class LockFreeRingBuffer<T>
    {
        private readonly T[] buffer;
        private int writeIndex = 0;
        private int readIndex = 0;

        public LockFreeRingBuffer(int size)
        {
            buffer = new T[size];
        }

        public bool TryWrite(T item)
        {
            int nextWriteIndex = (writeIndex + 1) % buffer.Length;
            if (nextWriteIndex == readIndex)
            {
                return false; // Buffer full
            }
            buffer[writeIndex] = item;
            writeIndex = nextWriteIndex;
            return true;
        }

        public bool TryRead(out T item)
        {
            if (writeIndex == readIndex)
            {
                item = default;
                return false; // Buffer empty
            }
            item = buffer[readIndex];
            readIndex = (readIndex + 1) % buffer.Length;
            return true;
        }
    }

    public class SingleProducerRingBuffer<T>
    {
        private readonly T[] buffer;
        private int writeIndex = 0;
        private int readIndex = 0;

        public SingleProducerRingBuffer(int size)
        {
            buffer = new T[size];
        }

        public bool TryWrite(T item)
        {
            int nextWriteIndex = (writeIndex + 1) % buffer.Length;
            if (nextWriteIndex == readIndex)
            {
                // Buffer is full
                return false;
            }

            // Only the single producer can modify the write index, so no need for locks here
            buffer[writeIndex] = item;
            writeIndex = nextWriteIndex;
            return true;
        }

        public bool TryRead(out T item)
        {
            if (writeIndex == readIndex)
            {
                item = default;
                return false; // Buffer empty
            }

            item = buffer[readIndex];
            readIndex = (readIndex + 1) % buffer.Length;
            return true;
        }
    }

}
