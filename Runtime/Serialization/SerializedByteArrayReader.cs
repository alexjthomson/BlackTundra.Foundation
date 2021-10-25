using System;

namespace BlackTundra.Foundation.Serialization {

    /// <summary>
    /// Responsible for translating serialized objects that were written to a <see cref="byte"/> array by <see cref="SerializedByteArrayBuilder"/> from
    /// a <see cref="byte"/> array back to the original object.
    /// </summary>
    public sealed class SerializedByteArrayReader {

        #region variable

        /// <summary>
        /// <see cref="byte"/> array to read.
        /// </summary>
        private readonly byte[] bytes;

        /// <summary>
        /// Position through the <see cref="bytes"/>.
        /// </summary>
        private int position;

        #endregion

        #region property

        public int Length => bytes.Length;

        public bool HasNext => position < bytes.Length - 2;

        public int Position => position;

        public byte this[in int index] => bytes[index];

        #endregion

        #region constructor

        public SerializedByteArrayReader(in byte[] bytes) {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            int byteCount = bytes.Length;
            this.bytes = new byte[byteCount];
            Array.Copy(bytes, 0, this.bytes, 0, byteCount);
            position = 0;
        }

        #endregion

        #region ReadNext

        /// <inheritdoc cref="ReadNext{T}(in int)"/>
        public T ReadNext<T>() => ReadNext<T>(ref position, bytes.Length);

        /// <summary>
        /// Reads the next variable.
        /// </summary>
        /// <typeparam name="T">Expected type of the variable.</typeparam>
        /// <param name="position">Position to start reading from. The position will be updated as the data is read.</param>
        /// <param name="lastIndex">Last index (exclusive) that can be read. If the index is exceeded, an <see cref="OutOfMemoryException"/> is thrown.</param>
        /// <returns>Next variable.</returns>
        public T ReadNext<T>(ref int position, in int lastIndex) {
            int nextPosition = position + 4;
            if (nextPosition > lastIndex) throw new IndexOutOfRangeException("position");

            int length = BitConverter.ToInt32(bytes, position);
            position = nextPosition;

            if (length == 0) return default; // null reference

            nextPosition = position + length;
            if (nextPosition > lastIndex) {
                position -= 4; // move back to before the method was called
                throw new IndexOutOfRangeException("position");
            }

            byte[] buffer = new byte[length];
            Array.Copy(bytes, position, buffer, 0, length);
            position = nextPosition;

            return ObjectSerializer.ToObject<T>(buffer);
        }

        #endregion

        #region SkipNext

        /// <summary>
        /// Skips the next variable without reading it.
        /// </summary>
        /// <typeparam name="T">Expected type of the variable.</typeparam>
        public void SkipNext<T>() {
            int lastIndex = bytes.Length;
            int nextPosition = position + 4;
            if (nextPosition > lastIndex) throw new IndexOutOfRangeException("position");
            int length = BitConverter.ToInt32(bytes, position);
            position = nextPosition;
            if (length == 0) return;
            nextPosition = position + length;
            if (nextPosition > lastIndex) {
                position -= 4; // move back to before the method was called
                throw new IndexOutOfRangeException("position");
            }
            position = nextPosition;
        }

        #endregion

        #region ResetPosition

        public int ResetPosition() {
            int lastPosition = position;
            position = 0;
            return lastPosition;
        }

        #endregion

        #region ToBytes

        public byte[] ToBytes() {
            int byteCount = bytes.Length;
            byte[] buffer = new byte[byteCount];
            Array.Copy(bytes, 0, buffer, 0, byteCount);
            return buffer;
        }

        #endregion

    }

}