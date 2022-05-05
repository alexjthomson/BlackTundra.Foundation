using BlackTundra.Foundation.Utility;

using System;
using System.Runtime.InteropServices;

namespace BlackTundra.Foundation {

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct Version : IEquatable<Version> {

        #region constant

        /// <summary>
        /// Invalid version "0.0.0a".
        /// </summary>
        public static readonly Version Invalid = new Version(0, 0, 0, ReleaseType.Alpha);

        #endregion

        #region enum

        public enum ReleaseType : short {

            Alpha = 0,
            Beta = 1,
            Final = 2
        }

        #endregion

        #region variable

        [FieldOffset(0)]
        public ushort major;

        [FieldOffset(2)]
        public ushort minor;

        [FieldOffset(4)]
        public ushort release;

        [FieldOffset(6)]
        public ReleaseType type;

        #endregion

        #region constructor

        public Version(in ushort major, in ushort minor, in ushort release, in ReleaseType type) {
            this.major = major;
            this.minor = minor;
            this.release = release;
            this.type = type;
        }

        #endregion

        #region logic

        #region Equals

        public bool Equals(Version other) {
            return other.release == release
                && other.minor == minor
                && other.major == major
                && other.type == type;
        }

        #endregion

        #region ToLong

        public long ToLong() => major | (long)minor >> 16 | (long)release >> 32 | (long)type >> 48;

        #endregion

        #region ToCompatibilityCode

        /// <summary>
        /// Calculates a code that can be checked against other versions to confirm they're compatible.
        /// </summary>
        public int ToCompatibilityCode() => (major | (long)minor >> 16 | (long)type >> 32).ToGUID();

        #endregion

        #region ToString

        public override string ToString() => $"{major}.{minor}.{release}{ReleaseTypeToChar(type)}";

        #endregion

        #region Parse

        /// <summary>
        /// Parses a string in the format "{major}.{minor}.{release}{release_type}" to a Version.
        /// </summary>
        /// <param name="version">String to parse to a Version.</param>
        public static Version Parse(in string version) {

            if (version == null) throw new ArgumentNullException(nameof(version));

            // example: 1.4.16b (major: 1, minor: 4, release: 16, release_type: beta)

            string[] tokens = version.Split('.');
            if (tokens.Length != 3) throw new FormatException(
                string.Format(
                    "Expected 3 tokens, found {0}.",
                    tokens.Length
                )
            );

            string releaseString = tokens[2];
            if (releaseString.Length < 2) throw new FormatException("Release token must contain at least 2 characters.");

            int releaseCharIndex = releaseString.Length - 1;
            return new Version(
                ushort.Parse(tokens[0]),
                ushort.Parse(tokens[1]),
                ushort.Parse(releaseString.Substring(0, releaseCharIndex)),
                CharToReleaseType(releaseString[releaseCharIndex])
            );

        }

        #endregion

        #region GetHashCode

        public override int GetHashCode() => ToLong().ToGUID();

        #endregion

        #region ReleaseTypeToChar

        private static char ReleaseTypeToChar(in ReleaseType type) {

            switch (type) {

                case ReleaseType.Alpha: return 'a';
                case ReleaseType.Beta: return 'b';
                case ReleaseType.Final: return 'f';
                default:
                    throw new FormatException(
               string.Format(
                   "Unknown release type: \"ReleaseType.{0}\".",
                   type.ToString()
               )
           );

            }

        }

        #endregion

        #region CharToReleaseType

        private static ReleaseType CharToReleaseType(in char c) {
            return c switch {
                'a' => ReleaseType.Alpha,
                'b' => ReleaseType.Beta,
                'f' => ReleaseType.Final,
                _ => throw new ArgumentException($"Unknown release code: '{c}'.")
            };
        }

        #endregion

        #region IsValid

        public bool IsValid() => major > 0;
        public static bool IsValid(in string version) {
            if (version == null) throw new ArgumentNullException(nameof(version));
            Version v;
            try { v = Parse(version); } catch (Exception) { return false; }
            return v.IsValid();
        }
        public static bool IsValid(in Version version) => version.major > 0;

        #endregion

        #endregion

        #region operators

        public static bool operator <(Version lhs, Version rhs) => lhs.major < rhs.major || (lhs.major == rhs.major && (lhs.minor < rhs.minor || (lhs.minor == rhs.minor && lhs.release < rhs.release)));
        public static bool operator >(Version lhs, Version rhs) => lhs.major > rhs.major || (lhs.major == rhs.major && (lhs.minor > rhs.minor || (lhs.minor == rhs.minor && lhs.release > rhs.release)));

        #endregion

    }

}