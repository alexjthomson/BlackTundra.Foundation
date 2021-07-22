namespace BlackTundra.Foundation.IO {

    /// <summary>
    /// Describes the method used to write a file to the system.
    /// </summary>
    public enum FileFormat : int {

        /// <summary>
        /// The file has no special formatting or compression applied to it.
        /// It is saved as raw bytes.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// The file is first encrypted using AES using a known IV and key and is then compressed using GZIP compression.
        /// </summary>
        Obfuscated = 1,

        /// <summary>
        /// The file is first encrypted using AES using a known IV and randomly generated key and is then compressed using GZIP compression.
        /// </summary>
        Encrypted = 2

    }

}