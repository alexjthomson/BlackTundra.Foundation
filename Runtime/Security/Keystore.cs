using System;

using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Collections.Generic;

using UnityEngine;

namespace BlackTundra.Foundation.Security {

    /// <summary>
    /// Links a string key to a crypto-key.
    /// </summary>
    [Serializable]
    public sealed class Keystore {

        #region constant

        /// <summary>
        /// Default capacity of a keystore.
        /// </summary>
        private const int DefaultKeystoreCapacity = 16;

        /// <summary>
        /// How much the buffer is expanded when full and more space is required.
        /// </summary>
        private const int KeystoreExpandSize = 4;

        #endregion

        #region nested
        
        /// <summary>
        /// Links a hash to a value.
        /// </summary>
        [Serializable]
        private sealed class HashValuePair<T> {

            #region variable

            /// <summary>
            /// Hash associated with the value.
            /// </summary>
            internal int hash;

            /// <summary>
            /// Value of the KeyValuePair.
            /// </summary>
            internal T value;

            #endregion

            #region constructor

            internal HashValuePair(in int hash, in T value) {

                this.hash = hash;
                this.value = value;

            }

            #endregion

        }

        #endregion

        #region variable

        /// <summary>
        /// Buffer linking string keys to crypto keys.
        /// </summary>
        [SerializeField]
        private PackedBuffer<HashValuePair<byte[]>> buffer;

        #endregion

        #region property

        /// <summary>
        /// Allows manipulation of the keystore.
        /// </summary>
        /// <param name="key">Key to manipulate.</param>
        /// <returns>Value associated with the provided key. If the key doesn't exist in the keystore, a new crypto-key will be generated for the key and will added to the keystore.</returns>
        public byte[] this[in string key] {

            get {

                if (key == null) throw new ArgumentNullException(nameof(key));
                int hash = key.ToGUID();

                // find key:

                HashValuePair<byte[]> entry;
                for (int i = 0; i < buffer.Count; i++) {

                    entry = buffer[i];
                    if (entry.hash == hash) {

                        byte[] cryptoKey = entry.value;
                        if (cryptoKey.Length != CryptoUtility.CryptoKeySize) {
                            throw new KeystoreException(
                                $"Keystore entry \"{key}\" key size ({cryptoKey.Length}) doesn't match keystore key block size ({CryptoUtility.CryptoKeySize}).",
                                null
                            );
                        }
                        return cryptoKey;

                    }

                }

                // add key as not found:

                if (buffer.Count == buffer.Capacity) buffer.Expand(KeystoreExpandSize);
                byte[] value = CryptoUtility.GenerateCryptoKey();
                buffer.AddLast(new HashValuePair<byte[]>(hash, value));
                return value;

            }

            set {

                if (key == null) throw new ArgumentNullException(nameof(key));
                int hash = key.ToGUID();

                // find key:

                HashValuePair<byte[]> entry;
                for (int i = 0; i < buffer.Count; i++) {

                    entry = buffer[i];
                    if (entry.hash == hash) {

                        if (value == null) buffer.RemoveAt(i); // delete the key
                        else entry.value = value;
                        return;

                    }

                }

                // add key:

                if (value != null) {

                    if (buffer.Count == buffer.Capacity) buffer.Expand(KeystoreExpandSize);
                    buffer.AddLast(new HashValuePair<byte[]>(hash, value)); // add the entry to the buffer

                }

            }

        }

        #endregion

        #region constructor

        internal Keystore() => buffer = new PackedBuffer<HashValuePair<byte[]>>(DefaultKeystoreCapacity);

        #endregion

    }

}