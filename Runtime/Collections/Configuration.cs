//#define CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION

using System;
using System.Text;

using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Collections.Generic;

namespace BlackTundra.Foundation.Collections {

    public sealed class Configuration {

        #region constant

        private const int DefaultConfigurationBufferCapacity = 32;
        private const int DefaultConfigurationBufferExpandSize = 8;

        private readonly static string EndOfStatement = ';' + Environment.NewLine;

        #endregion

        #region variable

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
                if (key == null) throw new ArgumentNullException("key");
                int hash = key.GetHashCode();
                ConfigurationEntry entry;
                for (int i = buffer.Count - 1; i >= 0; i--) {
                    entry = buffer[i];
                    if (entry.hash == hash) {
                        if (!dirty) entry.dirty = false; // set as not dirty
                        return entry.value;
                    }
                }
                return null;
            }

            set {
                if (key == null) throw new ArgumentNullException("key");
                int hash = key.GetHashCode();
                ConfigurationEntry entry;
                for (int i = buffer.Count - 1; i >= 0; i--) {
                    entry = buffer[i];
                    if (entry.hash == hash) {
                        if (value == null) buffer.RemoveAt(i); // remove the value
                        else if (entry.value != value) { // value is different
                            entry.value = value; // override value
                            if (dirty) {
                                entry.dirty = true;
                                this.dirty = true;
                            } else
                                entry.dirty = false;
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
                if (index < 0 || index >= buffer.Count) throw new ArgumentOutOfRangeException("index");
                return buffer[index];
            }
        }

        /// <summary>
        /// Tracks if the <see cref="Configuration"/> is modified or not.
        /// </summary>
        public bool IsDirty => dirty;

        #endregion

        #region constructor

        public Configuration(in int capacity = DefaultConfigurationBufferCapacity, in int expandSize = DefaultConfigurationBufferExpandSize) {
            if (capacity < 1) throw new ArgumentOutOfRangeException("capacity must be greater than zero.");
            if (expandSize < 1) throw new ArgumentOutOfRangeException("expand size must be greater than zero.");
            buffer = new PackedBuffer<ConfigurationEntry>(capacity);
            this.expandSize = expandSize;
            dirty = false;
        }

        #endregion

        #region logic

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

                            #region find key

                            int length = keyEnd - keyStart + 1;
                            if (length < 1) throw new ConfigurationSyntaxException("Empty key found", keyStart);
                            string key = source.Substring(keyStart, length);

                            #endregion

                            #region find value

                            length = valueEnd - valueStart + 1;
                            string value = length <= 0 ? string.Empty : source.Substring(valueStart, length);

                            #endregion

                            #region override value

                            if (overrideExisting) { // any existing value in the configuration should be overridden

                                this[key, false] = value;

                            } else if (rewrite) { // the source should be overridden with the value inside the configuration (if it exists)

                                string existingValue = this[key, false];
                                builder.Append(key);
                                builder.Append(source.Substring(keyEnd + 1, valueStart - keyEnd - 1)); // add the characters between the key and value
                                builder.Append(existingValue ?? value);

                            }

                            #endregion

                            #region reset

                            keyStart = -1;
                            keyEnd = -1;

                            valueStart = -1;
                            //valueEnd = -1; // not required to be reset

                            noKey = true;

                            #endregion

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
                    if (entry.dirty) { // value is dirty
                        builder.Append(entry.key);
                        builder.Append(" = ");
                        builder.Append(entry.value);
                        builder.Append(EndOfStatement);
                        if (overrideIsDirty) entry.dirty = false; // no longer dirty
                    }
                }
                if (overrideIsDirty) dirty = false;
            }

            return builder; // return the builder

        }

        #endregion

        #region ForceGet

        /// <summary>
        /// Force gets an entry from the configuration.
        /// <para>
        /// If an entry already exists in the configuration, it will be returned provided it is parsed correctly.
#if CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION
        /// If it isn't parsed, a <see cref="ConfigurationSyntaxException"/> will be thrown.
#else
        /// If it isn't parsed, the <c>fallback</c> value will be returned instead.
#endif
        /// </para>
        /// </summary>
        /// <param name="key">Key associated with the value trying to be accessed.</param>
        /// <param name="fallback">Fallback value to return if no valid value is found in the <see cref="Configuration"/>.</param>
        public string ForceGet(in string key, in string fallback) {

            if (key == null) throw new ArgumentNullException("key");

            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback, true);
                return fallback;
            }

            return entry;

        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public int ForceGet(in string key, in int fallback) {

            if (key == null) throw new ArgumentNullException("key");

            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback.ToString(), true);
                return fallback;
            }

            try {
                return int.Parse(entry);
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
                this[key] = fallback.ToString();
                return fallback;
            }
#endif
        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public bool ForceGet(in string key, in bool fallback) {

            if (key == null) throw new ArgumentNullException("key");

            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback ? "true" : "false", true);
                return fallback;
            }

            entry = entry.ToLower();
            if (entry.Equals("true")) return true;
            else if (entry.Equals("false")) return false;
            else {
#if CONFIGURATION_FORCE_GET_THROW_PARSE_EXCEPTION
                throw new ConfigurationSyntaxException(
                    $"Failed to parse \"{entry}\" to a bool.",
                    -1
                );
#else
                this[key] = fallback ? "true" : "false";
                return fallback;
#endif
            }

        }

        /// <inheritdoc cref="ForceGet(in string, in string)"/>
        public float ForceGet(in string key, in float fallback) {

            if (key == null) throw new ArgumentNullException("key");

            string entry = this[key];
            if (entry == null) {
                AddEntry(key, fallback.ToString(), true);
                return fallback;
            }

            try {
                return float.Parse(entry);
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
                this[key] = fallback.ToString();
                return fallback;
            }
#endif

        }

        #endregion

        #region AddEntry

        private void AddEntry(in string key, in string value, in bool dirty) {

            if (buffer.Count == buffer.Capacity) buffer.Expand(expandSize); // expand the buffer
            buffer.AddLast(new ConfigurationEntry(key, value, dirty)); // add the new configuration entry to the buffer

        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the configuration of all entries.
        /// </summary>
        public void Clear() => buffer.Clear();

        #endregion

        #endregion

    }

}