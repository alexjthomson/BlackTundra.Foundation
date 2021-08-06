using System;

namespace BlackTundra.Foundation.Utility {

    /// <summary>
    /// Marks a type as a serializable implementation of another type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class SerializableImplementationOfAttribute : Attribute {

        /// <summary>
        /// Type that the object decorated with this attribute will replace during object serialization/deserialization.
        /// </summary>
        public readonly Type target;

        /// <param name="target">
        /// Type that the object decorated with this attribute will replace during object serialization/deserialization.
        /// </param>
        /// <seealso cref="ObjectUtility.SerializeToBytes(object, in bool)"/>
        /// <seealso cref="ObjectUtility.ToObject{T}(in byte[])"/>
        public SerializableImplementationOfAttribute(Type target) {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

    }

}