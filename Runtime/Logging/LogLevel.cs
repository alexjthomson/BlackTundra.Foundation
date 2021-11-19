using System;

using UnityEngine;

namespace BlackTundra.Foundation.Logging {

    /// <summary>
    /// Contains information about a level that a log message is logged at.
    /// </summary>
    public struct LogLevel : IEquatable<LogLevel>, IEquatable<LogType> {

        #region constant

        public static readonly LogLevel None    = new LogLevel(-3, null     , null   , LogType.Log     , ConsoleColour.White  );
        public static readonly LogLevel Trace   = new LogLevel(-2, "trace"  , "TRACE", LogType.Log     , ConsoleColour.Trace  );
        public static readonly LogLevel Debug   = new LogLevel(-1, "debug"  , "DEBUG", LogType.Log     , ConsoleColour.Debug  );
        public static readonly LogLevel Info    = new LogLevel( 0, "info"   , "INFO" , LogType.Log     , ConsoleColour.Info   );
        public static readonly LogLevel Warning = new LogLevel( 1, "warning", "WARN" , LogType.Warning , ConsoleColour.Warning);
        public static readonly LogLevel Error   = new LogLevel( 2, "error"  , "ERROR", LogType.Error   , ConsoleColour.Error  );
        public static readonly LogLevel Fatal   = new LogLevel( 3, "fatal"  , "FATAL", LogType.Error   , ConsoleColour.Fatal  );

        private static readonly LogLevel[] LogLevels = new LogLevel[] {
            None, Trace, Debug, Info, Warning, Error, Fatal
        };

        #endregion

        #region variable

        /// <summary>
        /// Priority that describes how important the log level is.
        /// A higher value is a higher importance.
        /// </summary>
        public readonly int priority;

        /// <summary>
        /// Unique name of a log level.
        /// </summary>
        public readonly string distinctName;

        /// <summary>
        /// Name used in a log.
        /// </summary>
        public readonly string logName;

        /// <summary>
        /// Unity <see cref="LogType"/> assoicated with this <see cref="LogLevel"/>.
        /// </summary>
        public readonly LogType unityLogType;

        /// <summary>
        /// Color to display the log file.
        /// </summary>
        public readonly ConsoleColour colour;

        #endregion

        #region constructor

        private LogLevel(in int priority, in string distinctName, in string logName, in LogType unityLogType, in ConsoleColour colour) {
            this.priority = priority;
            this.distinctName = distinctName;
            this.logName = logName;
            this.unityLogType = unityLogType;
            this.colour = colour;
        }

        #endregion

        #region logic

        #region Parse

        /// <summary>
        /// Parses a <paramref name="distinctName"/> to a <see cref="LogLevel"/>.
        /// </summary>
        /// <returns>
        /// If no <see cref="LogLevel"/> is found matching the <paramref name="distinctName"/>, <see cref="None"/> is returned; otherwise, the matching log level is returned.
        /// </returns>
        public static LogLevel Parse(in string distinctName) {
            if (distinctName == null) throw new ArgumentNullException(nameof(distinctName));
            LogLevel logLevel;
            for (int i = LogLevels.Length - 1; i >= 0; i--) {
                logLevel = LogLevels[i];
                if (distinctName.Equals(logLevel.distinctName, StringComparison.OrdinalIgnoreCase)) {
                    return logLevel;
                }
            }
            return None;
        }

        #endregion

        #region Equals

        public bool Equals(LogLevel logLevel) => priority == logLevel.priority;
        public bool Equals(LogType logType) => unityLogType == logType;

        #endregion

        #endregion

    }

    public static class LogLevelUtility {

        #region logic

        /// <summary>
        /// Converts a <see cref="LogType"/> to a <see cref="LogLevel"/>.
        /// </summary>
        public static LogLevel ToLogLevel(this LogType type) {
            switch (type) {
                case LogType.Log: return LogLevel.Info;
                case LogType.Warning: return LogLevel.Warning;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception: return LogLevel.Error;
                default: throw new NotSupportedException();
            }
        }

        #endregion

    }

}