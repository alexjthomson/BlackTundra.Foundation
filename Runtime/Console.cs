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
        private static readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>();

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

            public delegate bool CommandCallbackDelegate(in Command command, in string[] args);

            #endregion

            #region variable

            public readonly string name;

            public readonly string description;

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

        #region CommandInfo

        private sealed class CommandInfo {

            #region variable

            internal readonly Command command;
            internal readonly string[] args;
            /// <summary>
            /// When <c>false</c>, the command will only execute after the last command
            /// has executed successfully. If <c>true</c>, the command will execute
            /// regardless of whether the last command executed successfully.
            /// </summary>
            internal bool independent;

            #endregion

            #region constructor

            private CommandInfo(in Command command, in string[] args, in bool independent) {
                this.command = command;
                this.args = args;
                this.independent = independent;
            }

            #endregion

            #region logic

            #region ProcessCommand

            internal static CommandInfo[] ProcessCommand(string command) {

                command = command.Trim();
                int commandLength = command.Length;
                if (commandLength == 0) return new CommandInfo[0];

                List<CommandInfo> commandInfoList = new List<CommandInfo>();

                string commandName = null; // track the current command name, null if no name
                List<string> argumentList = new List<string>(); // track the current arguments
                bool independent = true; // marks if the current command is independent
                bool nextIndependent = true; // marks if the next command is independent from the last command

                int tokenStart = -1; // start index (inclusive)
                int tokenEnd = -1; // end index (exclusive)

                const int InternalState_AwaitToken = 0;
                const int InternalState_AwaitWhitespace = 1; // waiting for whitespace after token
                const int InternalState_AwaitEndStringToken = 2; // waiting for " character

                int internalState = InternalState_AwaitToken;
                int lastIndex = commandLength - 1;

                char c;
                bool processCommand = false; // when true, the current command will be processed and the internal state will be reset
                for (int i = 0; i < commandLength; i++) {
                    c = command[i];
                    switch (internalState) {
                        case InternalState_AwaitToken: {
                            if (c == '"') { // string
                                tokenStart = i + 1;
                                internalState = InternalState_AwaitEndStringToken;
                            } else if (c == ';') {
                                independent = true;
                                processCommand = true;
                            } else if (c == '&') {
                                independent = false;
                                processCommand = true;
                            } else if (!char.IsWhiteSpace(c)) {
                                tokenStart = i;
                                internalState = InternalState_AwaitWhitespace;
                            }
                            break;
                        }
                        case InternalState_AwaitWhitespace: {
                            if (char.IsWhiteSpace(c) || i == lastIndex) {
                                if (tokenEnd == -1) tokenEnd = i == lastIndex ? commandLength : i;
                                string token = command.Substring(tokenStart, tokenEnd - tokenStart);
                                if (commandName == null) commandName = token;
                                else argumentList.Add(token);
                                tokenEnd = -1;
                                internalState = InternalState_AwaitToken;
                            }
                            break;
                        }
                        case InternalState_AwaitEndStringToken: {
                            if (c == '\\') { // escape character
                                if (++i == commandLength) throw new CommandSyntaxException(string.Concat("Command contains incomplete string: ", command));
                            } else if (c == '"') { // end of string
                                tokenEnd = i;
                                internalState = InternalState_AwaitWhitespace;
                            }
                            break;
                        }
                        default: throw new NotSupportedException(string.Concat("Unknown internal state: ", internalState));
                    }

                    if (processCommand || i == lastIndex) { // process the current command
                        processCommand = false;
                        if (commandName != null && Commands.TryGetValue(commandName, out Command cmd))
                            commandInfoList.Add(new CommandInfo(cmd, argumentList.ToArray(), independent));
                        commandName = null;
                        argumentList.Clear();
                        independent = nextIndependent;
                        nextIndependent = true;
                        tokenEnd = -1;
                        internalState = InternalState_AwaitToken;
                    }

                }

                return commandInfoList.ToArray();

            }

            #endregion

            #region Execute

            private bool Execute() {
                try {
                    return command.callback(command, args);
                } catch (Exception exception) {
                    exception.Handle();
                    return false;
                }
            }

            internal static bool Execute(in string command) => Execute(ProcessCommand(command));

            /// <returns>Success state of the last executed command.</returns>
            private static bool Execute(in CommandInfo[] commandInfo) {
                CommandInfo info;
                bool lastCommandState = true;
                for (int i = 0; i < commandInfo.Length; i++) {
                    info = commandInfo[i];
                    if (info.independent || lastCommandState)
                        lastCommandState = info.Execute();
                }
                return lastCommandState;
            }

            #endregion

            #endregion

        }

        #endregion

        #region CommandSyntaxException

        public sealed class CommandSyntaxException : Exception {
            internal CommandSyntaxException(in string message, in Exception innerException = null) : base(message, innerException) { }
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
        }

        #endregion

        #region logic

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

        #region Bind

        public static Command Bind(in string name, in Command.CommandCallbackDelegate callback, string description = null, string usage = null) {

            #region validate arguments
            if (name == null) throw new ArgumentNullException("name");
            if (callback == null) throw new ArgumentNullException("callback");
            if (!Command.IsValidName(name)) throw new ArgumentException("Invalid name.");
            if (description != null) {
                description = description.Replace("\t", TabSpaces);
                if (!Command.IsValidDescription(description)) throw new ArgumentException("Invalid decription.");
            }
            if (usage != null) {
                usage = usage.Replace("\t", TabSpaces);
                if (!Command.IsValidDescription(usage)) throw new ArgumentException("Invalid usage.");
            }
            if (Commands.ContainsKey(name)) throw new ArgumentException(string.Concat(name, " command already exists."));
            #endregion

            Command command = new Command(name, description, usage, callback);
            Commands.Add(name, command);
            return command;

        }

        #endregion

        #region Execute

        public static bool Execute(in string command) {
            if (command.IsNullOrWhitespace()) return true; // no command entered
            return CommandInfo.Execute(command);
        }

        #endregion

        #region GetCommand

        public static Command GetCommand(in string name) {
            if (name == null) throw new ArgumentNullException("name");
            return Commands.TryGetValue(name, out Command command) ? command : null;
        }

        #endregion

        #region GetCommands

        public static Command[] GetCommands() => Commands.Values.ToArray();

        #endregion

        #endregion

    }

}