using System;

using UnityEngine;

namespace BlackTundra.Foundation.Logging {

    /// <summary>
    /// Contains information about a level that a log message is logged at.
    /// </summary>
    public sealed class LogLevel {

        #region constant

        public static readonly LogLevel None    = new LogLevel(-3, null     , null   , ConsoleColour.White  );
        public static readonly LogLevel Trace   = new LogLevel(-2, "trace"  , "TRACE", ConsoleColour.Trace  );
        public static readonly LogLevel Debug   = new LogLevel(-1, "debug"  , "DEBUG", ConsoleColour.Debug  );
        public static readonly LogLevel Info    = new LogLevel( 0, "info"   , "INFO" , ConsoleColour.Info   );
        public static readonly LogLevel Warning = new LogLevel( 1, "warning", "WARN" , ConsoleColour.Warning);
        public static readonly LogLevel Error   = new LogLevel( 2, "error"  , "ERROR", ConsoleColour.Error  );
        public static readonly LogLevel Fatal   = new LogLevel( 3, "fatal"  , "FATAL", ConsoleColour.Fatal  );

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
        /// Color to display the log file.
        /// </summary>
        public readonly ConsoleColour colour;

        #endregion

        #region constructor

        private LogLevel() => throw new NotSupportedException();

        private LogLevel(in int priority, in string distinctName, in string logName, in ConsoleColour colour) {
            this.priority = priority;
            this.distinctName = distinctName;
            this.logName = logName;
            this.colour = colour;
        }

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