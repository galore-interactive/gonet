using MemoryPack;
using System.Collections.Generic;
using System.IO;

namespace GONet.Utils
{
    public class FileBackedMap<TKey, TValue>
    {
        private string filePath;

        private Dictionary<TKey, TValue> dictionary;

        public FileBackedMap(string filePath)
        {
            this.filePath = filePath;

            if (!File.Exists(filePath))
            {
                using (var stream = File.Create(filePath))
                {
                    // haha close me slueth
                }
            }

            InitFromFile();
        }

        public TValue this[TKey key]
        {
            get
            {
                InitFromFile();
                return dictionary[key];
            }

            set
            {
                dictionary[key] = value;
                SaveToFile();
            }
        }

        public int Count => dictionary.Count;

        public void Clear()
        {
            dictionary.Clear();
            SaveToFile();
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            bool wasRemoved = dictionary.Remove(key);
            SaveToFile();
            return wasRemoved;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        private void InitFromFile()
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length == 0)
            {
                dictionary = new Dictionary<TKey, TValue>();
            }
            else
            {
                dictionary = SerializationUtils.DeserializeFromBytes<Dicky<TKey, TValue>>(fileBytes).dictionary;
            }
        }

        private void SaveToFile()
        {
            int returnBytesUsedCount;
            byte[] fileBytes = SerializationUtils.SerializeToBytes(new Dicky<TKey, TValue>(dictionary), out returnBytesUsedCount, out bool doesNeedToReturn);
            FileUtils.WriteBytesToFile(filePath, fileBytes, returnBytesUsedCount, FileMode.Truncate);
            if (doesNeedToReturn)
            {
                SerializationUtils.ReturnByteArray(fileBytes);
            }
        }
    }

    [MemoryPackable]
    public partial class Dicky<TKey, TValue>
    {
        public Dictionary<TKey, TValue> dictionary;

        [MemoryPackConstructor]
        public Dicky() { }

        public Dicky(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }
    }
}
