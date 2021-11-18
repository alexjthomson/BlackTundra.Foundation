//#define CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION

using System;
using System.Reflection;

namespace BlackTundra.Foundation.IO {

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

        /// <summary>
        /// <see cref="PropertyInfo"/> that this <see cref="ConfigurationEntry"/> is bound to.
        /// </summary>
        internal PropertyInfo property;

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
            this.key = key ?? throw new ArgumentNullException(nameof(key));
            hash = key.GetHashCode();
            this.value = value ?? throw new ArgumentNullException(nameof(value));
            this.dirty = dirty;
            property = null;
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

        #region ToInt

        internal static bool ToInt(in string entry, out int value) {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            try {
                value = int.Parse(entry);
                return true;
            }
#if CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION
            catch (FormatException exception) {
                throw new ConfigurationSyntaxException(
                    $"Failed to parse \"{entry}\" to an int.",
                    -1, exception
                );
            }
#else
            catch (FormatException) {
                value = 0;
                return false;
            }
#endif
        }

        #endregion

        #region ToBool

        internal static bool ToBool(in string entry, out bool value) {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (entry.Equals("true", StringComparison.OrdinalIgnoreCase)) {
                value = true;
                return true;
            }
            if (entry.Equals("false", StringComparison.OrdinalIgnoreCase)) {
                value = false;
                return true;
            }
#if CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION
            throw new ConfigurationSyntaxException(
                $"Failed to parse \"{value}\" to a bool.",
                -1
            );
#else
            value = false;
            return false;
#endif
        }

        #endregion

        #region ToFloat

        internal static bool ToFloat(in string entry, out float value) {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            try {
                value = float.Parse(entry);
                return true;
            }
#if CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION
            catch (FormatException exception) {
                throw new ConfigurationSyntaxException(
                    $"Failed to parse \"{entry}\" to float.",
                    -1, exception
                );
            }
#else
            catch (FormatException) {
                value = 0.0f;
                return false;
            }
#endif
        }

        #endregion

        #region UpdatePropertyValue

        /// <summary>
        /// Updates the value of the <see cref="property"/> with the current <see cref="value"/> of the <see cref="ConfigurationEntry"/>.
        /// </summary>
        internal void UpdatePropertyValue() {
            if (property == null) return;
            Type type = property.PropertyType;
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode) {
                case TypeCode.String: {
                    property.SetValue(null, value);
                    break;
                }
                case TypeCode.Int32: {
                    if (ToInt(value, out int v)) {
                        property.SetValue(null, v);
                    } else {
                        Console.Error($"[{nameof(ConfigurationEntry)}] Failed to update property \"{property.Name}\": Failed to parse value to int.");
                    }
                    break;
                }
                case TypeCode.Boolean: {
                    if (ToBool(value, out bool v)) {
                        property.SetValue(null, v);
                    } else {
                        Console.Error($"[{nameof(ConfigurationEntry)}] Failed to update property \"{property.Name}\": Failed to parse value to bool.");
                    }
                    break;
                }
                case TypeCode.Single: {
                    if (ToFloat(value, out float v)) {
                        property.SetValue(null, v);
                    } else {
                        Console.Error($"[{nameof(ConfigurationEntry)}] Failed to update property \"{property.Name}\": Failed to parse value to float.");
                    }
                    break;
                }
                default: {
                    Console.Error($"[{nameof(ConfigurationEntry)}] Failed to update property \"{property.Name}\": Unsupported property type \"{type.FullName}\".");
                    break;
                }
            }
        }

        #endregion

        #endregion

    }

}