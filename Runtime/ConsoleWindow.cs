using System;
using System.Text;

using UnityEngine;
using UnityEngine.InputSystem;

using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Logging;
using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.IO;

namespace BlackTundra.Foundation {

    public static class ConsoleWindow {

        #region constant

        private const int WindowID = 10000;

        public const int MaxWidth = 8192;
        public const int MaxHeight = 4096;

        private const int WindowMargin = 16;
        private const int TitleBarHeight = 16;

        private static readonly Rect TitleBarRect = new Rect(0, 0, MaxWidth, TitleBarHeight);

        private const int DefaultEntryBufferSize = 256;
        private static readonly RollingBuffer<LogEntry> EntryBuffer = new RollingBuffer<LogEntry>(DefaultEntryBufferSize, true);

        private const int DefaultHistoryBufferSize = 32;

        /// <summary>
        /// Used so the control system can control the <see cref="ConsoleWindow"/>.
        /// </summary>
        private static readonly ConsoleWindowControlHandle ControlHandle = new ConsoleWindowControlHandle();

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(ConsoleWindow));

        #endregion

        #region nested

        /// <summary>
        /// Class used simply to allow the control system to see the <see cref="ConsoleWindow"/> as something it can control.
        /// </summary>
        private sealed class ConsoleWindowControlHandle : IControllable {
            public ControlFlags OnControlGained() {
                focus = true;
                return ControlFlags.None;
            }
            public void OnControlRevoked() { }
        }

        #endregion

        #region variable

        /// <summary>
        /// Tracks if the <see cref="ConsoleWindow"/> should be drawn or not.
        /// </summary>
        private static bool draw = false;

        private static Rect windowRect;

        private static Vector2 scrollPosition;
        private static string input;

        private static int fontWidth;

        private static GUIStyle entryStyle = null;
        private static GUIStyle inputStyle = null;

        private static bool focus = true;

        private static string[] inputHistoryBuffer = new string[DefaultHistoryBufferSize];
        private static int inputHistoryIndex;

        private static ConsoleWindowSettings settings = null;

        #endregion

        #region property

        [ConfigurationEntry(Core.ConfigurationName, "console.window",
#if UNITY_EDITOR
            "enabled"
#else
            "disabled"
#endif
        )]
        private static string _IsEnabled {
            get => _enabled ? "enabled" : "disabled";
            set => _enabled = string.Equals(value, "enabled", StringComparison.OrdinalIgnoreCase);
        }
        private static bool _enabled = false;
        public static bool IsEnabled => _enabled;

        [ConfigurationEntry(Core.ConfigurationName, "console.window.name", "Console")]
        public static string WindowName {
            get => name;
            set => name = value;
        }
        private static string name;

        [ConfigurationEntry(Core.ConfigurationName, "console.window.width", -1)]
        public static float Width {
            get => width;
            set {
                width = value;
                UpdateRect();
            }
        }
        private static float width;

        [ConfigurationEntry(Core.ConfigurationName, "console.window.height", -1)]
        public static float Height {
            get => height;
            set {
                height = value;
                UpdateRect();
            }
        }
        private static float height;

        [ConfigurationEntry(Core.ConfigurationName, "console.echo", true)]
        public static bool Echo { get; set; }

        [ConfigurationEntry(Core.ConfigurationName, "console.application", "enabled")]
        private static string RegisterConsoleCallback {
            get => registerConsoleCallback ? "enabled" : "disabled";
            set {
                bool state = string.Equals(value, "enabled", StringComparison.OrdinalIgnoreCase);
                if (registerConsoleCallback != state) {
                    registerConsoleCallback = state;
                    Console.OnPushLogEntry -= OnConsolePushLogEntry;
                    if (state) Console.OnPushLogEntry += OnConsolePushLogEntry;
                }
            }
        }
        private static bool registerConsoleCallback = false;

        [ConfigurationEntry(Core.ConfigurationName, "console.application.log_level", "warning")]
        private static string ConsoleCallbackLogLevel {
            get => consoleCallbackLogLevel.distinctName ?? "none";
            set => consoleCallbackLogLevel = LogLevel.Parse(value);
        }
        private static LogLevel consoleCallbackLogLevel = LogLevel.Info;

        [ConfigurationEntry(Core.ConfigurationName, "console.engine", "enabled")]
        public static string RegisterApplicationLogCallback {
            get => registerApplicationLogCallback ? "enabled" : "disabled";
            set {
                bool state = string.Equals(value, "enabled", StringComparison.OrdinalIgnoreCase);
                if (registerApplicationLogCallback != state) {
                    registerApplicationLogCallback = state;
                    Application.logMessageReceived -= ApplicationLogCallback;
                    if (state) Application.logMessageReceived += ApplicationLogCallback;
                }
            }
        }
        private static bool registerApplicationLogCallback = false;

        [ConfigurationEntry(Core.ConfigurationName, "console.engine.log_level", "warning")]
        private static string ApplicationLogLevel {
            get => applicationLogLevel.ToString().ToLower();
            set {
                LogLevel logLevel = LogLevel.Parse(value);
                applicationLogLevel = logLevel.unityLogType;
                applicationLogPriority = logLevel.priority;
            }
        }
        private static LogType applicationLogLevel;
        private static int applicationLogPriority;

        [ConfigurationEntry(Core.ConfigurationName, "console.engine.stacktrace_level", "warning")]
        private static string ApplicationStacktraceLevel {
            get => applicationStacktraceLevel.ToString().ToLower();
            set {
                LogLevel logLevel = LogLevel.Parse(value);
                applicationStacktraceLevel = logLevel.unityLogType;
                applicationStacktracePriority = logLevel.priority;
            }
        }
        private static LogType applicationStacktraceLevel;
        private static int applicationStacktracePriority;

        [ConfigurationEntry(Core.ConfigurationName, "console.entry_buffer_size", DefaultEntryBufferSize)]
        public static int WindowEntryBufferSize {
            get => EntryBuffer.Length;
            set {
                if (value < 1) throw new ArgumentOutOfRangeException();
                EntryBuffer.Clear(value);
            }
        }

        [ConfigurationEntry(Core.ConfigurationName, "console.history_buffer_size", DefaultHistoryBufferSize)]
        public static int HistoryBufferSize {
            get => inputHistoryBuffer.Length;
            set {
                if (value < 1) throw new ArgumentOutOfRangeException();
                inputHistoryBuffer = new string[value];
                inputHistoryIndex = 0;
            }
        }

        public static string[] CommandHistory {
            get {
                int bufferSize = inputHistoryBuffer.Length;
                string[] buffer = new string[bufferSize];
                Array.Copy(inputHistoryBuffer, 0, buffer, 0, bufferSize);
                return buffer;
            }
        }

        #endregion

        #region logic

        #region Initialise

        internal static void Initialise() {
            if (settings == null) settings = SettingsManager.GetSettings<ConsoleWindowSettings>();
            if (settings.font == null) {
#if UNITY_EDITOR
                Debug.LogWarning("Make sure a mono-spaced font asset is assigned within `Resources/Settings/ConsoleWindowSettings`. A sample settings asset can be imported from the package manager.", settings);
#endif
                throw new NullReferenceException("settings.font");
            }
            settings.font.RequestCharactersInTexture(" ", settings.fontSize);
            if (!settings.font.GetCharacterInfo(' ', out CharacterInfo info, settings.fontSize)) throw new NotSupportedException("Invalid font.");
            fontWidth = info.advance;
        }

        #endregion

        #region Terminate

        internal static void Terminate() {
            Application.logMessageReceived -= ApplicationLogCallback;
            Console.OnPushLogEntry -= OnConsolePushLogEntry;
        }

        #endregion

        #region Update

        /// <summary>
        /// Called every frame by the <see cref="Core.Update"/> method.
        /// </summary>
        internal static void Update() {
            if (!_enabled) return;
            Keyboard keyboard = Keyboard.current; // get the current keyboard
            if (keyboard != null) { // the current keyboard is not null
                if (draw) { // the console window should be drawn
                    if (keyboard.escapeKey.wasReleasedThisFrame) { // the escape key was released
                        ControlHandle.RevokeControl(true);
                        draw = false; // stop drawing the console window
                    } else if (keyboard.enterKey.wasReleasedThisFrame) // the enter key was released
                        ExecuteInput(); // execute the input of the debug console
                    else if (keyboard.upArrowKey.wasReleasedThisFrame) // the up arrow was released
                        PreviousCommand(); // move to the previous command entered into the console window
                    else if (keyboard.downArrowKey.wasReleasedThisFrame) // the down arrow was released
                        NextCommand(); // move to the next command entered into the console window
                } else if (keyboard.slashKey.wasReleasedThisFrame) { // the console window is not currently active and the slash key was released
                    if (ControlHandle.GainControl(true)) { // gain control over the console window
                        UpdateRect();
                        draw = true; // start drawing the console window
                    }
                }
            }
        }

        #endregion

        #region UpdateRect

        private static void UpdateRect() {
            windowRect = new Rect(
                WindowMargin,
                WindowMargin,
                width <= 0.0f
                    ? Screen.width - (WindowMargin * 2)
                    : Mathf.Min(width, MaxWidth),
                height <= 0.0f
                    ? Screen.height - (WindowMargin * 2)
                    : Mathf.Min(height, MaxHeight)
            );
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw must be called from an OnGUI() method.
        /// </summary>
        internal static void Draw() {
            if (!draw || !_enabled) return;
            if (entryStyle == null) {
                entryStyle = new GUIStyle() {
                    name = "DebugConsoleEntryStyle",
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    normal = new GUIStyleState() {
                        textColor = Color.white
                    },
                    richText = true,
                    font = settings.font,
                    fontSize = settings.fontSize
                };
            }
            if (inputStyle == null) {
                inputStyle = new GUIStyle() {
                    name = "DebugConsoleInputStyle",
                    //margin = new RectOffset(fontWidth * 2, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    normal = new GUIStyleState() {
                        textColor = Color.clear
                    },
                    richText = false,
                    font = settings.font,
                    fontSize = settings.fontSize
                };
            }

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.black;
            windowRect = GUILayout.Window(WindowID, windowRect, DrawWindow, name);
        }

        #endregion

        #region DrawWindow

        private static void DrawWindow(int windowId) {

            GUILayout.BeginVertical();

            #region console

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.black;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            #region content

            lock (EntryBuffer) { // lock entry buffer

                StringBuilder entryBufferBuilder = new StringBuilder(); // create a string builder to build the content for the console window

                LogEntry entry; // used to store a reference to the current entry being processed
                bool firstElement = true; // stores if the first element has been placed yet
                for (int i = 0; i < EntryBuffer.Length; i++) { // iterate through every entry in the entry buffer

                    entry = EntryBuffer[i]; // get the current entry
                    if (entry == null) continue; // the entry is null, skip this entry

                    if (!firstElement) entryBufferBuilder.Append('\n'); // new line required
                    else firstElement = false; // this is the first element

                    entryBufferBuilder.Append(entry.FormattedRichTextEntry);
                }

                if (!firstElement) { // the first element has been placed (therefore the string builder is not empty)
                    GUILayout.TextArea( // create a text area
                        entryBufferBuilder.ToString(), // write the content of the console to the text area
                        entryStyle // use the entry style
                    );
                }

            }

            #endregion

            #region input

            GUI.SetNextControlName("DebugConsoleTextField");
            if (input == null) input = string.Empty; // ensure input is not null
            StringBuilder inputPreviewBuilder = new StringBuilder(input.Length).Append($"<color=#{ConsoleColour.DarkGray}>></color> ");
            GUILayout.Label(DecorateCommand(input, inputPreviewBuilder), entryStyle);
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.x += fontWidth * 2;
            input = GUI.TextField(rect, input, inputStyle).TrimStart();

            #endregion

            GUILayout.EndScrollView();

            #endregion

            GUILayout.EndVertical();

            #region drag window

            GUI.DragWindow(TitleBarRect); // make the window draggable

            if (focus) {
                GUI.FocusWindow(windowId);
                GUI.FocusControl("DebugConsoleTextField");
                focus = false;
            }

            #endregion

        }

        #endregion

        #region PreviousCommand

        private static void PreviousCommand() {
            if (input != null && input.Equals(inputHistoryBuffer[inputHistoryIndex])) { // move to previous index
                if (++inputHistoryIndex >= inputHistoryBuffer.Length) inputHistoryIndex = inputHistoryBuffer.Length - 1;
                else {
                    string value = inputHistoryBuffer[inputHistoryIndex];
                    if (value == null) inputHistoryIndex--; // return to last value
                    else input = value; // set the input to the next value
                }
            } else input = inputHistoryBuffer[inputHistoryIndex];
        }

        #endregion

        #region NextCommand

        private static void NextCommand() {
            if (input != null && input.Equals(inputHistoryBuffer[inputHistoryIndex])) { // move to next index
                if (inputHistoryIndex <= 0) {
                    inputHistoryIndex = 0;
                    input = string.Empty;
                } else input = inputHistoryBuffer[--inputHistoryIndex];
            } else input = inputHistoryBuffer[inputHistoryIndex];
        }

        #endregion

        #region DecorateCommand

        public static string DecorateCommand(string command, in StringBuilder stringBuilder) {

            if (command == null) throw new ArgumentNullException(nameof(command));
            if (stringBuilder == null) throw new ArgumentNullException(nameof(stringBuilder));

            command = command.TrimStart();
            int commandLength = command.Length;
            if (commandLength == 0) return stringBuilder.ToString();

            int internalState = 0;
            int lastIndex = 0;

            for (int i = 0; i < commandLength; i++) {

                char c = command[i];
                bool end = i == commandLength - 1;

                switch (internalState) {
                    case 0: { // start of command
                        if (c == ' ' || end) {
                            string cmd = (end ? command : command.Substring(0, i)).TrimEnd();
                            stringBuilder.Append($"<color=#");
                            stringBuilder.Append(Console.GetCommand(cmd) != null ? ConsoleColour.Blue.hex : ConsoleColour.Red.hex);
                            stringBuilder.Append('>');
                            stringBuilder.Append(ConsoleUtility.Escape(cmd));
                            if (!end) {
                                stringBuilder.Append("</color> <color=#");
                                stringBuilder.Append(ConsoleColour.Yellow.hex);
                                stringBuilder.Append('>');
                                internalState = 1;
                            }
                        }
                        break;
                    }
                    case 1: { // processing start of new argument
                        if (c == ' ') {
                            stringBuilder.Append(' ');
                        } else if (c == '"') {
                            lastIndex = i + 1;
                            if (end) {
                                stringBuilder.Append("</color><color=#");
                                stringBuilder.Append(ConsoleColour.Orange.hex);
                                stringBuilder.Append(">\"");
                            } else {
                                internalState = 3;
                            }
                        } else {
                            lastIndex = i;
                            internalState = 2;
                            goto case 2;
                        }
                        break;
                    }
                    case 2: { // processing standard argument
                        if (c == ' ' || end) {
                            stringBuilder.Append(ConsoleUtility.Escape(end ? command.Substring(lastIndex) : command.Substring(lastIndex, i - lastIndex)));
                            if (!end) stringBuilder.Append(' ');
                            //lastIndex = i + 1;
                            internalState = 1;
                        }
                        break;
                    }
                    case 3: { // processing a quote
                        if (c == '"' || end) { // last character of the quote, it doesnt matter what this is, it will be the colour of a quote
                            stringBuilder.Append("</color><color=#");
                            stringBuilder.Append(ConsoleColour.Orange.hex);
                            stringBuilder.Append(">\"");
                            if (end) {
                                stringBuilder.Append(ConsoleUtility.Escape(command.Substring(lastIndex)));
                            } else {
                                stringBuilder.Append(ConsoleUtility.Escape(command.Substring(lastIndex, i - lastIndex + 1)));
                                stringBuilder.Append("</color><color=#");
                                stringBuilder.Append(ConsoleColour.Yellow.hex);
                                stringBuilder.Append('>');
                                //lastIndex = i + 1;
                                internalState = 1;
                            }
                        } else if (c == '\\') { // escape character
                            i++; // skip the next character
                            if (i == commandLength - 1) {
                                stringBuilder.Append("</color><color=#");
                                stringBuilder.Append(ConsoleColour.Orange.hex);
                                stringBuilder.Append(">\"");
                                stringBuilder.Append(ConsoleUtility.Escape(command.Substring(lastIndex)));
                            }
                        }
                        break;
                    }
                }
            }

            stringBuilder.Append("</color>");
            return stringBuilder.ToString();

        }

        #endregion

        #region ExecuteInput

        private static void ExecuteInput() {
            focus = true;
            if (!string.IsNullOrWhiteSpace(input)) {
                inputHistoryBuffer.ShiftRight();
                inputHistoryBuffer[0] = input;
                inputHistoryIndex = 0; // reset history index
                scrollPosition.y = float.MaxValue;
                string displayCommand = DecorateCommand(input, new StringBuilder());
                LogEntry echoEntry = null;
                if (Echo) echoEntry = Print($"<color=#{ConsoleColour.Green.hex}>></color> " + displayCommand);
                try {
                    if (!Console.Execute(input) && echoEntry != null) { // execute command
                        EntryBuffer.Replace( // execution failed, change colour of echo message
                            echoEntry,
                            new LogEntry($"<color=#{ConsoleColour.Red.hex}>></color> " + displayCommand)
                        );
                    }
                } catch (Exception exception) {
                    ConsoleFormatter.Error("Failed to execute command.", exception);
                }
            }
            input = string.Empty;
        }

        #endregion

        #region Print

        public static LogEntry Print(in string message) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            LogEntry entry = new LogEntry(message);
            lock (EntryBuffer) EntryBuffer.Push(entry, out _);
            return entry;
        }

        public static LogEntry[] Print(in string[] messages) {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            int messageCount = messages.Length;
            LogEntry[] entryBuffer = new LogEntry[messageCount];
            if (messageCount > 0) {
                LogEntry entry;
                lock (EntryBuffer) {
                    for (int i = 0; i < messageCount; i++) {
                        entry = new LogEntry(messages[i]);
                        entryBuffer[i] = entry;
                        EntryBuffer.Push(entry, out _);
                    }
                }
            }
            return entryBuffer;
        }

        #endregion

        #region PrintTable

        /// <summary>
        /// Prints a table to the debug console.
        /// </summary>
        /// <param name="elements">Elements where the first index is the column index, then the row index.</param>
        /// <param name="header">When <c>true</c>, the first row will be highlighted.</param>
        /// <param name="inverted">When <c>true</c>, rows and columns will be swapped.</param>
        /// <param name="spacing">Number of spaces to place between columns.</param>
        public static void PrintTable(string[,] elements, in bool header = false, in bool inverted = false, in int spacing = 3) {

            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (spacing < 0) throw new ArgumentOutOfRangeException(string.Concat(nameof(spacing), " must be positive."));

            int columns = elements.GetLength(0);
            if (columns == 0) return;

            int rows = elements.GetLength(1);
            if (rows == 0) return;

            if (inverted) { // invert the elements
                string[,] invertedElements = new string[rows, columns];
                for (int c = 0; c < columns; c++) {
                    for (int r = 0; r < rows; r++) {
                        invertedElements[r, c] = elements[c, r];
                    }
                }
                elements = invertedElements;
                int temp = rows;
                rows = columns;
                columns = temp;
            }

            int finalColumnIndex = columns - 1; // get the index of the final column
            int[] columnSizes = new int[finalColumnIndex]; // width to make each column
            string[][][] cells = new string[columns][][]; // create array to track all columns, rows, and lines
            int[][][] cellLineLengths = new int[finalColumnIndex][][]; // width of each line
            int[] rowLineCount = new int[rows]; // track the number of lines per row

            string[][] row;
            int[][] rowLengths = null;
            for (int c = 0; c < columns; c++) { // iterate each column
                row = new string[rows][]; // create row
                cells[c] = row; // assign the row to the column
                if (c < finalColumnIndex) {
                    rowLengths = new int[rows][]; // create row lengths
                    cellLineLengths[c] = rowLengths; // assign the row lengths to the line lengths
                }
                int maximumColumnSize = 0; // track the largest size of the column
                for (int r = 0; r < rows; r++) { // iterate each row
                    string cell = elements[c, r]; // get the current value of this cell
                    if (cell != null) { // cell is not null
                        int cellSize = 0; // maximum size of the cell (this is equal to the length of the longest line)
                        string[] lines = cell.Split('\n'); // split into each line
                        int lineCount = lines.Length; // get the number of lines in the row
                        if (lineCount > rowLineCount[r]) // exceeds the line count for the row
                            rowLineCount[r] = lineCount; // override the line count for the row
                        int[] lineLengths = new int[lineCount];
                        row[r] = lines; // assign the lines to the row
                        if (c < finalColumnIndex) rowLengths[r] = lineLengths;
                        for (int l = 0; l < lineCount; l++) { // iterate each line
                            int length = ConsoleUtility.RemoveFormatting(lines[l]).Length; // get the length of the line
                            lineLengths[l] = length; // assign the length to the line lengths
                            if (length > cellSize) // the length of this line is greater than the current greatest length
                                cellSize = length; // override the cell size with the new longest length
                        }
                        if (cellSize > maximumColumnSize) maximumColumnSize = cellSize; // new maximum column size
                    } else { // cell is null, there is no content for this row
                        row[r] = new string[0]; // create empty cell
                    }
                }
                if (c < finalColumnIndex) columnSizes[c] = maximumColumnSize + spacing; // store maximum column size
            }

            lock (EntryBuffer) { // lock on the entity buffer
                for (int r = 0; r < rows; r++) { // iterate each row
                    int lines = rowLineCount[r]; // get the number of lines for this row
                    if (lines == 0) { // row is empty
                        EntryBuffer.Push(LogEntry.Empty, out _); // add empty line
                    } else { // row is not empty
                        int[] lengths = null;
                        int columnWidth = 0;
                        for (int l = 0; l < lines; l++) { // iterate each of the lines for this row
                            StringBuilder lineBuilder = new StringBuilder(); // create a new string builder to build the line
                            if (header && r == 0) lineBuilder.Append("<b>"); // create bold text if this is the header row
                            for (int c = 0; c < columns; c++) { // iterate each column
                                string[] cell = cells[c][r]; // get a reference to the current cell
                                int cellLineCount = cell.Length; // get the number of lines that make up this cell
                                if (c < finalColumnIndex) {
                                    lengths = cellLineLengths[c][r]; // get lengths of each line
                                    columnWidth = columnSizes[c]; // get the width of the column
                                }
                                if (l < cellLineCount) { // a line exists in the cell for the line index
                                    string line = cell[l]; // get the value of the line
                                    if (line != null && line.Length > 0) { // this line has content
                                        lineBuilder.Append(line); // write the value of the cell to the line builder
                                        if (c < finalColumnIndex) lineBuilder.Append(' ', columnWidth - lengths[l]); // apply spacing between cells
                                    } else if (c < finalColumnIndex) lineBuilder.Append(' ', columnWidth); // create blank line
                                } else if (c < finalColumnIndex) lineBuilder.Append(' ', columnWidth); // there is no content for this line, create a blank line
                            }
                            if (header && r == 0) lineBuilder.Append("</b>"); // close the bold tag if this is the header row
                            EntryBuffer.Push(new LogEntry(lineBuilder.ToString()), out _); // push the line to the entry buffer
                        }
                    }
                }
            }
        }

        public static void PrintTable(in string[] rows, in char splitCharacter, in bool header = false, in int spacing = 3) {

            if (rows == null) throw new ArgumentNullException(nameof(rows));

            int rowCount = rows.Length;
            if (rowCount == 0) return;

            int columnCount = -1;

            string[,] elements = null;

            string row;
            string[] values;
            for (int r = 0; r < rowCount; r++) {

                row = rows[r];
                if (row == null) continue;

                values = row.Split(splitCharacter);
                if (columnCount == -1) {
                    columnCount = values.Length;
                    elements = new string[columnCount, rowCount];
                } else if (values.Length != columnCount) throw new ArgumentException(string.Concat(nameof(rows), " doesn't have a consistent number of columns."));

                for (int c = 0; c < columnCount; c++) elements[c, r] = values[c].Trim();

            }

            if (elements != null)
                PrintTable(elements, header, false, spacing);

        }

        #endregion

        #region OnConsolePushLogEntry

        private static void OnConsolePushLogEntry(LogEntry entry) {
            if (entry.logLevel.priority < consoleCallbackLogLevel.priority) return;
            lock (EntryBuffer) {
                EntryBuffer.Push(entry, out _);
            }
        }

        #endregion

        #region ApplicationLogCallback

        private static void ApplicationLogCallback(string message, string stacktrace, LogType type) {
            LogLevel logLevel = type.ToLogLevel();
            int priority = logLevel.priority;
            if (priority < applicationLogPriority) return;
            lock (EntryBuffer) {
                EntryBuffer.Push(
                    new LogEntry(
                        logLevel,
                        priority < applicationStacktracePriority || stacktrace.IsNullOrWhitespace()
                            ? message.Trim()
                            : $"{message.Trim()}\n{stacktrace.Trim()}"
                    ),
                    out _
                );
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the debug console.
        /// </summary>
        public static void Clear() {
            lock (EntryBuffer) EntryBuffer.Clear();
        }

        #endregion

        #region ClearCommandHistory

        public static void ClearCommandHistory() {
            for (int i = inputHistoryBuffer.Length - 1; i >= 0; i--) inputHistoryBuffer[i] = null;
            inputHistoryIndex = 0;
        }

        #endregion

        #endregion

    }

}