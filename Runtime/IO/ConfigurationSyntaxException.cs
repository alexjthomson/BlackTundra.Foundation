using System;

namespace BlackTundra.Foundation.IO {

    public sealed class ConfigurationSyntaxException : Exception {

        #region variable

        public readonly int index;

        #endregion

        #region constructor

        internal ConfigurationSyntaxException(in string message, in int index, in Exception innerException = null) : base(message, innerException) {

            this.index = index;

        }

        #endregion

    }

}