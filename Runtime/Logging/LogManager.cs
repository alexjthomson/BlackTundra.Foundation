using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;
using System.Text;

namespace BlackTundra.Foundation.Logging {

    public static class LogManager {

        #region constant

        /// <summary>
        /// Name of the <see cref="RootLogger"/>.
        /// </summary>
        public const string RootLoggerName = "console";

        /// <summary>
        /// Default capacity of a <see cref="Logger"/> with an unspecified capacity.
        /// This is also the capacity of the <see cref="RootLoggerName"/>
        /// </summary>
        public const int DefaultLoggerCapacity = 16;

        /// <summary>
        /// Default <see cref="LogLevel"/> of a <see cref="Logger"/>.
        /// </summary>
        public static readonly LogLevel DefaultLoggerLogLevel = LogLevel.None;

        /// <summary>
        /// Default callback when a <see cref="Logger"/> buffer is full.
        /// This is used by the <see cref="RootLogger"/>.
        /// </summary>
        private static readonly Logger.LogBufferFullDelegate DefaultLoggerBufferFullDelegate = HandleFullLoggerBuffer;

        /// <summary>
        /// Default callback when <see cref="Logger.Clear"/> is invoked.
        /// This is used by the <see cref="RootLogger"/>.
        /// </summary>
        private static readonly Logger.ClearLoggerDelegate DefaultLoggerClearLoggerDelegate = HandleClearLogger;

        /// <summary>
        /// Root <see cref="Logger"/> used as the main logger for the application.
        /// </summary>
        public static readonly Logger RootLogger;

        /// <summary>
        /// Dictionary linking every <see cref="Logger"/> in the application to it's name.
        /// </summary>
        private static readonly Dictionary<string, Logger> LoggerDictionary;

        #endregion

        #region constructor

        static LogManager() {
            RootLogger = new Logger(
                typeof(LogManager),
                RootLoggerName,
                DefaultLoggerCapacity,
                DefaultLoggerLogLevel,
                DefaultLoggerBufferFullDelegate,
                DefaultLoggerClearLoggerDelegate
            );
            LoggerDictionary = new Dictionary<string, Logger>() {
                { RootLoggerName, RootLogger }
            };
        }

        #endregion

        #region logic

        #region GetLogger

        /// <summary>
        /// Gets a <see cref="Logger"/> by name. If no <see cref="Logger"/> is found with a matching
        /// name, a <see cref="KeyNotFoundException"/> will be thrown.
        /// </summary>
        /// <param name="name">Name of the <see cref="Logger"/> to get.</param>
        /// <returns>Always returns a reference to a <see cref="Logger"/>. This is never <c>null</c>.</returns>
        public static Logger GetLogger(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (LoggerDictionary.TryGetValue(name, out Logger logger)) return logger;
            throw new KeyNotFoundException(name);
        }

        #endregion

        #region GetContext

        /// <summary>
        /// Gets a <see cref="Logger"/> by context. A <see cref="Logger"/> can exists for every type, if
        /// no context exists for the requested type, a <see cref="Logger"/> will be created for the type
        /// and returned.
        /// </summary>
        /// <typeparam name="T">Type/context to get a <see cref="Logger"/> for.</typeparam>
        /// <returns>Returns a reference to the <see cref="Logger"/> for the provided context.</returns>
        public static Logger GetContext<T>() {
            Type type = typeof(T); // get the type of T
            string name = string.Concat("c_", type.FullName); // calculate the name of the context logger
            if (LoggerDictionary.TryGetValue(name, out Logger logger)) return logger; // try find the logger if it already exists
            else { // if no logger exists, create one
                logger = new Logger(
                    type,
                    name,
                    DefaultLoggerCapacity,
                    DefaultLoggerLogLevel,
                    DefaultLoggerBufferFullDelegate,
                    DefaultLoggerClearLoggerDelegate
                );
                LoggerDictionary.Add(name, logger);
                return logger;
            }
        }

        #endregion

        #region Exists

        /// <summary>
        /// Checks if a <see cref="Logger"/> exsits.
        /// </summary>
        /// <param name="name">Name of the <see cref="Logger"/>.</param>
        /// <returns>Returns true if a <see cref="Logger"/> exists with the provided name.</returns>
        public static bool Exists(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return LoggerDictionary.ContainsKey(name);
        }

        #endregion

        #region HandleFullLoggerBuffer

        private static void HandleFullLoggerBuffer(in Logger logger, in LogEntry[] buffer) {
            string content = FormatBufferToString(buffer).ToString();
            if (content.IsNullOrEmpty()) return;
            FileSystemReference fsr = GetFileSystemReference(logger);
            FileSystem.Write(fsr, content, FileFormat.Standard, true);
        }

        #endregion

        #region HandleClearLogger

        private static void HandleClearLogger(in Logger logger) {
            FileSystemReference fsr = GetFileSystemReference(logger);
            FileSystem.Delete(fsr);
        }

        #endregion

        #region FormatBufferToString

        private static StringBuilder FormatBufferToString(in LogEntry[] buffer) {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            StringBuilder stringBuilder = new StringBuilder(buffer.Length * 128);
            LogEntry entry;
            for (int i = 0; i < buffer.Length; i++) {
                entry = buffer[i];
                if (entry == null) continue;
                stringBuilder.Append(entry.FormattedLogEntry);
            }
            return stringBuilder;
        }

        #endregion

        #region GetFileSystemReference

        private static FileSystemReference GetFileSystemReference(in Logger logger) {
            return new FileSystemReference(
                string.Concat(FileSystem.LocalLogsDirectory, logger.name, ".log"),
                true, // is local
                false // is not directory
            );
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// Shuts down the <see cref="LogManager"/>.
        /// </summary>
        internal static void Shutdown() {
            foreach (Logger logger in LoggerDictionary.Values) {
                try {
                    logger.Dispose();
                } catch (Exception exception) {
                    exception.Handle($"An unhandled exception was caught while trying to dispose of logger \"{logger.name}\".");
                }
            }
            LoggerDictionary.Clear();
        }

        #endregion

        #endregion

    }

}