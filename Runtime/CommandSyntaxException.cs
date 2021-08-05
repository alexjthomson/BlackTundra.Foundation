using System;

namespace BlackTundra.Foundation {

    public sealed class CommandSyntaxException : Exception {
        internal CommandSyntaxException(in string message, in Exception innerException = null) : base(message, innerException) { }
    }

}
