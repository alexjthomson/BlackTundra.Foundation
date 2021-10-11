using BlackTundra.Foundation.Utility;

using System;
using System.Text;

using UnityEngine;

namespace BlackTundra.Foundation.Collections {

    /// <summary>
    /// Stores a single key value pair that can exist inside of a <see cref="Keystore"/>.
    /// </summary>
    [Serializable]
    public sealed class KeystoreEntry {

        /*
         * BYTE FORMATTING
         * [(int)_hash][(byte)_key.Length][(byte[])_key][(int)data.Length][(byte[])data]
         */

        #region constant

        private const int FixedToBytesSize = sizeof(int) + 1 + sizeof(int);

        #endregion

        #region variable

        /// <inheritdoc cref="hash"/>
        [SerializeField]
        internal int _hash;

        // key length stored as byte
        /// <inheritdoc cref="key"/>
        [SerializeField]
        internal string _key;

        /// <summary>
        /// Data associated with this <see cref="KeystoreEntry"/>.
        /// This cannot be <c>null</c>.
        /// </summary>
        [SerializeField]
        internal byte[] _data;

        #endregion

        #region property

        /// <summary>
        /// Hash code used for quicky finding this <see cref="KeystoreEntry"/>.
        /// </summary>
        public int hash => _hash;

        /// <summary>
        /// Full name of the <see cref="KeystoreEntry"/> key.
        /// </summary>
        public string key {
            get => _key;
            set {
                if (key == null) throw new ArgumentNullException(nameof(key));
                if (key.Length > byte.MaxValue) throw new ArgumentException($"{nameof(key)} length exceeded {byte.MaxValue}.");
                _key = value;
                _hash = key.ToGUID();
            }
        }

        public byte[] data {
            get {
                int dataLength = _data.Length;
                if (dataLength == 0) return new byte[0];
                byte[] returnValue = new byte[dataLength];
                Array.Copy(_data, 0, returnValue, 0, dataLength);
                return returnValue;
            }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));
                long valueLength = value.LongLength;
                if (valueLength == 0L) _data = new byte[0];
                else if (valueLength > int.MaxValue) throw new ArgumentException(nameof(value));
                else {
                    int intValueLength = (int)valueLength;
                    _data = new byte[intValueLength];
                    Array.Copy(value, 0, _data, 0, intValueLength);
                }
            }
        }

        public byte this[in int index] {
            get => _data[index];
            set => _data[index] = value;
        }

        public int Length => _data.Length;

        #endregion

        #region constructor

        private KeystoreEntry() => throw new NotSupportedException();

        internal KeystoreEntry(in string key, in byte[] data) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length > byte.MaxValue) throw new ArgumentException($"{nameof(key)} length exceeded {byte.MaxValue}.");
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.LongLength > int.MaxValue) throw new ArgumentException($"{nameof(data)} length exceeded {int.MaxValue}.");
            _hash = key.ToGUID();
            _key = key;
            _data = data;
        }

        private KeystoreEntry(in int hash, in string key, in byte[] data) {
            _hash = hash;
            _key = key;
            _data = data;
        }

        #endregion

        #region logic

        #region ToBytes

        public byte[] ToBytes() {
            int keySize = _key.Length;
            if (keySize > byte.MaxValue) throw new OutOfMemoryException($"{nameof(_key)} length exceeded {byte.MaxValue}.");
            int dataSize = _data != null ? _data.Length : 0;
            byte[] buffer = new byte[FixedToBytesSize + keySize + dataSize];
            // hash:
            byte[] temp = BitConverter.GetBytes(_hash);
            int index = temp.Length;
            Array.Copy(temp, 0, buffer, 0, index);
            // key length:
            buffer[index++] = (byte)keySize; // cast key length into byte
            // key value:
            temp = Encoding.ASCII.GetBytes(_key);
            Array.Copy(temp, 0, buffer, index, temp.Length);
            index += temp.Length;
            // data length:
            temp = BitConverter.GetBytes(dataSize);
            Array.Copy(temp, 0, buffer, index, temp.Length);
            index += temp.Length;
            // data:
            if (dataSize > 0) Array.Copy(_data, 0, buffer, index, dataSize);
            return buffer;
        }

        #endregion

        #region FromBytes

        public static KeystoreEntry FromBytes(in byte[] bytes) => FromBytes(bytes, 0, out _);

        public static KeystoreEntry FromBytes(in byte[] bytes, in int startIndex) => FromBytes(bytes, startIndex, out _);

        public static KeystoreEntry FromBytes(in byte[] bytes, in int startIndex, out int endIndex) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (startIndex < 0 || startIndex >= bytes.Length - FixedToBytesSize) throw new ArgumentOutOfRangeException(nameof(startIndex));
            // hash:
            int hash = BitConverter.ToInt32(bytes, startIndex);
            endIndex = startIndex + sizeof(int); // index to read from
            // key length:
            int keyLength = bytes[endIndex++];
            // key value:
            string key = Encoding.ASCII.GetString(bytes, endIndex, keyLength);
            if (key.ToGUID() != hash) throw new InvalidOperationException($"Hash mismatch for key \"{key}\" (expected: \"{key.ToGUID()}\", actual: \"{hash}\").");
            endIndex += keyLength;
            // data length:
            int dataLength = BitConverter.ToInt32(bytes, endIndex);
            endIndex += sizeof(int);
            // data:
            byte[] data = new byte[dataLength];
            if (dataLength > 0) {
                Array.Copy(bytes, endIndex, data, 0, dataLength);
                endIndex += dataLength;
            }
            // finalize:
            return new KeystoreEntry(hash, key, data);
        }

        #endregion

        #endregion

    }

}