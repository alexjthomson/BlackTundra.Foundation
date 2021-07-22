using System;

namespace BlackTundra.Foundation.Collections {
    public sealed class BufferException : Exception {
        private BufferException() => throw new InvalidOperationException();
        internal BufferException(in string message, in Exception innerException = null) : base(message, innerException) { }
    }
}