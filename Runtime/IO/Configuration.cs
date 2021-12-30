using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Colour = BlackTundra.Foundation.ConsoleColour;

namespace BlackTundra.Foundation.IO {

    public sealed class Configuration {

        #region nested

        #region ConfigurationSortKeyDescriptor

        /// <summary>
        /// Used while sorting configuration entries. This describes a configuration entry key and the index in the
        /// <see cref="buffer"/> that the descriptor is reffering to.
        /// </summary>
        private struct ConfigurationSortKeyDescriptor {

            #region variable

            /// <summary>
            /// Index in the <see cref="buffer"/> of the original <see cref="ConfigurationEntry"/> that this is
            /// describing the key of.
            /// </summary>
            internal readonly int index;

            /// <summary>
            /// Key of the <see cref="ConfigurationEntry"/> being described.
            /// </summary>
            internal readonly string key;

            #endregion

            #region constructor

            internal ConfigurationSortKeyDescriptor(in int index, in string key) {
                this.index = index;
                this.key = key;
            }

            #endregion

        }

        #endregion

        #endregion

        #region constant

        /// <summary>
        /// <see cref="Dictionary{TKey, TValue}"/> containing a reference to every <see cref="Configuration"/> entry.
        /// </summary>
        private static readonly Dictionary<int, Configuration> ConfigurationDictionary = new Dictionary<int, Configuration>();

        private const int DefaultConfigurationBufferCapacity = 32;
        private const int DefaultConfigurationBufferExpandSize = 8;

        /// <summary>
        /// String used at the end of a configuration statement when converting <see cref="Configuration"/> to a string.
        /// </summary>
        private readonly static string EndOfStatement = ';' + Environment.NewLine;

        #endregion

        #region variable

        /// <summary>
        /// Name of the <see cref="Configuration"/>.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Hash of the <see cref="name"/>.
        /// </summary>
        private readonly int nameHash;

        /// <summary>
        /// <see cref="FileSystemReference"/> used for saving / loading.
        /// </summary>
        internal readonly FileSystemReference fsr;

        /// <summary>
        /// <see cref="FileFormat"/> used to read/write the <see cref="Configuration"/> to the <see cref="FileSystem"/>.
        /// </summary>
        internal readonly FileFormat format;

        /// <summary>
        /// Buffer where all configuration entries are stored.
        /// </summary>
        private readonly PackedBuffer<ConfigurationEntry> buffer;

        /// <summary>
        /// Size that the buffer should expand by when there is not enough room for more entries.
        /// </summary>
        private readonly int expandSize;

        /// <inheritdoc cref="IsDirty"/>
        private bool dirty;

        #endregion

        #region property

        /// <summary>
        /// Gets a configuration entry by the key.
        /// </summary>
        /// <param name="key">Key to search by.</param>
        /// <param name="dirty">When true, the configuration entry will be set to dirty.</param>
        /// <returns>Configuration entry or null if no entry was found.</returns>
        public string this[in string key, in bool dirty = true] {
            get {
                if (key == null) throw new ArgumentNullException(nameof(key));
                int hash = key.GetHashCode();
                ConfigurationEntry entry;
                for (int i = buffer.Count - 1; i >= 0; i--) {
                    entry = buffer[i];
                    if (entry.hash == hash) {
                        if (!dirty) entry._dirty = false; // set as not dirty
                        return entry.value;
                    }
                }
                return null;
            }
            set {
                if (key == null) throw new ArgumentNullException(nameof(key));
                int hash = key.GetHashCode();
                ConfigurationEntry entry;
                for (int i = buffer.Count - 1; i >= 0; i--) {
                    entry = buffer[i];
                    if (entry.hash == hash) {
                        if (entry.value != value) { // value is different
                            entry.value = value; // override value
                            if (dirty) {
                                entry._dirty = true;
                                this.dirty = true;
                            } else
                                entry._dirty = false;
                        }
                        return;
                    }
                }
                AddEntry(key, value, dirty);
            }
        }

        /// <summary>
        /// Number of entries in the configuration.
        /// </summary>
        public int Length => buffer.Count;

        /// <summary>
        /// Gets a configuration entry by index.
        /// </summary>
        /// <param name="index">Index of the entry.</param>
        /// <returns>Value of the configuration entry at this index.</returns>
        public ConfigurationEntry this[in int index] {
            get {
                if (index < 0 || index >= buffer.Count) throw new ArgumentOutOfRangeException(nameof(index));
                return buffer[index];
            }
        }

        /// <summary>
        /// Tracks if the <see cref="Configuration"/> is modified or not.
        /// </summary>
        public bool IsDirty => dirty;

        #endregion

        #region constructor

        private Configuration() => throw new NotSupportedException();

        private Configuration(in string name, in FileFormat format, in int capacity, in int expandSize) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (capacity < 1) throw new ArgumentOutOfRangeException(string.Concat(nameof(capacity), " must be greater than zero."));
            if (expandSize < 1) throw new ArgumentOutOfRangeException(string.Concat(nameof(expandSize), " must be greater than zero."));
            nameHash = name.GetHashCode();
            if (ConfigurationDictionary.ContainsKey(nameHash)) throw new ArgumentException($"{nameof(name)}: {nameof(Configuration)} \"{name}\" already exists.");
            this.name = name;
            fsr = new FileSystemReference(
                string.Concat(
                    FileSystem.LocalConfigDirectory,
                    name,
                    FileSystem.ConfigExtension
                ),
                true, // is local
                false // is not a directory
            );
            this.format = format;
            buffer = new PackedBuffer<ConfigurationEntry>(capacity);
            this.expandSize = expandSize;
            dirty = false;
            ConfigurationDictionary.Add(nameHash, this);
        }

        private Configuration(in FileSystemReference fsr, in FileFormat format, in int capacity, in int expandSize) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (capacity < 1) throw new ArgumentOutOfRangeException(string.Concat(nameof(capacity), " must be greater than zero."));
            if (expandSize < 1) throw new ArgumentOutOfRangeException(string.Concat(nameof(expandSize), " must be greater than zero."));
            name = fsr.FileNameWithoutExtension;
            nameHash = name.GetHashCode();
            if (ConfigurationDictionary.ContainsKey(nameHash)) throw new ArgumentException($"{nameof(name)}: {nameof(Configuration)} \"{name}\" already exists.");
            this.fsr = fsr;
            this.format = format;
            buffer = new PackedBuffer<ConfigurationEntry>(capacity);
            this.expandSize = expandSize;
            dirty = false;
            ConfigurationDictionary.Add(nameHash, this);
        }

        #endregion

        #region destructor

        ~Configuration() {
            ConfigurationDictionary.Remove(nameHash);
        }

        #endregion

        #region logic

        #region BindAttributesToConfiguration

        [CoreInitialise(int.MinValue)]
        private static void BindAttributesToConfiguration() {
            Type context = typeof(ConfigurationEntryAttribute); // get attribute to search for
            IEnumerable<PropertyInfo> properties = ObjectUtility.GetDecoratedProperties<ConfigurationEntryAttribute>(); // get every property that implements the attribute
            ConfigurationEntryAttribute attribute; ConfigurationEntry entry; // temporary references
            foreach (PropertyInfo property in properties) { // iterate each property
                attribute = (ConfigurationEntryAttribute)property.GetCustomAttribute(context, false); // get the attribute
                int configurationHash = attribute.configuration.GetHashCode(); // find the hash of the configuration name
                if (!ConfigurationDictionary.TryGetValue(configurationHash, out Configuration configuration)) { // check if the configuration entry exists yet
                    configuration = new Configuration(attribute.configuration, FileFormat.Standard, DefaultConfigurationBufferCapacity, DefaultConfigurationBufferExpandSize); // create the configuration entry
                    configuration.Load(); // load any existing values
                }
                int index = configuration.IndexOf(attribute.key); // get the position of the target configuration key that the property aims to bind to
                if (index == -1) { // the key does not exist, therefore it should be created
                    entry = configuration.AddEntry(attribute.key, attribute.defaultValue, true); // create a new key with the default value assigned by the property
                } else { // the key exists, find the entry
                    entry = configuration.buffer[index]; // get the key
                }
                entry.property = property; // assign the property to the entry
                entry.UpdatePropertyValue(); // update the value of the property based on the value of the entry
            }
        }

        #endregion

        #region SaveConfiguration

        [CoreInitialise(int.MaxValue)]
        [CoreTerminate]
        private static void SaveConfiguration() {
            foreach (Configuration configuration in ConfigurationDictionary.Values) {
                if (configuration.dirty) configuration.Save();
            }
        }

        #endregion

        #region IndexOf

        private int IndexOf(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return IndexOf(key.GetHashCode());
        }

        private int IndexOf(in int hash) {
            ConfigurationEntry entry;
            for (int i = buffer.Count - 1; i >= 0; i--) {
                entry = buffer[i];
                if (entry.hash == hash) return i;
            }
            return -1;
        }

        #endregion

        #region Parse

        /// <summary>
        /// Parses configuration into a Configuration instance. This will result in all parameters being marked as not dirty.
        /// </summary>
        /// <param name="source">Source configuration.</param>
        /// <param name="rewrite">When <c>true</c>, the source configuration will be written to, updating it with existing values in the configuration instance.</param>
        /// <param name="overrideExisting">When <c>true</c>, existing values in the configuration file will be overridden if a value with the same key is found in the source configuration.</param>
        /// <param name="overrideIsDirty">When <c>true</c> and <paramref name="rewrite"/> is <c>true</c>, entries written to the <see cref="StringBuilder"/> will be marked as not dirty.</param>
        /// <returns>Returns a reference to the StringBuilder used to rewrite the source configuration. If the rewrite argument is false, null will be returned.</returns>
        public StringBuilder Parse(in string source, in bool rewrite = false, in bool overrideExisting = false, in bool overrideIsDirty = true) {

            StringBuilder builder = rewrite ? new StringBuilder(source.Length) : null; // StringBuilder used to rewrite the source configuration

            if (source != null && source.Length > 0) { // source configuration was included

                bool escape = false, // tracks if the next character should be escaped
                     whitespace = true, // tracks if the previous characters have been whitespace
                     noKey = true; // tracks if the key has been found or not

                int keyStart = -1, keyEnd = -1, valueStart = -1, valueEnd = -1; // track the positions of the key and value

                char c;
                for (int i = 0; i < source.Length; i++) {

                    c = source[i];

                    if (noKey) { // no key

                        /*
                         * A key should follow the following syntax:
                         * "   i.am.a.key = ".
                         * Whitespace is allowed before and after a key, but not as part of the key. A key
                         * should also end with an '=' character, to denote that the next set of characters
                         * should be processed as a value to a key.
                         */

                        if (char.IsWhiteSpace(c)) whitespace = true;
                        else if (c == '=') {

                            whitespace = true;
                            noKey = false;

                            if (rewrite) { // append characters between last value and current start of next value

                                int length = keyStart - valueEnd - 1;
                                if (length > 0) builder.Append(source.Substring(valueEnd + 1, length));

                            }

                        } else if (char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-') { // legal key character

                            // find new key bounds:

                            if (keyStart == -1) { // key hasn't started yet

                                keyStart = i;
                                whitespace = false;

                            } else if (whitespace) throw new ConfigurationSyntaxException(
                                "Illegal whitespace character found in key; a key cannot contain whitespace.", i
                            );

                            keyEnd = i;

                        } else if (c == '#') { // comment

                            whitespace = true;
                            for (++i; i < source.Length; i++) if (source[i] == '\n') break; // end of comment found

                        } else throw new ConfigurationSyntaxException( // invalid character
                            $"Illegal character (c: '{c}', id: {(int)c}, index: {i}, keyIndex: {(keyStart == -1 ? "unknown" : (i - keyStart).ToString())}) found in key.", i
                        );

                    } else { // reading value

                        /*
                         * A value should follow the following syntax:
                         * "  I'm a value, I can include special characters if I use the escape character: \\. ;".
                         * Any pre/post whitespace will be stripped from the value unless it is escaped using
                         * the '\' character, which has been defined as the escape character in a value.
                         */

                        if (escape) escape = false;
                        else if (c == '\\') escape = true;
                        else if (c == ';') { // end of statement
                                             // find key:
                            int length = keyEnd - keyStart + 1;
                            if (length < 1) throw new ConfigurationSyntaxException("Empty key found", keyStart);
                            string key = source.Substring(keyStart, length);

                            string value;
                            if (valueStart != -1) { // when the value start is -1, it means there is an equals but no value supplied afterwards
                                // find value:
                                length = valueEnd - valueStart + 1;
                                value = length <= 0 ? string.Empty : source.Substring(valueStart, length);
                            } else { // there is no value, simply default to an empty string
                                value = string.Empty;
                            }

                            // override value:
                            if (overrideExisting) { // any existing value in the configuration should be overridden
                                this[key, false] = value;
                            } else if (rewrite) { // the source should be overridden with the value inside the configuration (if it exists)
                                string existingValue = this[key, false];
                                builder.Append(key);
                                if (valueStart != -1)
                                    builder.Append(source.Substring(keyEnd + 1, valueStart - keyEnd - 1)); // add the characters between the key and value
                                else
                                    builder.Append(" = "); // manually construct section between key and value since it was malformed in the source
                                builder.Append(existingValue ?? value);
                            }
                            // reset:
                            keyStart = -1;
                            keyEnd = -1;
                            valueStart = -1;
                            //valueEnd = -1; // not required to be reset
                            noKey = true;
                        } else if (c == '#') { // comment

                            whitespace = true;
                            for (++i; i < source.Length; i++) if (source[i] == '\n') break; // found end of line

                        } else if (!char.IsWhiteSpace(c)) { // not whitespace

                            if (valueStart == -1) valueStart = i; // assign the start of the value
                            valueEnd = i; // move the valueEnd accordingly

                        }

                    }

                }

                if (rewrite) { // write the final part of the source to the builder

                    builder.Append(source.Substring(valueEnd + 1, source.Length - valueEnd - 1));
                    if (source[source.Length - 1] != '\n') builder.Append(Environment.NewLine); // ensure the configuration file ends in a new line

                }

            }

            if (rewrite) { // write the remaining values to the source
                ConfigurationEntry entry;
                for (int i = 0; i < buffer.Count; i++) { // iterate through values in buffer
                    entry = buffer[i];
                    if (entry._dirty) { // value is dirty
                        builder.Append(entry._key);
                        builder.Append(" = ");
                        builder.Append(entry.value);
                        builder.Append(EndOfStatement);
                        if (overrideIsDirty) entry._dirty = false; // no longer dirty
                    }
                }
                if (overrideIsDirty) dirty = false;
            }

            return builder; // return the builder

        }

        #endregion

        #region Get

        /// <summary>
        /// Gets the entry associated with the <paramref name="key"/> and casts to type <typeparamref name="T"/>.
        /// </summary>
        public object Get<T>(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) throw new KeyNotFoundException(key);
            TypeCode type = Type.GetTypeCode(typeof(T));
            return type switch {
                TypeCode.String => entry,
                TypeCode.Int32 => ConfigurationEntry.ToInt(entry, out int i) ? i : 0,
                TypeCode.Boolean => ConfigurationEntry.ToBool(entry, out bool b) ? b : false,
                TypeCode.Single => ConfigurationEntry.ToFloat(entry, out float f) ? f : 0.0f,
                _ => throw new NotSupportedException(type.ToString()),
            };
        }

        #endregion

        #region GetString

        public string GetString(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) throw new KeyNotFoundException(key);
            return entry;
        }

        #endregion

        #region GetInt

        public int GetInt(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) throw new KeyNotFoundException(key);
            return ConfigurationEntry.ToInt(entry, out int value) ? value : 0;
        }

        #endregion

        #region GetBool

        public bool GetBool(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) throw new KeyNotFoundException(key);
            return ConfigurationEntry.ToBool(entry, out bool value) ? value : false;
        }

        #endregion

        #region GetFloat

        public float GetFloat(in string key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) throw new KeyNotFoundException(key);
            return ConfigurationEntry.ToFloat(entry, out float value) ? value : 0.0f;
        }

        #endregion

        #region ForceGet

        /// <summary>
        /// Force gets an entry from the configuration.
        /// <para>
        /// If an entry already exists in the configuration, it will be returned provided it is parsed correctly.
        /// </para>
        /// </summary>
        /// <param name="key">Key associated with the value trying to be accessed.</param>
        /// <param name="fallback">Fallback value to return if no valid value is found in the <see cref="Configuration"/>.</param>
        public string ForceGet(in string key, in string fallback) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback, true);
                return fallback;
            }
            return entry;
        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public int ForceGet(in string key, in int fallback) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback.ToString(), true);
                return fallback;
            }
            if (ConfigurationEntry.ToInt(entry, out int value)) return value;
            else {
                this[key] = fallback.ToString();
                return fallback;
            }
        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public bool ForceGet(in string key, in bool fallback) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback ? "true" : "false", true);
                return fallback;
            }
            if (ConfigurationEntry.ToBool(entry, out bool value)) return value;
            else {
                this[key] = fallback ? "true" : "false";
                return fallback;
            }
        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public float ForceGet(in string key, in float fallback) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback.ToString(), true);
                return fallback;
            }
            if (ConfigurationEntry.ToFloat(entry, out float value)) return value;
            else {
                this[key] = fallback.ToString();
                return fallback;
            }
        }

        #endregion

        #region AddEntry

        private ConfigurationEntry AddEntry(in string key, in string value, in bool dirty) {
            if (buffer.Count == buffer.Capacity) buffer.Expand(expandSize); // expand the buffer
            ConfigurationEntry entry = new ConfigurationEntry(key, value, dirty);
            buffer.AddLast(entry); // add the new configuration entry to the buffer
            if (dirty) this.dirty = true; // mark as dirty
            return entry;
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the configuration of all entries.
        /// </summary>
        public void Clear() {
            buffer.Clear();
            dirty = true;
        }

        #endregion

        #region GetConfiguration

        /// <summary>
        /// Gets <see cref="Configuration"/> by <paramref name="name"/>.
        /// </summary>
        /// <remarks>
        /// If no <see cref="Configuration"/> is found, a new instance is created. No <see cref="FileSystem"/> operations are performed.
        /// </remarks>
        public static Configuration GetConfiguration(in string name, in FileFormat format = FileFormat.Standard) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            int hash = name.GetHashCode();
            if (!ConfigurationDictionary.TryGetValue(hash, out Configuration configuration)) {
                configuration = new Configuration(name, format, DefaultConfigurationBufferCapacity, DefaultConfigurationBufferExpandSize);
            }
            return configuration;
        }

        /// <summary>
        /// Gets <see cref="Configuration"/> by <see cref="FileSystemReference"/>.
        /// </summary>
        /// <remarks>
        /// If no <see cref="Configuration"/> is found, one is loaded from the system.
        /// </remarks>
        public static Configuration GetConfiguration(in FileSystemReference fsr, in FileFormat format = FileFormat.Standard) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            foreach (Configuration entry in ConfigurationDictionary.Values) {
                if (fsr.Equals(entry.fsr)) {
                    return entry;
                }
            }
            Configuration configuration = new Configuration(fsr, format, DefaultConfigurationBufferCapacity, DefaultConfigurationBufferExpandSize);
            return FileSystem.LoadConfiguration(configuration);
        }

        #endregion

        #region Save

        /// <summary>
        /// Force saves the <see cref="Configuration"/>.
        /// </summary>
        public bool Save() {
            Sort();
            bool success = FileSystem.UpdateConfiguration(this);
            dirty = false;
            return success;
        }

        #endregion

        #region Load

        /// <summary>
        /// Reloads the <see cref="Configuration"/> using the values stored on the system rather than in memory.
        /// </summary>
        public void Load() {
            FileSystem.LoadConfiguration(this);
            RefreshProperties();
        }

        #endregion

        #region Sort

        /// <summary>
        /// Sorts the <see cref="buffer"/> <see cref="ConfigurationEntry"/> instances alphabetically by key.
        /// </summary>
        private void Sort() {
            // split keys by seperator and find depth:
            int count = buffer.Count;
            ConfigurationSortKeyDescriptor[] keys = new ConfigurationSortKeyDescriptor[count];
            ConfigurationEntry entry;
            for (int i = count - 1; i >= 0; i--) {
                entry = buffer[i];
                keys[i] = new ConfigurationSortKeyDescriptor(i, entry._key);
            }
            // sort:
            keys.Sort((x, y) => string.Compare(x.key, y.key, StringComparison.OrdinalIgnoreCase));
            // finalize:
            int[] map = new int[count];
            for (int i = count - 1; i >= 0; i--) {
                map[i] = keys[i].index;
            }
            buffer.Remap(map);
        }

        #endregion

        #region RefreshProperties

        /// <summary>
        /// Reassigns the values of every property bound to the specified <paramref name="configurationName"/>.
        /// </summary>
        public static void RefreshProperties(in string configurationName) {
            if (configurationName == null) throw new ArgumentNullException(configurationName);
            int hash = configurationName.GetHashCode();
            RefreshProperties(hash);
        }

        /// <summary>
        /// Reassigns the values of every property bound to the specified configuration name <paramref name="hash"/> code.
        /// </summary>
        public static void RefreshProperties(in int hash) {
            if (ConfigurationDictionary.TryGetValue(hash, out Configuration configuration)) {
                configuration.RefreshProperties();
            }
        }

        /// <summary>
        /// Reassigns the values of every property bound to this <see cref="Configuration"/>.
        /// </summary>
        public void RefreshProperties() {
            for (int i = buffer.Count - 1; i >= 0; i--) buffer[i].UpdatePropertyValue();
        }

        #endregion

        #region ConfigCommand

        [Command(
            name: "config",
            description: "Provides the ability to modify configuration entry values through the console.",
            usage:
            "config" +
                "\n\tDisplays a list of every cached configuration instance." +
                "\nconfig {name}" +
                "\n\tDisplays the contents of a cached configuration instance." +
                "\n\tname: Name of the configuration instance." +
            "\nconfig {name} {key}" +
                "\n\tDisplays the value of a specific key in a cached configuration instance." +
                "\n\tname: Name of the configuration instance." +
                "\n\tkey: Name of the entry in the configuration instance." +
            "\nconfig {file} {key} {value}" +
                "\n\tOverrides and commits a change to the value of a key in a cached configuration instance." +
                "\n\tname: Name of the configuration instance." +
                "\n\tkey: Name of the entry in the configuration instance." +
                "\n\tvalue: New value to assign to the entry.",
            hidden: false
        )]
        private static bool ConfigCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                string[,] table = new string[4, ConfigurationDictionary.Count + 1];
                table[0, 0] = "Name"; table[1, 0] = "FSR"; table[2, 0] = "Format"; table[3, 0] = "Sync State";
                int index = 1;
                foreach (Configuration configuration in ConfigurationDictionary.Values) {
#if UNITY_EDITOR
                    table[0, index] = configuration.name;
                    table[1, index] = configuration.fsr.ToString();
                    table[2, index] = configuration.format.ToString();
#else
                    if (configuration.format == FileFormat.Standard) {
                        table[0, index] = configuration.name;
                        table[1, index] = configuration.fsr.ToString();
                        table[2, index] = "Unlocked";
                    } else {
                        table[0, index] = configuration.nameHash.ToHex();
                        table[1, index] = "N/A";
                        table[2, index] = "Locked";
                    }
#endif
                    table[3, index] = configuration.dirty ? "Pending" : "Pushed";
                    index++;
                }
                ConsoleWindow.PrintTable(table, true);
            } else { // a config file was specified
                string configurationName = info.args[0];
                if (!ConfigurationDictionary.TryGetValue(configurationName.GetHashCode(), out Configuration configuration) // try get configuration
#if !UNITY_EDITOR
                    || configuration.format != FileFormat.Standard // ensure only standard formatted configuration can be modified via the config command
#endif
                ) {
                    ConsoleWindow.Print($"No configuration entry found for \"{ConsoleUtility.Escape(configurationName)}\".");
                    return false;
                } else { // configuration found
                    string[,] table = new string[4, 2];
                    table[0, 0] = "Name"; table[1, 0] = "FSR"; table[2, 0] = "Format"; table[3, 0] = "Sync State";
#if UNITY_EDITOR
                    table[0, 1] = configuration.name;
                    table[1, 1] = configuration.fsr.ToString();
                    table[2, 1] = configuration.format.ToString();
#else
                    if (configuration.format == FileFormat.Standard) {
                        table[0, 1] = configuration.name;
                        table[1, 1] = configuration.fsr.ToString();
                        table[2, 1] = "Unlocked";
                    } else {
                        table[0, 1] = configuration.nameHash.ToHex();
                        table[1, 1] = "N/A";
                        table[2, 1] = "Locked";
                    }
#endif
                    table[3, 1] = configuration.dirty ? "Pending" : "Pushed";
                    ConsoleWindow.PrintTable(table, true);
                    if (argumentCount == 1) { // no further arguments; therefore, display every configuration entry to the console
                        ConsoleWindow.Print(string.Empty);
                        #region list config entries
                        int entryCount = configuration.Length;
                        table = new string[3, entryCount + 1];
                        table[0, 0] = "Key Hash"; table[1, 0] = "Key"; table[2, 0] = "Value";
                        ConfigurationEntry entry;
                        for (int i = 0; i < entryCount;) {
                            entry = configuration[i++];
                            table[0, i] = $"<color=#{Colour.Red.hex}>{StringUtility.ToHex(entry.hash)}</color>";
                            table[1, i] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(entry._key)}</color>";
                            table[2, i] = ConsoleUtility.Escape(entry.value);
                        }
                        ConsoleWindow.PrintTable(table, true);
                        #endregion
                    } else { // an additional argument, this sepecifies an entry to target
                        string key = info.args[1]; // get the entry to edit
                        int index = configuration.IndexOf(key);
                        if (index == -1) {
                            ConsoleWindow.Print($"<b><color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(key)}</color></b>: Key not found.");
                            return false;
                        }
                        ConfigurationEntry entry = configuration[index]; // get configuration entry
                        if (argumentCount == 2) { // no further arguments; therefore, display the value of the target entry
                            ConsoleWindow.Print(string.Empty);
                            table = new string[3, 2];
                            table[0, 0] = "Key Hash"; table[1, 0] = "Key"; table[2, 0] = "Value";
                            table[0, 1] = $"<color=#{Colour.Red.hex}>{StringUtility.ToHex(entry.hash)}</color>";
                            table[1, 1] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(entry._key)}</color>";
                            table[2, 1] = ConsoleUtility.Escape(entry.value);
                            ConsoleWindow.PrintTable(table, true);
                        } else if (argumentCount == 3) { // override value
                            #region construct new value
                            StringBuilder valueBuilder = new StringBuilder((argumentCount - 2) * 7);
                            valueBuilder.Append(info.args[2]); // append the first argument
                            for (int i = 3; i < argumentCount; i++) { // there are more arguments
                                valueBuilder.Append(' ');
                                valueBuilder.Append(info.args[i]);
                            }
                            string finalValue = valueBuilder.ToString();
                            #endregion
                            #region override target value
                            string lastValue = entry.value;
                            configuration[key] = finalValue;
                            configuration.Save();
                            ConsoleWindow.Print($"<b><color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(entry._key)}</color></b>: \"{ConsoleUtility.Escape(lastValue)}\" => \"{ConsoleUtility.Escape(configuration[key])}\"");
                            #endregion
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 3));
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #endregion

    }

}