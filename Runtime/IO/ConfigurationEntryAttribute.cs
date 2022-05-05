using System;

namespace BlackTundra.Foundation.IO {

    /// <summary>
    /// Indicates that a static property has a corresponding configuration entry associated with it.
    /// When the configuration value is updated, the attribute value will also be updated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ConfigurationEntryAttribute : Attribute {

        #region variable

        /// <summary>
        /// Configuration file that the <see cref="ConfigurationEntryAttribute"/> belongs to.
        /// </summary>
        public readonly string configuration;

        /// <summary>
        /// Configuration key in the <see cref="configuration"/> file that this <see cref="ConfigurationEntryAttribute"/> is bound to.
        /// </summary>
        public readonly string key;

        /// <summary>
        /// Default value of the <see cref="ConfigurationEntryAttribute"/>.
        /// </summary>
        internal readonly string defaultValue;

        #endregion

        #region constructor

        public ConfigurationEntryAttribute(string configuration, string key, string defaultValue) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.configuration = configuration;
            this.key = key;
            this.defaultValue = defaultValue;
        }

        public ConfigurationEntryAttribute(string configuration, string key, int defaultValue) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.configuration = configuration;
            this.key = key;
            this.defaultValue = defaultValue.ToString();
        }

        public ConfigurationEntryAttribute(string configuration, string key, float defaultValue) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.configuration = configuration;
            this.key = key;
            this.defaultValue = defaultValue.ToString();
        }

        public ConfigurationEntryAttribute(string configuration, string key, bool defaultValue) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (key == null) throw new ArgumentNullException(nameof(key));
            this.configuration = configuration;
            this.key = key;
            this.defaultValue = defaultValue ? "true" : "false";
        }

        #endregion

    }

}