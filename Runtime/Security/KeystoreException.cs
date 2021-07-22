using System;

namespace BlackTundra.Foundation.Security {

    public sealed class KeystoreException : Exception {
        private KeystoreException() => throw new NotSupportedException();
        internal KeystoreException(in string message, in Exception innerException) : base(message, innerException) { }
    }

}