/*                                                                         *\

                    Copyright (C) 2019 Black Tundra Ltd
                            All rights reserved

     The copyright to the computer program(s) herein is the property of
  Black Tundra LTD. The program(s) may be used and/or copied only with the
   written permission of Black Tundra LTD or in accordance with the terms
     and conditions stipulated in the agreement/contract under which the
      program(s) have been supplied. This copyright notice must not be
                                  removed.
                                  
\*                                                                         */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using BlackTundra.Foundation.Collections;
using BlackTundra.Foundation.Security;
using BlackTundra.Foundation.Utility;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace BlackTundra.Foundation.IO {

    /// <summary>
    /// Utility used to interact with the operating system's file system.
    /// </summary>
    public static class FileSystem {

        #region constant

        /// <summary>
        /// Default encoding used when reading/writing to the system.
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Regex pattern for file names (excluding the extension).
        /// </summary>
        public const string FileNameRegexPattern = "[a-zA-Z]{1}[a-zA-Z0-9_]*";

        public const string LocalLogsDirectory = "Logs/";
        public const string LocalResourcesDirectory = "Resources/";
        public const string LocalCacheDirectory = LocalResourcesDirectory + "Cache/";
        public const string LocalConfigDirectory = LocalResourcesDirectory + "Config/";
        public const string LocalDataDirectory = LocalResourcesDirectory + "Data/";
        public const string LocalSavesDirectory = LocalResourcesDirectory + "Saves/";
#if USE_MOD_FRAMEWORK
        public const string LocalModsDirectory = LocalResourcesDirectory + "Mods/";
#endif

        /// <summary>
        /// Order that every local directory is created in.
        /// This array is used to maintain the project directory and ensure every directory that needs
        /// to exist exists.
        /// </summary>
        private static readonly string[] FileSystemDirectoryLayout = new string[] {
            LocalLogsDirectory,
            LocalResourcesDirectory,
            LocalCacheDirectory,
            LocalConfigDirectory,
            LocalDataDirectory,
            LocalSavesDirectory,
#if USE_MOD_FRAMEWORK
            LocalModsDirectory,
#endif
        };

        /// <summary>
        /// Extension (including the '.' character) of config files.
        /// </summary>
        public const string ConfigExtension = ".config";

        #endregion

        #region variable

        /// <summary>
        /// Keystore used to associate GUIDs with crypto-keys.
        /// </summary>
        private static Keystore keystore = null;

        #endregion

        #region property

        /// <summary>
        /// Keystore used by the FileSystem to associate files with crypto-keys.
        /// </summary>
        public static Keystore Keystore {
            get {
                if (keystore == null) { // no keystore exists
                    if (Read(
                        new FileSystemReference(
                            string.Concat(LocalDataDirectory, "keystore.dat"),
                            true, // is local
                            false // is not a directory
                        ),
                        out byte[] content,
                        FileFormat.Obfuscated
                    )) { // read was successfull
                        try {
                            keystore = Keystore.FromBytes(content, 0, out _);
                        } catch (Exception exception) {
                            exception.Handle("Failed to convert keystore data to keystore object.");
                            throw exception;
                        }
                    } else { // keystore couldn't be loaded from the system, assume there is none
                        keystore = new Keystore();
                    }
                }
                return keystore; // return a reference to the keystore
            }
        }

        #endregion

        #region logic

        #region Initialise

        /// <summary>
        /// Called when the application is initialised.
        /// </summary>
        internal static void Initialise() {
            #region check directory structure
            string path;
            for (int i = 0; i < FileSystemDirectoryLayout.Length; i++) {
                path = ToCanonicalPath(FileSystemDirectoryLayout[i]);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            #endregion
        }

        #endregion

        #region OnPostProcessBuild
#if UNITY_EDITOR
        /// <summary>
        /// Called after a build has been made, this method is responsible for setting up the file system
        /// for the build.
        /// </summary>
        [PostProcessBuild(1)]
#pragma warning disable IDE0051 // remove unused private members
        private static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath) {
#pragma warning restore IDE0051 // remove unused private members

            buildPath = Path.GetDirectoryName(buildPath).Replace('\\', '/');
            if (buildPath[buildPath.Length - 1] != '/') buildPath += '/';
            UnityEngine.Debug.Log($"Build location: \"{buildPath}\", target: {buildTarget}.");

            #region check directory structure
            string path;
            for (int i = 0; i < FileSystemDirectoryLayout.Length; i++) {
                path = buildPath + FileSystemDirectoryLayout[i];
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
            UnityEngine.Debug.Log("Build file system built.");
            #endregion

            #region copy data
            CopyToBuild(LocalConfigDirectory, buildPath);
            CopyToBuild(LocalDataDirectory, buildPath);
            UnityEngine.Debug.Log("Project data transferred to build.");
            #endregion

        }
#endif
        #endregion

        #region CopyToBuild
#if UNITY_EDITOR
        /// <summary>
        /// Copies a file from the project (unity editor) to a build of the game.
        /// </summary>
        /// <param name="localPath">Local path to a directory from the project to copy to the build.</param>
        /// <param name="buildPath">Canonical path to the build root directory.</param>
        private static void CopyToBuild(in string localPath, in string buildPath) {
            string project = ToCanonicalPath(localPath);
            string build = buildPath + localPath;
            string[] files = Directory.GetFiles(project);
            for (int i = 0; i < files.Length; i++) File.Copy(files[i], build + Path.GetFileName(files[i]), true);
        }
#endif
        #endregion

        #region ToCanonicalPath

        /// <summary>
        /// Converts a local path to an absolute (full) path on the system.
        /// </summary>
        /// <param name="localPath">Local path relative to the root project directory.</param>
        /// <param name="formatAsDirectory">When true, a '/' character is appended to the path.</param>
        /// <returns>Absolute path on the system.</returns>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCanonicalPath(in string localPath, in bool formatAsDirectory = false) {
            if (localPath == null) throw new ArgumentNullException(nameof(localPath));
            string path = Path.GetFullPath(localPath).Replace('\\', '/');
            if (formatAsDirectory && path[path.Length - 1] != '/') path += '/';
            return path;
        }

        #endregion

        #region Write

        public static bool Write(in FileSystemReference fsr, in string content, in FileFormat fileFormat = FileFormat.Standard, in bool append = false) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (content == null) throw new ArgumentNullException(nameof(content));
            byte[] byteContent = DefaultEncoding.GetBytes(content);
            return Write(fsr, byteContent, fileFormat, append);
        }

        public static bool Write(in FileSystemReference fsr, in byte[] content, in FileFormat fileFormat = FileFormat.Standard, in bool append = false) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (content == null) throw new ArgumentNullException(nameof(content));

            byte[] formattedContent;
            if (fileFormat == FileFormat.Standard)
                formattedContent = content;
            else if (fileFormat == FileFormat.Obfuscated || fileFormat == FileFormat.Encrypted) {

                #region format content
                if (append) {
                    if (!Read(fsr, out byte[] existingContent, fileFormat))
                        return false;
                    int contentLength = existingContent.Length + content.Length;
                    formattedContent = new byte[contentLength];
                    Array.Copy(existingContent, formattedContent, existingContent.Length);
                    Array.Copy(content, 0, formattedContent, existingContent.Length, content.Length);
                } else
                    formattedContent = content;
                #endregion

                #region encrypt content
                try {
                    formattedContent = fileFormat == FileFormat.Obfuscated
                        ? CryptoUtility.Obfuscate(formattedContent)
                        : CryptoUtility.Encrypt(formattedContent, GetCryptoKey(fsr));
                } catch (Exception exception) {
                    exception.Handle();
                    return false;
                }
                #endregion

            } else throw new InvalidOperationException($"Unknown file format: \"{fileFormat}\".");

            #region write content
            try {
                using FileStream fileStream = new FileStream(fsr.AbsolutePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write);
                fileStream.Write(formattedContent, 0, formattedContent.Length);
            } catch (Exception exception) {
                exception.Handle();
                return false;
            }
            #endregion

            return true;

        }

        #endregion

        #region Read

        public static bool Read(in FileSystemReference fsr, out string content, in FileFormat fileFormat = FileFormat.Standard) {
            if (Read(fsr, out byte[] bytes, fileFormat)) {
                content = DefaultEncoding.GetString(bytes);
                return true;
            } else {
                content = null;
                return false;
            }
        }

        public static bool Read(in FileSystemReference fsr, out byte[] content, in FileFormat fileFormat = FileFormat.Standard) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));

            #region load content
            byte[] rawContent;
            try {
                rawContent = File.ReadAllBytes(fsr.AbsolutePath);
            } catch (FileNotFoundException) {
                content = null;
                return false;
            } catch (Exception exception) {
                exception.Handle();
                content = null;
                return false;
            }
            #endregion

            if (rawContent == null) {
                content = null;
                return false;
            }

            if (fileFormat == FileFormat.Standard)
                content = rawContent;
            else if (fileFormat == FileFormat.Obfuscated || fileFormat == FileFormat.Encrypted) {
                try {
                    content = fileFormat == FileFormat.Obfuscated
                        ? CryptoUtility.Deobfuscate(rawContent)
                        : CryptoUtility.Decrypt(rawContent, GetCryptoKey(fsr));
                } catch (Exception exception) {
                    exception.Handle();
                    content = null;
                    return false;
                }
            } else throw new InvalidOperationException($"Unknown file format: \"{fileFormat}\".");

            return true;

        }

        #endregion

        #region Delete

        /// <summary>
        /// Deletes a local <paramref name="fsr">file system reference</paramref>.
        /// </summary>
        /// <param name="fsr">Local <see cref="FileSystemReference"/>.</param>
        /// <returns>Returns <c>true</c> if <paramref name="fsr"/> was deleted.</returns>
        public static bool Delete(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            if (!fsr.IsLocal) { // file system reference is not local, do not allow deletion of non-local files
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning("Deletion of non-local files is prohibited.");
#endif
                return false;
            }
            if (fsr.IsFile) {
                File.Delete(fsr.AbsolutePath);
            } else if (fsr.IsDirectory) {
                Directory.Delete(fsr.AbsolutePath, true);
            }
            return true;
        }

        #endregion

        #region UpdateConfiguration

        internal static bool UpdateConfiguration(in Configuration configuration) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (!Read(configuration.fsr, out string content, configuration.format)) content = string.Empty;
            StringBuilder configBuilder;
            try {
                configBuilder = configuration.Parse(
                    content,
                    rewrite: true,
                    overrideExisting: false,
                    overrideIsDirty: true
                );
            } catch (ConfigurationSyntaxException exception) {
                exception.Handle();
                return false;
            }
            return Write(configuration.fsr, configBuilder.ToString(), configuration.format);
        }

        #endregion

        #region LoadConfiguration

        internal static Configuration LoadConfiguration(Configuration configuration) {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (Read(configuration.fsr, out string content, configuration.format)) {
                try {
                    configuration.Parse(
                        content,
                        rewrite: false,
                        overrideExisting: true,
                        overrideIsDirty: true
                    );
                } catch (ConfigurationSyntaxException exception) {
                    exception.Handle();
                }
            }
            return configuration;
        }

        #endregion

        #region GetFiles

        /// <summary>
        /// Recursivly searches for files in the game directory that match
        /// a provided pattern.
        /// provided.
        /// </summary>
        /// <param name="pattern">
        /// The search string to match against the names of files in path.
        /// This parameter can contain a combination of valid literal path
        /// and wildcard (* and ?) characters, but it doesn't support
        /// regular expressions.
        /// </param>
        /// <returns>
        /// An array of all matching files.
        /// </returns>
        public static string[] GetFiles(in string pattern) {
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), pattern, SearchOption.AllDirectories); // get files
            for (int i = 0; i < files.Length; i++) files[i] = files[i].Replace('\\', '/'); // format
            return files; // return formatted file paths
        }

        #endregion

        #region GetCryptoKey

        /// <summary>
        /// Gets the crypto-key corresponding to a <see cref="FileSystemReference"/>.
        /// </summary>
        /// <param name="fsr"><see cref="FileSystemReference"/> to a file with a crypto-key.</param>
        /// <returns>Key assosicated with the <paramref name="fsr"/>.</returns>
        private static byte[] GetCryptoKey(in FileSystemReference fsr) {
            if (!fsr.IsFile) throw new InvalidOperationException("Only files can have crypto-keys.");
            string key = string.Concat(
                fsr.IsLocal ? "~" : "/", // add identifier ('~' for local, '/' for non-local)
                fsr.LocalPath.ToLower()
            );
            return Keystore[key];
        }

        #endregion

        #region OpenInExplorer

        /// <summary>
        /// Opens a path in the native file explorer for the current operating system.
        /// </summary>
        /// <param name="fsr">
        /// Reference to the location to open on the native file explorer.
        /// </param>
        public static Process OpenInExplorer(in FileSystemReference fsr) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
#if UNITY_STANDALONE_WIN || UNITY_WSA // windows
            //Process.Start("explorer.exe", $"/{(Directory.Exists(path) ? "root" : "select")},{path}");
            string path = fsr.AbsolutePath.Replace('/', '\\');
            return Process.Start("explorer.exe", fsr.IsDirectory ? path : $"/select,\"{path}\"");
#elif UNITY_STANDALONE_OSX // mac
            return Process.Start("open", Directory.Exists(path) ? path : ("-R " + path));
#elif UNITY_STANDALONE_LINUX // linux
            Process process;
            try { process = Process.Start("xdg-open", path); } catch (Exception) { }
            if (process != null) return process;
            try { process = Process.Start("nautilus", path); } catch (Exception) { }
            if (process != null) return process;
            try { process = Process.Start("thunar", path); } catch (Exception) { }
            if (process != null) return process;
            throw new NotImplementedException("No explorer found.");
#else // unsupported platform
            throw new NotSupportedException($"Cannot open \"{nameof(fsr)}\" in native file explorer because the platform is not supported.");
#endif
        }

        #endregion

        #region Run

        /// <summary>
        /// Starts a process on the local machine as the current user.
        /// </summary>
        /// <param name="fsr"><see cref="FileSystemReference"/> to the resource to run.</param>
        /// <param name="arguments">Arguments to start the process running the resource with.</param>
        /// <returns>Returns the <see cref="Process"/> started to run the resource.</returns>
        public static Process Run(in FileSystemReference fsr, in string arguments = null) {
            if (fsr == null) throw new ArgumentNullException(nameof(fsr));
            return fsr.IsDirectory ? OpenInExplorer(fsr) : Process.Start(fsr.AbsolutePath, arguments);
        }

        #endregion

        #endregion

    }

}