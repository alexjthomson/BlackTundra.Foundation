using System;
using System.Text;

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using BlackTundra.Foundation.Control;
#endif
using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Logging;
using BlackTundra.Foundation.Collections.Generic;

namespace BlackTundra.Foundation {

    public sealed class ConsoleWindow
#if ENABLE_INPUT_SYSTEM
        : IControllable
#endif
        {

        #region constant

        public const int MaxWidth = 8192;
        public const int MaxHeight = 4096;

        private const int WindowMargin = 16;
        private const int TitleBarHeight = 16;

        #endregion

        #region nested

        #endregion

        #region variable

        public readonly string name;

        private readonly RollingBuffer<LogEntry> entryBuffer;
        private readonly bool echo;

        private readonly string[] inputHistoryBuffer;
        private int inputHistoryIndex;

        private readonly int windowId;
        private Rect windowRect;
        private readonly Rect titleBarRect;

        private Vector2 scrollPosition;
        private string input;

        private readonly int fontWidth;

        private GUIStyle entryStyle = null;
        private GUIStyle inputStyle = null;

        private bool focus = true;

        private static int rollingWindowId = 100000;
        private static ConsoleWindowSettings settings = null;

        #endregion

        #region property

        public string[] CommandHistory {
            get {
                int bufferSize = inputHistoryBuffer.Length;
                string[] buffer = new string[bufferSize];
                Array.Copy(inputHistoryBuffer, buffer, bufferSize);
                return buffer;
            }
        }

        #endregion

        #region constructor

        public ConsoleWindow(in string name, in Vector2 windowSize, in bool echo = true, in bool registerApplicationLogCallback = false, in int capacity = 128, in int inputHistoryCapcity = 32) {

            this.name = name ?? throw new ArgumentNullException("name");
            entryBuffer = new RollingBuffer<LogEntry>(capacity, true);
            this.echo = echo;

            inputHistoryBuffer = new string[inputHistoryCapcity];
            inputHistoryIndex = 0;

            windowId = rollingWindowId++;
            SetWindowSize(windowSize.x, windowSize.y);
            titleBarRect = new Rect(0, 0, MaxWidth, TitleBarHeight);

            scrollPosition = Vector2.zero;
            input = string.Empty;

            entryStyle = null;

            Console.OnPushLogEntry += OnConsolePushLogEntry;
            if (registerApplicationLogCallback) Application.logMessageReceived += ApplicationLogCallback;

            if (settings == null) settings = SettingsManager.GetSettings<ConsoleWindowSettings>();

            if (settings.font == null) throw new NullReferenceException("settings.font");
            settings.font.RequestCharactersInTexture(" ", settings.fontSize);
            if (!settings.font.GetCharacterInfo(' ', out CharacterInfo info, settings.fontSize)) throw new NotSupportedException("Invalid font.");
            fontWidth = info.advance;

        }

        #endregion

        #region destructor

        ~ConsoleWindow() {

            Application.logMessageReceived -= ApplicationLogCallback;
            Console.OnPushLogEntry -= OnConsolePushLogEntry;

        }

        #endregion

        #region logic

        #region SetWindowSize

        public void SetWindowSize(in float width, in float height) {

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
        public void Draw() {

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

            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, name);

        }

        #endregion

        #region DrawWindow

        private void DrawWindow(int windowId) {

            GUILayout.BeginVertical();

            #region console

            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.black;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            #region content

            lock (entryBuffer) { // lock entry buffer

                StringBuilder entryBufferBuilder = new StringBuilder(); // create a string builder to build the content for the console window

                LogEntry entry; // used to store a reference to the current entry being processed
                bool firstElement = true; // stores if the first element has been placed yet
                for (int i = 0; i < entryBuffer.Length; i++) { // iterate through every entry in the entry buffer

                    entry = entryBuffer[i]; // get the current entry
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

            GUI.DragWindow(titleBarRect); // make the window draggable

            if (focus) {
                GUI.FocusWindow(windowId);
                GUI.FocusControl("DebugConsoleTextField");
                focus = false;
            }

            #endregion

        }

        #endregion

        #region PreviousCommand

        public void PreviousCommand() {
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

        public void NextCommand() {
            if (input != null && input.Equals(inputHistoryBuffer[inputHistoryIndex])) { // move to next index
                if (inputHistoryIndex <= 0) {
                    inputHistoryIndex = 0;
                    input = string.Empty;
                } else input = inputHistoryBuffer[--inputHistoryIndex];
            } else input = inputHistoryBuffer[inputHistoryIndex];
        }

        #endregion

        #region DecorateCommand

        public string DecorateCommand(string command, in StringBuilder stringBuilder) {

            if (command == null) throw new ArgumentNullException("command");
            if (stringBuilder == null) throw new ArgumentNullException("stringBuilder");

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

        public void ExecuteInput() {
            focus = true;
            if (!string.IsNullOrWhiteSpace(input)) {
                inputHistoryBuffer.ShiftRight();
                inputHistoryBuffer[0] = input;
                inputHistoryIndex = 0; // reset history index
                scrollPosition.y = float.MaxValue;
                string displayCommand = DecorateCommand(input, new StringBuilder());
                LogEntry echoEntry = null;
                if (echo) echoEntry = Print($"<color=#{ConsoleColour.Green.hex}>></color> " + displayCommand);
                try {
                    if (!Console.Execute(input) && echoEntry != null) { // execute command
                        entryBuffer.Replace( // execution failed, change colour of echo message
                            echoEntry,
                            new LogEntry($"<color=#{ConsoleColour.Red.hex}>></color> " + displayCommand)
                        );
                    }
                } catch (Exception exception) {
                    exception.Handle();
                }
            }
            input = string.Empty;
        }

        #endregion

        #region Print

        public LogEntry Print(in string message) {
            if (message == null) throw new ArgumentNullException("message");
            LogEntry entry = new LogEntry(message);
            lock (entryBuffer) entryBuffer.Push(entry, out _);
            return entry;
        }

        public LogEntry[] Print(in string[] messages) {
            if (messages == null) throw new ArgumentNullException("messages");
            int messageCount = messages.Length;
            LogEntry[] entryBuffer = new LogEntry[messageCount];
            if (messageCount > 0) {
                LogEntry entry;
                lock (this.entryBuffer) {
                    for (int i = 0; i < messageCount; i++) {
                        entry = new LogEntry(messages[i]);
                        entryBuffer[i] = entry;
                        this.entryBuffer.Push(entry, out _);
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
        public void PrintTable(string[,] elements, in bool header = false, in bool inverted = false, in int spacing = 3) {

            if (elements == null) throw new ArgumentNullException("elements");
            if (spacing < 0) throw new ArgumentOutOfRangeException("spacing must be positive.");
            
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

            lock (entryBuffer) { // lock on the entity buffer
                for (int r = 0; r < rows; r++) { // iterate each row
                    int lines = rowLineCount[r]; // get the number of lines for this row
                    if (lines == 0) { // row is empty
                        entryBuffer.Push(LogEntry.Empty, out _); // add empty line
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
                            entryBuffer.Push(new LogEntry(lineBuilder.ToString()), out _); // push the line to the entry buffer
                        }
                    }
                }
            }
        }

        public void PrintTable(in string[] rows, in char splitCharacter, in bool header = false, in int spacing = 3) {

            if (rows == null) throw new ArgumentNullException("rows");

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
                } else if (values.Length != columnCount) throw new ArgumentException("rows doesn't have a consistent number of columns.");

                for (int c = 0; c < columnCount; c++) elements[c, r] = values[c].Trim();

            }

            if (elements != null)
                PrintTable(elements, header, false, spacing);

        }

        #endregion

        #region OnConsolePushLogEntry

        private void OnConsolePushLogEntry(LogEntry entry) {
            lock (entryBuffer) {
                entryBuffer.Push(entry, out _);
            }
        }

        #endregion

        #region ApplicationLogCallback

        private void ApplicationLogCallback(string message, string stacktrace, LogType type) {
            lock (entryBuffer) {
                entryBuffer.Push(
                    new LogEntry(
                        type.ToLogLevel(),
                        stacktrace.IsNullOrWhitespace() ? message.Trim() : $"{message.Trim()}\n{stacktrace.Trim()}"
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
        public void Clear() {
            lock (entryBuffer) entryBuffer.Clear();
        }

        #endregion

        #region ClearCommandHistory

        public void ClearCommandHistory() {
            for (int i = inputHistoryBuffer.Length - 1; i >= 0; i--) inputHistoryBuffer[i] = null;
            inputHistoryIndex = 0;
        }

        #endregion

        #region OnControlGained
#if ENABLE_INPUT_SYSTEM

        public ControlFlags OnControlGained(in ControlUser user) {
            focus = true;
            return ControlFlags.None;
        }

#endif
        #endregion

        #region OnControlRevoked
#if ENABLE_INPUT_SYSTEM

        public ControlFlags OnControlRevoked(in ControlUser user) => ControlUser.ControlFlags;

#endif
        #endregion

        #endregion

    }

}