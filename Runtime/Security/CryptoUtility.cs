using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

using BlackTundra.Foundation.Utility;

namespace BlackTundra.Foundation.Security {

    public static class CryptoUtility {

        #region constant

        /// <summary>
        /// Total number of bytes in an AES initialisation vector.
        /// </summary>
        private const int AesIvBlockSize = 16;

        /// <summary>
        /// Total number of bytes in an AES key.
        /// </summary>
        private const int AesKeyBlockSize = 32;

        private const int XKF0 = 6, XKF1 = 7, XKF2 = 3; // XOR Key Fragments

        /// <summary>
        /// Size of a key used for encrypting something.
        /// Key structure:
        /// <see cref="XKF0"/> bytes padding,
        /// <see cref="AesIvBlockSize"/> bytes IV,
        /// <see cref="XKF1"/> bytes padding,
        /// <see cref="AesKeyBlockSize"/> bytes key,
        /// <see cref="XKF2"/> bytes padding
        /// All of the padding is used to create an XOR key.
        /// </summary>
        public const int CryptoKeySize = XKF0 + AesIvBlockSize + XKF1 + AesKeyBlockSize + XKF2;

        /// <summary>
        /// Standard AES initialisation vector to use for all AES encryption.
        /// </summary>
        private static readonly byte[] AesStandardIv = new byte[AesIvBlockSize] {
            0xf4, 0x3e, 0xa2, 0x43,
            0x5c, 0x69, 0x14, 0x36,
            0x74, 0x61, 0x5b, 0x70,
            0x92, 0xf7, 0xa7, 0x10,
        };

        /// <summary>
        /// Standard AES key to use for all obfuscated encryption where the data should
        /// be encrypted to prevent easy modification. This is not hard to reverse
        /// since the key is known and not random. This just makes it slightly more
        /// difficult to modify the data.
        /// </summary>
        private static readonly byte[] AesStandardKey = new byte[AesKeyBlockSize] {
            0xb4, 0xa8, 0xfe, 0x02,
            0x20, 0x10, 0x26, 0xc7,
            0x51, 0xb4, 0x09, 0x4f,
            0x37, 0xe0, 0xa0, 0xb0,
            0xab, 0xd0, 0x03, 0x52,
            0xfa, 0x1e, 0xc5, 0x0d,
            0x20, 0x8a, 0x92, 0xc1,
            0x89, 0x34, 0x60, 0x70,
        };

        /// <summary>
        /// Random number generator.
        /// </summary>
        private static readonly RNGCryptoServiceProvider RNG = new RNGCryptoServiceProvider();

        private const int DefaultDecompressionSize = 1000000000;

        #endregion

        #region logic

        #region Encrypt

        /// <summary>
        /// Encrypts a raw byte array using AES encryption and then compresses the result using the GZIP algorithm.
        /// </summary>
        /// <param name="content">Raw bytes to encrypt.</param>
        /// <param name="key">Key to use for encryption.</param>
        /// <returns>Encrypted and compressed byte array.</returns>
        public static byte[] Encrypt(in byte[] content, in byte[] iv, in byte[] key) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (iv == null) throw new ArgumentNullException(nameof(iv));
            if (key == null) throw new ArgumentNullException(nameof(key));
            using var cryptoProvider = new AesCryptoServiceProvider();
            using var cryptoTransform = cryptoProvider.CreateEncryptor(key, iv);
            using var memoryStream = new MemoryStream();
            using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
                using var cryptoStream = new CryptoStream(compressionStream, cryptoTransform, CryptoStreamMode.Write);
                cryptoStream.Write(content, 0, content.Length);
            }
            return memoryStream.ToArray();
        }

        public static byte[] Encrypt(in byte[] content, in byte[] cryptoKey) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (cryptoKey == null) throw new ArgumentNullException(nameof(cryptoKey));
            if (cryptoKey.Length != CryptoKeySize) throw new ArgumentException(nameof(cryptoKey));
            ProcessCryptoKey(cryptoKey, out byte[] iv, out byte[] key);
            return Encrypt(content, iv, key);
        }

        #endregion

        #region Decrypt

        /// <summary>
        /// Decrypts GZIP compressed encrypted AES bytes.
        /// </summary>
        /// <param name="content">Data to decrypt.</param>
        /// <param name="key">Key to use for the decryption.</param>
        /// <returns>Decrypted raw bytes.</returns>
        public static byte[] Decrypt(in byte[] content, in byte[] iv, in byte[] key, in int maxDecompressionSize = DefaultDecompressionSize) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (iv == null) throw new ArgumentNullException(nameof(iv));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (maxDecompressionSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxDecompressionSize);
            using var memoryStream = new MemoryStream(content);
            using var decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var limiterStream = new LimiterStream(decompressionStream, maxDecompressionSize); // limiter stream prevents compression bombs
            using var cryptoServiceProvider = new AesCryptoServiceProvider();
            using var cryptoTransform = cryptoServiceProvider.CreateDecryptor(key, iv);
            using var cryptoStream = new CryptoStream(limiterStream, cryptoTransform, CryptoStreamMode.Read);
            using var decryptedMemoryStream = new MemoryStream();
            cryptoStream.CopyTo(decryptedMemoryStream);
            return decryptedMemoryStream.ToArray();
        }

        public static byte[] Decrypt(in byte[] content, in byte[] cryptoKey, in int maxDecompressionSize = DefaultDecompressionSize) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (cryptoKey == null) throw new ArgumentNullException(nameof(cryptoKey));
            if (maxDecompressionSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxDecompressionSize);
            ProcessCryptoKey(cryptoKey, out byte[] iv, out byte[] key);
            return Decrypt(content, iv, key, maxDecompressionSize);
        }

        #endregion

        #region Obfuscate

        public static byte[] Obfuscate(in byte[] content) => Encrypt(content, AesStandardKey, AesStandardIv);

        #endregion

        #region Deobfuscate

        public static byte[] Deobfuscate(in byte[] content) => Decrypt(content, AesStandardKey, AesStandardIv);

        #endregion

        #region GenerateRandomBytes

        /// <summary>
        /// Generates a random number of bytes.
        /// </summary>
        /// <param name="size">Number of bytes to generate.</param>
        private static byte[] GenerateRandomBytes(in int size) {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            byte[] buffer = new byte[size];
            if (size == 0) return buffer;
            RNG.GetBytes(buffer);
            return buffer;
        }

        #endregion

        #region GenerateCryptoKey

        /// <summary>
        /// Creates a random crypto key.
        /// </summary>
        /// <seealso cref="CryptoKeySize"/>
        public static byte[] GenerateCryptoKey() => GenerateRandomBytes(CryptoKeySize);

        #endregion

        #region ProcessCryptoKey

        /// <summary>
        /// Processes a crypto key and seperates it into an IV and KEY.
        /// </summary>
        /// <param name="cryptoKey">Crypto key containing the IV and KEY.</param>
        /// <param name="iv">IV contained in the crypto key.</param>
        /// <param name="key">KEY contained in the crypto key.</param>
        private static void ProcessCryptoKey(in byte[] cryptoKey, out byte[] iv, out byte[] key) {
            if (cryptoKey == null) throw new ArgumentNullException(nameof(cryptoKey));
            if (cryptoKey.Length != CryptoKeySize) throw new ArgumentException(nameof(cryptoKey));
            #region find xor key
            byte[] xorKey = new byte[XKF0 + XKF1 + XKF2];
            int i = 0;
            int keyIndex = 0;
            Array.Copy(cryptoKey, i, xorKey, keyIndex, XKF0);
            i += XKF0 + AesIvBlockSize; keyIndex += XKF0;
            Array.Copy(cryptoKey, i, xorKey, keyIndex, XKF1);
            i += XKF1 + AesKeyBlockSize; keyIndex += XKF1;
            Array.Copy(cryptoKey, i, xorKey, keyIndex, XKF2);
            #endregion
            iv = new byte[AesIvBlockSize];
            key = new byte[AesKeyBlockSize];
            Array.Copy(cryptoKey, XKF0, iv, 0, AesIvBlockSize);
            Array.Copy(cryptoKey, XKF0 + AesIvBlockSize + XKF1, key, 0, AesKeyBlockSize);
            iv = XOR(iv, xorKey);
            key = XOR(key, xorKey);
        }

        #endregion

        #region XOR

        public static byte[] XOR(in byte[] content, in byte[] key) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (key == null) throw new ArgumentNullException(nameof(key));
            int bufferSize = content.Length;
            byte[] buffer = new byte[bufferSize];
            if (bufferSize == 0) return buffer;
            if (key.Length == 0) {
                Array.Copy(content, buffer, bufferSize);
                return buffer;
            }
            int keyIndex = 0;
            for (int i = 0; i < bufferSize; i++) buffer[i] = (byte)(content[i] ^ key[keyIndex++]);
            return buffer;
        }

        #endregion

        #endregion

    }

}