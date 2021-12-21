//#define CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION

using System;
using System.Reflection;

namespace BlackTundra.Foundation.IO {

    /// <summary>
    /// Links a string key with a string value that is saved in a configuration file.
    /// </summary>
    public sealed class ConfigurationEntry : IEquatable<ConfigurationEntry>, IEquatable<string> {

        #region constant

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(ConfigurationEntry));

        #endregion

        #region variable

        /// <summary>
        /// String key of the configuration entry.
        /// </summary>
        internal readonly string _key;

        /// <summary>
        /// Cached hash code used to identify the entry quickly.
        /// </summary>
        internal readonly int hash;

        /// <summary>
        /// Value of the entry.
        /// </summary>
        private string _value;

        /// <summary>
        /// Is the entry dirty.
        /// </summary>
        internal bool _dirty;

        /// <summary>
        /// <see cref="PropertyInfo"/> that this <see cref="ConfigurationEntry"/> is bound to.
        /// </summary>
        private PropertyInfo _property;

        #endregion

        #region property

        /// <summary>
        /// Gets the key associated with the entry.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public string key => _key;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// Gets the value associated with the entry.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public string value {
#pragma warning restore IDE1006 // naming styles
            get {
                if (_property != null) {
                    object propertyValue = _property.GetValue(null);
                    TypeCode typeCode = Type.GetTypeCode(_property.PropertyType);
                    return typeCode switch {
                        TypeCode.String => propertyValue != null ? propertyValue.ToString() : string.Empty,
                        TypeCode.Int32 or TypeCode.Single => propertyValue != null ? propertyValue.ToString() : "0",
                        TypeCode.Boolean => propertyValue != null && propertyValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false",
                        _ => string.Empty,
                    };
                } else return _value;
            }
            set {
                if (value == null) throw new ArgumentNullException();
                if (_property != null) SetPropertyValue(value);
                _value = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public PropertyInfo property {
#pragma warning restore IDE1006 // naming styles
            get => _property;
            internal set {
                if (value == null) throw new ArgumentNullException();
                _property = value;
            }
        }

        /// <summary>
        /// Indicates if the entry is dirty or not.
        /// </summary>
        public bool IsDirty => _dirty;

        #endregion

        #region constructor

        internal ConfigurationEntry(in string key, in string value, in bool dirty) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));
            _key = key;
            hash = key.GetHashCode();
            _value = value;
            _dirty = dirty;
            _property = null;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(ConfigurationEntry entry) => entry != null && ((_value == null && entry._value == null) || _value.Equals(entry._value));

        public bool Equals(string value) => (this._value == null && value == null) || this._value.Equals(value);

        #endregion

        #region ToString

        public sealed override string ToString() => $"{_key}: {_value}";

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
        /// Updates the value of the <see cref="_property"/> with the current <see cref="_value"/> of the <see cref="ConfigurationEntry"/>.
        /// </summary>
        internal void UpdatePropertyValue() {
            if (_property == null) return;
            SetPropertyValue(_value);
        }

        #endregion

        #region SetPropertyValue

        private void SetPropertyValue(in string value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Type type = _property.PropertyType;
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode) {
                case TypeCode.String: {
                    _property.SetValue(null, value);
                    break;
                }
                case TypeCode.Int32: {
                    if (ToInt(value, out int v)) {
                        _property.SetValue(null, v);
                    } else {
                        ConsoleFormatter.Error($"Failed to update property `{_property.Name}`: Failed to parse value to int.");
                    }
                    break;
                }
                case TypeCode.Boolean: {
                    if (ToBool(value, out bool v)) {
                        _property.SetValue(null, v);
                    } else {
                        ConsoleFormatter.Error($"Failed to update property `{_property.Name}`: Failed to parse value to bool.");
                    }
                    break;
                }
                case TypeCode.Single: {
                    if (ToFloat(value, out float v)) {
                        _property.SetValue(null, v);
                    } else {
                        ConsoleFormatter.Error($"Failed to update property `{_property.Name}`: Failed to parse value to float.");
                    }
                    break;
                }
                default: {
                    ConsoleFormatter.Error($"Failed to update property `{_property.Name}`: Unsupported property type \"{type.FullName}\".");
                    break;
                }
            }
        }

        #endregion

        #endregion

    }

}