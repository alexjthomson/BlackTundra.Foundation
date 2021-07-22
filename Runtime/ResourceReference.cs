using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.Foundation {

    /// <summary>
    /// References a Unity resource and allows access.
    /// The resource is cached after its been accessed
    /// for the first time.
    /// </summary>
    public sealed class ResourceReference<T> where T : Object {

        #region variable

        /// <summary>
        /// Path of the Unity resource.
        /// </summary>
        public readonly string path;

        /// <summary>
        /// Cached reference to the loaded resource.
        /// </summary>
        private T reference;

        #endregion

        #region property

        /// <summary>
        /// Value of the reference.
        /// </summary>
        public T Value {
            get {
                if (reference == null) reference = Resources.Load<T>(path);
                return reference;
            }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a new <see cref="ResourceReference{T}"/>.
        /// </summary>
        /// <param name="path">Path of the Unity resource.</param>
        public ResourceReference(in string path) {
            this.path = path ?? throw new ArgumentNullException("path");
            reference = null;
        }

        #endregion

    }

}