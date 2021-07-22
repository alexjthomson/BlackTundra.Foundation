using System;

namespace BlackTundra.Foundation.IO {

    public sealed class FileSystemException : Exception {
        internal FileSystemException(in Exception innerException, in string message) : base(message, innerException) { }
        internal FileSystemException(in Exception innerException, in string message, params object[] args) : base(string.Format(message, args), innerException) { }
    }

}