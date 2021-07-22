using System;
using System.IO;

namespace BlackTundra.Foundation.Utility {

    /// <summary>
    /// Used to limit the number of bytes that can be read from another <see cref="Stream"/>.
    /// </summary>
    public sealed class LimiterStream : Stream {

        #region variable

        private readonly Stream stream;
        private readonly long maxLength;
        private long length;

        #endregion

        #region property

        public long MaxLength => maxLength;

        public sealed override long Length => length;

        public sealed override bool CanRead => stream.CanRead;

        public sealed override bool CanSeek => false;

        public sealed override bool CanWrite => false;

        public sealed override long Position {
            get => stream.Position;
            set => throw new NotSupportedException();
        }

        #endregion

        #region constructor

        public LimiterStream(in Stream stream, in long maxLength) {

            if (stream == null) throw new ArgumentNullException("stream");
            if (maxLength < 0L) throw new ArgumentOutOfRangeException("maxLength");

            this.stream = stream;
            this.maxLength = maxLength;
            length = 0L;

        }

        #endregion

        #region logic

        public sealed override void Flush() => throw new NotSupportedException();

        public sealed override int Read(byte[] buffer, int offset, int count) {
            int bytes = stream.Read(buffer, offset, count);
            length += bytes;
            if (length > maxLength)
                throw new OutOfMemoryException("LimitedStream reached it's memory read limit.");
            return bytes;
        }

        public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public sealed override void SetLength(long value) => throw new NotSupportedException();

        public sealed override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected sealed override void Dispose(bool disposing) {
            stream.Dispose();
            base.Dispose(disposing);
        }

        #endregion

    }

}