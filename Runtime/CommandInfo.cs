using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using static BlackTundra.Foundation.Console;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Contains information about a command that is being executed.
    /// </summary>
    public sealed class CommandInfo {

        #region constant

        /// <summary>
        /// Regex checker for flag text.
        /// </summary>
        private static readonly Regex FlagRegex = new Regex(
            @"[a-z]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// Character used for declaring flags.
        /// </summary>
        private const char FlagCharacter = '?';

        #endregion

        #region variable

        /// <summary>
        /// Command that has been called.
        /// </summary>
        public readonly Command command;

        /// <summary>
        /// Arguments associated with the command.
        /// </summary>
        public readonly ReadOnlyCollection<string> args;

        /// <summary>
        /// Flags set in the command.
        /// </summary>
        public readonly ReadOnlyCollection<string> flags;

        /// <summary>
        /// When <c>false</c>, the command will only execute after the last command
        /// has executed successfully. If <c>true</c>, the command will execute
        /// regardless of whether the last command executed successfully.
        /// </summary>
        internal bool independent;

        #endregion

        #region constructor

        internal CommandInfo(in Command command, in string[] args, in string[] flags, in bool independent) {
            this.command = command;
            this.args = Array.AsReadOnly(args);
            this.flags = Array.AsReadOnly(flags);
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
                            if (i == lastIndex) throw new CommandSyntaxException(string.Concat("Command contains incomplete string: ", command));
                        } else if (c == ';') {
                            independent = true;
                            processCommand = true;
                        } else if (c == '&') {
                            independent = false;
                            processCommand = true;
                        } else if (!char.IsWhiteSpace(c)) {
                            tokenStart = i;
                            internalState = InternalState_AwaitWhitespace;
                            if (i == lastIndex) goto case InternalState_AwaitWhitespace;
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
                            goto case InternalState_AwaitWhitespace;
                        }
                        break;
                    }
                    default: throw new NotSupportedException(string.Concat("Unknown internal state: ", internalState));
                }

                if (processCommand || i == lastIndex) { // process the current command
                    processCommand = false;
                    if (commandName != null && Commands.TryGetValue(commandName, out Command cmd)) {
                        string argument;
                        List<string> flagList = new List<string>();
                        for (int j = argumentList.Count - 1; j >= 0; j--) {
                            argument = argumentList[j];
                            if (argument.Length > 1 && argument[0] == FlagCharacter) { // flags
                                if (argument.Length > 2 && argument[1] == FlagCharacter) { // literal (word) flag
                                    string flag = argument.Substring(2, argument.Length - 2); // construct the flags
                                    if (!FlagRegex.IsMatch(flag)) throw new CommandSyntaxException("Flags must only contain letters."); // flag doesnt match flags regex
                                    if (!flagList.Contains(flag)) flagList.Add(flag); // if the flag has not been defined already, add it to the flags
                                } else { // list of single character flags
                                    char flag;
                                    for (int k = argument.Length - 1; k >= 1; k--) {
                                        flag = argument[k];
                                        if (!char.IsLetter(flag)) throw new CommandSyntaxException("Flags must only contain letters."); // flag is not a letter
                                        string strFlag = char.ToString(flag); // convert to string
                                        if (!flagList.Contains(strFlag)) flagList.Add(strFlag); // if the flag is not already defined, define the flag
                                    }
                                }
                                argumentList.RemoveAt(j); // remove the argument from the argument list
                            }
                        }
                        commandInfoList.Add(new CommandInfo(cmd, argumentList.ToArray(), flagList.ToArray(), independent));
                    }
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
                return command.callback(this);
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

        #region HasFlag

        /// <returns>Returns <c>true</c> if the <see cref="CommandInfo"/> contains the specified <paramref name="flag"/>.</returns>
        public bool HasFlag(in char flag) => flags.Contains(char.ToString(flag));

        /// <returns>Returns <c>true</c> if the <see cref="CommandInfo"/> contains the specified <paramref name="flag"/>.</returns>
        public bool HasFlag(in string flag) => flags.Contains(flag ?? throw new ArgumentNullException(nameof(flag)));

        /// <param name="flag">Single letter variant of the flag</param>
        /// <param name="fullFlag">Full name of the flag. This should all be one word only containing letters.</param>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="CommandInfo"/> contains the specified <paramref name="flag"/> or the
        /// full name of the flag <paramref name="fullFlag"/>.
        /// </returns>
        public bool HasFlag(in char flag, in string fullFlag) {
            if (fullFlag == null) throw new ArgumentNullException(nameof(fullFlag));
            return flags.Contains(fullFlag) || flags.Contains(char.ToString(flag));
        }

        #endregion

        #endregion

    }

}