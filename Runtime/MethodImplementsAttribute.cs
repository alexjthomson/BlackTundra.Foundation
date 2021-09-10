using System;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Enforces a possible set of arguments that a method decorated with the attribute this
    /// attribute is used to decorate may have.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class MethodImplementsAttribute : Attribute {

        #region variable

        /// <summary>
        /// Signature to check.
        /// </summary>
        internal readonly Type[] signature;

        #endregion

        #region constructor

        /// <param name="signature">
        /// Possible signature of a method decorated with the attribute this <see cref="MethodImplementsAttribute"/>
        /// is decorating.
        /// </param>
        public MethodImplementsAttribute(params Type[] signature) {
            this.signature = signature ?? new Type[0];
        }

        #endregion

    }

}