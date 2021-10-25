using System;
using System.Collections.Generic;

namespace BlackTundra.Foundation.Serialization {

    /// <summary>
    /// Responsible for building a <see cref="byte"/> array.
    /// </summary>
    public sealed class SerializedByteArrayBuilder {

        #region constant

        /// <summary>
        /// Null value bytes.
        /// </summary>
        private static readonly byte[] NullBytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        #endregion

        #region variable

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="byte"/> arrays to sitch together later.
        /// </summary>
        private readonly List<byte[]> byteList;

        /// <summary>
        /// Tracks the total number of bytes in the <see cref="byteList"/>.
        /// </summary>
        private int length;

        #endregion

        #region constructor

        public SerializedByteArrayBuilder() {
            byteList = new List<byte[]>();
            length = 0;
        }

        #endregion

        #region logic

        #region WriteNext

        public SerializedByteArrayBuilder WriteNext(in object obj) {
            if (obj == null) {
                byteList.Add(NullBytes); // null byte (0)
                length += 4;
            } else {
                byte[] buffer = obj.SerializeToBytes(); // get the serialized version of the object
                byteList.Add(BitConverter.GetBytes(buffer.Length)); // find the length of the buffer and add it to the data
                byteList.Add(buffer); // add the buffer to the data
                length += 4 + buffer.Length;
            }
            return this;
        }

        #endregion

        #region ToBytes

        public byte[] ToBytes() {
            byte[] buffer = new byte[length], entry;
            int index = 0, entryLength;
            int entryCount = byteList.Count;
            for (int i = 0; i < entryCount; i++) {
                entry = byteList[i];
                entryLength = entry.Length;
                Array.Copy(entry, 0, buffer, index, entryLength);
                index += entryLength;
            }
            return buffer;
        }

        #endregion

        #endregion

    }

}