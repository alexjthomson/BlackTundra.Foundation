using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Serialization;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Collections {

    /// <summary>
    /// Keystore that efficiently stores data into a serializable format.
    /// This is slow at getting and setting data but is more secure than other methods of storing data and is less prone to tampering.
    /// </summary>
    /// <seealso cref="ToBytes"/>
    /// <seealso cref="FromBytes(in byte[])"/>
    public sealed class Keystore : IDictionary<string, byte[]> {

        #region constant

        /// <summary>
        /// Initial capacity of the <see cref="buffer"/>.
        /// </summary>
        private const int BufferInitialCapacity = 16;

        /// <summary>
        /// Number of elements to expand the <see cref="buffer"/> by when additional space is needed.
        /// </summary>
        private const int BufferExpandSize = 16;

        /// <summary>
        /// Maxium size of the <see cref="buffer"/>.
        /// </summary>
        private const int MaxBufferCapacity = ushort.MaxValue;

        #endregion

        #region variable

        private readonly PackedBuffer<KeystoreEntry> buffer;

        #endregion

        #region property

        public ICollection<string> Keys {
            get {
                int keyCount = buffer.Count;
                string[] keys = new string[keyCount];
                for (int i = keyCount - 1; i >= 0; i--) keys[i] = buffer[i]._key;
                return keys;
            }
        }

        public ICollection<byte[]> Values {
            get {
                int valueCount = buffer.Count;
                byte[][] values = new byte[valueCount][];
                for (int i = valueCount - 1; i >= 0; i--) values[i] = buffer[i].data;
                return values;
            }
        }

        public int Count => buffer.Count;

        public bool IsReadOnly => false;

        public byte[] this[string key] {
            get {
                if (key == null) throw new ArgumentNullException(nameof(key));
                int index = IndexOf(key);
                if (index != -1) throw new KeyNotFoundException();
                byte[] data = buffer[index]._data;
                int dataLength = data.Length;
                byte[] returnData = new byte[dataLength];
                Array.Copy(data, 0, returnData, 0, dataLength);
                return returnData;
            }
            set {
                if (key == null) throw new ArgumentNullException(nameof(key));
                if (value == null) throw new ArgumentNullException(nameof(value));
                int index = IndexOf(key);
                if (index != -1) throw new KeyNotFoundException();
                int valueLength = value.Length;
                byte[] newValue = new byte[valueLength];
                Array.Copy(value, 0, newValue, 0, valueLength);
                buffer[index]._data = newValue;
            }
        }

        #endregion

        #region constructor

        public Keystore() {
            buffer = new PackedBuffer<KeystoreEntry>(BufferInitialCapacity);
        }

        public Keystore(in int capacity) {
            if (capacity < 0 || capacity > MaxBufferCapacity) throw new ArgumentOutOfRangeException(nameof(capacity));
            buffer = new PackedBuffer<KeystoreEntry>(capacity);
        }

        private Keystore(in PackedBuffer<KeystoreEntry> buffer) {
            this.buffer = buffer;
        }

        #endregion

        #region logic

        #region Set

        public void Set<T>(in string key, in T value) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            Add(key, value != null ? ObjectSerializer.SerializeToBytes(value, true) : new byte[0]);
        }

        #endregion

        #region Get

        public T Get<T>(in string key) {
            if (key == null) throw new ArgumentNullException();
            int index = IndexOf(key);
            if (index == -1) return default;
            byte[] data = buffer[index]._data;
            if (data.Length == 0) return default;
            return ObjectSerializer.ToObject<T>(buffer[index]._data);
        }

        #endregion

        #region GetSet

        public T GetSet<T>(in string key, in T defaultValue) {
            if (key == null) throw new ArgumentNullException();
            int index = IndexOf(key);
            byte[] data;
            if (index == -1) { // no entry found
                data = defaultValue != null ? ObjectSerializer.SerializeToBytes(defaultValue, true) : null;
                AddLast(new KeystoreEntry(key, data));
                return defaultValue;
            } else { // entry found
                data = buffer[index]._data;
                if (data.Length == 0) return default; // null/default value
                return ObjectSerializer.ToObject<T>(data); // return deserialized value
            }
        }

        #endregion

        #region ToBytes

        public byte[] ToBytes() {
            int entryCount = buffer.Count; // get the total number of entries into the keystore
            if (entryCount == 0) return BitConverter.GetBytes((ushort)0); // there are no entries, return zero
            byte[][] staggeredOutputBytes = new byte[entryCount + 1][]; // create a staggered byte array to cache output results into
            byte[] temp = BitConverter.GetBytes((ushort)entryCount); // create a temporary byte array to store results in for processing
            int totalBytes = temp.Length; // create a total byte counter
            staggeredOutputBytes[0] = temp; // add the number of entries into the byte buffer
            for (int i = 0; i < entryCount;) { // iterate each entry put into the keystore
                temp = buffer[i].ToBytes(); // get the bytes for this entry
                totalBytes += temp.Length; // add to the total byte count
                staggeredOutputBytes[++i] = temp; // store the bytes into the staggered byte buffer
            }
            byte[] outputBytes = new byte[totalBytes]; // create a new byte array for the output bytes
            int index = 0; // create an index to start inserting bytes into the output bytes buffer at
            int tempLength; // temporary variable used to store the current length of the temp array
            for (int i = 0; i <= entryCount; i++) { // iterate each entry in the staggered output buffer
                temp = staggeredOutputBytes[i]; // get a reference to the current output array
                tempLength = temp.Length;
                Array.Copy(temp, 0, outputBytes, index, tempLength);
                index += tempLength;
            }
            return outputBytes; // return the output bytes
        }

        #endregion

        #region FromBytes

        public static Keystore FromBytes(in byte[] bytes) => FromBytes(bytes, 0, out _);

        public static Keystore FromBytes(in byte[] bytes, in int startIndex) => FromBytes(bytes, startIndex, out _);

        public static Keystore FromBytes(in byte[] bytes, in int startIndex, out int endIndex) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (startIndex < 0 || startIndex >= bytes.Length - sizeof(ushort)) throw new ArgumentOutOfRangeException(nameof(startIndex));
            int byteCount = bytes.Length; // get the number of bytes
            if (byteCount <= 2) throw new ArgumentException(nameof(bytes)); // not enough bytes to store the smallest possible value
            int entryCount = BitConverter.ToUInt16(bytes, startIndex); // get the number of entries into the keystore
            endIndex = startIndex + sizeof(ushort); // assign the end index (which will be used as the start index until the end of the method) to where the next entry is expected
            if (entryCount > 0) { // there are elements in the keystore
                KeystoreEntry[] buffer = new KeystoreEntry[entryCount]; // create an array to store the expected number of keystore entries
                for (int i = 0; i < entryCount; i++) { // iterate each entry in the keystore
                    buffer[i] = KeystoreEntry.FromBytes(bytes, endIndex, out int nextIndex); // construct the current keystore entry from bytes
                    endIndex = nextIndex; // assign the new end index (which is used as the next start index)
                }
                return new Keystore(new PackedBuffer<KeystoreEntry>(buffer)); // return a keystore constructed from the found entries
            } else { // there are no elements in the keystore
                return new Keystore(); // return an empty keystore
            }
        }

        #endregion

        #region IndexOf

        public int IndexOf(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            int bufferCount = buffer.Count;
            if (bufferCount == 0) return -1;
            int hash = key.ToGUID();
            KeystoreEntry entry;
            for (int i = bufferCount - 1; i >= 0; i--) {
                entry = buffer[i];
                if (entry._hash == hash) return i; // match found
            }
            return -1; // no match found
        }

        #endregion

        #region Add

        public void Add(string key, byte[] value) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            int index = IndexOf(key);
            if (index == -1) { // no existing entry found
                AddLast(new KeystoreEntry(key, value));
            } else {
                buffer[index].data = value; // override value
            }
        }

        public void Add(KeyValuePair<string, byte[]> item) {
            int index = IndexOf(item.Key);
            if (index == -1) { // no existing entry found
                AddLast(new KeystoreEntry(item.Key, item.Value));
            } else {
                buffer[index].data = item.Value; // override value
            }
        }

        #endregion

        #region AddLast

        /// <summary>
        /// Invoked when an entry should be added to the end of the <see cref="buffer"/>.
        /// </summary>
        private void AddLast(in KeystoreEntry entry) {
            // no null check required since this is private and entry can never be null
            if (buffer.IsFull) {
                int bufferCount = buffer.Count;
                if (bufferCount >= MaxBufferCapacity) throw new OutOfMemoryException();
                int expandLimit = MaxBufferCapacity - bufferCount; // find max expand size
                int expandSize = BufferExpandSize <= expandLimit ? BufferExpandSize : expandLimit; // calculate the amount that the buffer can expand by
                buffer.Expand(expandSize); // expand the buffer
            }
            buffer.AddLast(entry);
        }

        #endregion

        #region Contains

        public bool Contains(KeyValuePair<string, byte[]> item) => IndexOf(item.Key) != -1;

        #endregion

        #region ContainsKey

        public bool ContainsKey(string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return IndexOf(key) != -1;
        }

        #endregion

        #region Remove

        public bool Remove(string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            int index = IndexOf(key);
            if (index != -1) {
                buffer.RemoveAt(index);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<string, byte[]> item) {
            int index = IndexOf(item.Key);
            if (index != -1) {
                KeystoreEntry entry = buffer[index];
                if (entry._data.ContentEquals(item.Value)) {
                    buffer.RemoveAt(index);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region TryGetValue

        public bool TryGetValue(string key, out byte[] value) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            int index = IndexOf(key); // get index of key
            if (index != -1) { // match found
                value = buffer[index]._data;
                return true;
            } else { // no matches found
                value = null;
                return false;
            }
        }

        #endregion

        #region Clear

        public void Clear() => buffer.Clear();

        #endregion

        #region CopyTo

        public void CopyTo(KeyValuePair<string, byte[]>[] array, int arrayIndex) {
            if (arrayIndex < 0 || arrayIndex >= array.Length) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            KeystoreEntry entry;
            for (int i = buffer.Count - 1; i >= 0; i--) {
                entry = buffer[i];
                array[arrayIndex++] = new KeyValuePair<string, byte[]>(entry._key, entry._data);
            }
        }

        #endregion

        #region GetEnumerator

        public IEnumerator<KeyValuePair<string, byte[]>> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

    }

}