using System;

namespace BlackTundra.Foundation.Collections {

    /// <summary>
    /// Links a string key with a string value that is saved in a configuration file.
    /// </summary>
    public sealed class ConfigurationEntry : IEquatable<ConfigurationEntry>, IEquatable<string> {

        #region variable

        /// <summary>
        /// String key of the configuration entry.
        /// </summary>
        internal readonly string key;

        /// <summary>
        /// Cached hash code used to identify the entry quickly.
        /// </summary>
        internal readonly int hash;

        /// <summary>
        /// Value of the entry.
        /// </summary>
        internal string value;

        /// <summary>
        /// Is the entry dirty.
        /// </summary>
        internal bool dirty;

        #endregion

        #region property

        /// <summary>
        /// Gets the key associated with the entry.
        /// </summary>
        public string Key => key;

        /// <summary>
        /// Gets the value associated with the entry.
        /// </summary>
        public string Value => value;

        /// <summary>
        /// Indicates if the entry is dirty or not.
        /// </summary>
        public bool IsDirty => dirty;

        #endregion

        #region constructor

        internal ConfigurationEntry(in string key, in string value, in bool dirty) {

            this.key = key ?? throw new ArgumentNullException("key");
            hash = key.GetHashCode();

            this.value = value ?? throw new ArgumentNullException("value");
            this.dirty = dirty;

        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(ConfigurationEntry entry) => entry != null && ((value == null && entry.value == null) || value.Equals(entry.value));

        public bool Equals(string value) => (this.value == null && value == null) || this.value.Equals(value);

        #endregion

        #region ToString

        public sealed override string ToString() => $"{key}: {value}";

        #endregion

        #endregion

    }

}