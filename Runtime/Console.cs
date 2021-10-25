using System;
using System.Collections.Generic;
using System.Linq;

using BlackTundra.Foundation.Logging;
using BlackTundra.Foundation.Utility;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Used for logging application activity and executing commands.
    /// </summary>
    public static class Console {

        #region constant

        /// <summary>
        /// Charset allowed for command names.
        /// </summary>
        public static readonly char[] CommandNameCharset = "abcdefghijklmnopqrstuvwxyz0123456789_-".ToCharArray();

        /// <summary>
        /// Logger used for logging from the console.
        /// </summary>
        public static readonly Logger Logger = LogManager.RootLogger;

        /// <summary>
        /// Commands that the console can run.
        /// </summary>
        internal static readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        /// <summary>
        /// String that replaces the \t (tab) special character.
        /// </summary>
        private const string TabSpaces = "    ";

        #endregion

        #region nested

        #region Command

        public sealed class Command {

            #region constant

            private static readonly char[] CommandNameCharset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-.".ToCharArray();
            private static readonly char[] CommandDescriptionCharset = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-\\/,.?!£$%^&*()[]{};:'#~@+=|\n\"".ToCharArray();

            #endregion

            #region delegate

            public delegate bool CommandCallbackDelegate(CommandInfo info);

            #endregion

            #region variable

            /// <summary>
            /// Name of the <see cref="Command"/>.
            /// </summary>
            public readonly string name;

            /// <summary>
            /// Description of the <see cref="Command"/>.
            /// </summary>
            public readonly string description;

            /// <summary>
            /// Usage of the <see cref="Command"/>.
            /// This should detail how the command can be used.
            /// </summary>
            /// <remarks>
            /// Standard Formatting:
            /// <c>command_name {arg1} {arg2}\n\tdescription of what that does here.\ncommand_name {arg3}\n\tmore description here for a different variation of the command.</c>
            /// </remarks>
            public readonly string usage;

            internal readonly CommandCallbackDelegate callback;

            #endregion

            #region property

            #endregion

            #region constructor

            internal Command(in string name, in string description, in string usage, in CommandCallbackDelegate callback) {
                this.name = name;
                this.description = description;
                this.usage = usage;
                this.callback = callback;
            }

            #endregion

            #region logic

            public static bool IsValidName(in string name) => name.Matches(CommandNameCharset);

            public static bool IsValidDescription(in string description) => description.Matches(CommandDescriptionCharset);

            public sealed override string ToString() => description != null ? string.Concat(name, ": ", description) : name;

            #endregion

        }

        #endregion

        #endregion

        #region event

        /// <summary>
        /// Invoked when a <see cref="LogEntry"/> is pushed to the console <see cref="Logger"/>.
        /// </summary>
        public static event Action<LogEntry> OnPushLogEntry;

        #endregion

        #region property

        public static int TotalCommands => Commands.Count;

        #endregion

        #region constructor

        static Console() {
            Logger.OnPushLogEntry += OnPushLogEntry;
#if UNITY_EDITOR
            Logger.OnPushLogEntry += PushToUnityDebugConsole;
#endif
        }

        #endregion

        #region logic

        #region PushToUnityDebugConsole
#if UNITY_EDITOR
        /// <summary>
        /// Invoked when the <see cref="Console"/> has an entry pushed to it while the application is running
        /// and therefore the in-game <see cref="ConsoleWindow"/> cannot be seen.
        /// </summary>
        private static void PushToUnityDebugConsole(LogEntry entry) {
            if (!UnityEngine.Application.isEditor || UnityEngine.Application.isPlaying) return; // application is not in the Unity editor
            int priority = entry.logLevel.priority;
            if (priority <= LogLevel.Info.priority) {
                UnityEngine.Debug.Log(entry.FormattedPlainTextEntry);
            } else if (priority <= LogLevel.Warning.priority) {
                UnityEngine.Debug.LogWarning(entry.FormattedPlainTextEntry);
            } else {
                UnityEngine.Debug.LogError(entry.FormattedPlainTextEntry);
            }
        }
#endif
        #endregion

        #region Shutdown

        /// <summary>
        /// Called internally to shut down the application console.
        /// </summary>
        internal static void Shutdown() {
            Logger.Dispose();
            Commands.Clear();
        }

        #endregion

        #region Flush

        internal static void Flush() => Logger.Flush();

        #endregion

        #region Empty
        /// <summary>
        /// Pushes an empty message to the <see cref="Console"/>.
        /// This message will not have a log level or timestamp associated with it.
        /// </summary>
        /// <param name="message"></param>
        public static void Empty(in string message) => Logger.Push(LogLevel.None, message);
        #endregion

        #region Trace
        public static void Trace(in string message) => Logger.Push(LogLevel.Trace, message);
        #endregion

        #region Debug
        public static void Debug(in string message) => Logger.Push(LogLevel.Debug, message);
        #endregion

        #region Info
        public static void Info(in string message) => Logger.Push(LogLevel.Info, message);
        #endregion
        
        #region Warning
        public static void Warning(in string message) => Logger.Push(LogLevel.Warning, message);
        #endregion

        #region Error
        public static void Error(in string message) => Logger.Push(LogLevel.Error, message);
        public static void Error(in string message, in Exception exception) {
            if (exception != null) {
                exception.Handle();
                Logger.Push(
                    LogLevel.Error,
                    string.Concat(message, Environment.NewLine, exception.ToString())
                );
            } else {
                Logger.Push(LogLevel.Error, message);
            }
        }
        #endregion

        #region Fatal
        public static void Fatal(in string message) => Logger.Push(LogLevel.Fatal, message);
        public static void Fatal(in string message, in Exception exception) {
            if (exception != null) {
                exception.Handle();
                Logger.Push(
                    LogLevel.Fatal,
                    string.Concat(message, Environment.NewLine, exception.ToString())
                );
            } else {
                Logger.Push(LogLevel.Fatal, message);
            }
        }
        #endregion

        #region AssertCondition

        public static void AssertCondition(in bool condition) {
            if (condition) throw new ArgumentException(nameof(condition));
        }

        #endregion

        #region AssertReference

        public static void AssertReference<T>(in T reference) where T : class {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
        }

        #endregion

        #region Bind

        public static Command Bind(in string name, in Command.CommandCallbackDelegate callback, string description = null, string usage = null) {

            #region validate arguments
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            if (!Command.IsValidName(name)) throw new ArgumentException(nameof(name));
            if (description != null) {
                description = description.Replace("\t", TabSpaces);
                if (!Command.IsValidDescription(description)) throw new ArgumentException(nameof(description));
            }
            if (usage != null) {
                usage = usage.Replace("\t", TabSpaces);
                if (!Command.IsValidDescription(usage)) throw new ArgumentException(nameof(usage));
            }
            if (Commands.ContainsKey(name)) throw new ArgumentException(string.Concat(nameof(name), ": \"", name, "\" command already exists."));
            #endregion

            Command command = new Command(name, description, usage, callback);
            Commands.Add(name, command);
            return command;

        }

        #endregion

        #region Execute

        public static bool Execute(in ConsoleWindow context, string command) {
            if (command.IsNullOrWhitespace()) return true; // no command entered
            command = command.Trim();
            Info(string.Concat("[Console] Command: ", command));
            return CommandInfo.Execute(context, command);
        }

        #endregion

        #region GetCommand

        public static Command GetCommand(in string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return Commands.TryGetValue(name, out Command command) ? command : null;
        }

        #endregion

        #region GetCommands

        public static Command[] GetCommands() => Commands.Values.ToArray();

        #endregion

        #endregion

    }

}