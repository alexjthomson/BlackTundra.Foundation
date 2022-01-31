using BlackTundra.Foundation.Utility;

using System;

namespace BlackTundra.Foundation.Logging {

    /// <summary>
    /// Acts as a buffer that stores a fixed number <see cref="LogEntry"/> instances before invoking a
    /// callback for the entries to be handled.
    /// </summary>
    /// <seealso cref="LogBufferFullDelegate"/>
    /// <seealso cref="Push(in LogLevel, in string)"/>
    /// <seealso cref="Dispose"/>
    public sealed class Logger : IDisposable {

        #region constant

        /// <summary>
        /// Maximum capacity of the internal <see cref="LogEntry"/> buffer of a <see cref="Logger"/>.
        /// </summary>
        public const int MaxCapacity = 255;

        /// <summary>
        /// Minimum capacity of the internal <see cref="LogEntry"/> buffer of a <see cref="Logger"/>.
        /// </summary>
        public const int MinCapacity = 1;

        #endregion

        #region delegate

        /// <summary>
        /// Delegate used when the <see cref="logBuffer"/> is full and needs to be cleared.
        /// This delegate should handle all of the <see cref="LogEntry"/> instances in the buffer
        /// passed into it since they will be removed from the buffer after the delegate has
        /// been called.
        /// </summary>
        internal delegate void LogBufferFullDelegate(in Logger logger, in LogEntry[] buffer);

        /// <summary>
        /// Delegate used when the <see cref="Logger"/> is cleared.
        /// </summary>
        internal delegate void ClearLoggerDelegate(in Logger logger);

        #endregion

        #region variable

        /// <summary>
        /// Name of the <see cref="Logger"/>.
        /// </summary>
        public readonly string name;

        /// <summary>
        /// Context the <see cref="Logger"/> was created with.
        /// </summary>
        private readonly Type context;

        /// <summary>
        /// Maximum capacity of the <see cref="Logger"/> before it must be emptied.
        /// </summary>
        public readonly int capacity;

        /// <summary>
        /// Every <see cref="LogEntry"/> pushed to the logger.
        /// </summary>
        private readonly LogEntry[] logBuffer;

        /// <summary>
        /// Tracks the next index that a log entry should be inserted at.
        /// </summary>
        private int logIndex;

        /// <summary>
        /// Lock used to ensure accessing the <see cref="logBuffer"/> is thread safe.
        /// </summary>
        private object _logBufferLock;

        /// <summary>
        /// Called when <see cref="logBuffer"/> is full.
        /// See delegate definition for more information.
        /// </summary>
        /// <seealso cref="LogBufferFullDelegate"/>
        private readonly LogBufferFullDelegate logBufferFullCallback;

        /// <summary>
        /// Called when the <see cref="Logger"/> is cleared.
        /// </summary>
        private readonly ClearLoggerDelegate clearLoggerCallback;

        #endregion

        #region property

        /// <summary>
        /// Gets the current number of <see cref="LogEntry"/> instances inside the <see cref="Logger"/> buffer.
        /// </summary>
        public int Count {
            get {
                lock (_logBufferLock) {
                    return logIndex;
                }
            }
        }

        /// <summary>
        /// Gets the context in which the <see cref="Logger"/> exists.
        /// This can be <c>null</c> if the <see cref="Logger"/> is the root logger.
        /// </summary>
        public Type Context => context == typeof(LogManager) ? null : context;

        /// <summary>
        /// Returns <c>true</c> if the <see cref="Logger"/> is the root logger.
        /// </summary>
        public bool IsRootLogger => context == typeof(LogManager);

        public LogLevel LogLevel { get; set; }

        #endregion

        #region event

        /// <summary>
        /// Invoked when a new <see cref="LogEntry"/> is pushed to the <see cref="Logger"/>.
        /// </summary>
        public event Action<LogEntry> OnPushLogEntry;

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a new <see cref="Logger"/>.
        /// </summary>
        /// <param name="context">Context in which the <see cref="Logger"/> exists.</param>
        /// <param name="name">Name of the <see cref="Logger"/>.</param>
        /// <param name="capacity">Size of the internal <see cref="LogEntry"/> buffer.</param>
        /// <param name="logBufferFullCallback">
        /// Callback called when the <see cref="Logger"/> buffer is full or needs to be emptied.
        /// This callback is responsible for processing/handling the log buffer data before it
        /// is cleared.
        /// </param>
        internal Logger(
            in Type context,
            in string name,
            in int capacity,
            in LogLevel logLevel,
            in LogBufferFullDelegate logBufferFullCallback,
            in ClearLoggerDelegate clearLoggerCallback
        ) {
            if (context == null) throw new ArgumentNullException(nameof(context));
            //if (name == null) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
            if (capacity < MinCapacity || capacity > MaxCapacity) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (logBufferFullCallback == null) throw new ArgumentNullException(nameof(logBufferFullCallback));
            if (clearLoggerCallback == null) throw new ArgumentNullException(nameof(clearLoggerCallback));

            this.name = name;
            this.context = context;
            this.capacity = capacity;
            this.logBufferFullCallback = logBufferFullCallback;
            this.clearLoggerCallback = clearLoggerCallback;

            LogLevel = logLevel;
            logBuffer = new LogEntry[capacity];
            logIndex = 0;
            _logBufferLock = new object();

        }

        #endregion

        #region logic

        #region Push

        /// <summary>
        /// Pushes a <see cref="LogEntry"/> to the <see cref="Logger"/>.
        /// This will add the entry to an entry buffer. When the buffer is full, a callback
        /// will be invoked (<see cref="logBufferFullCallback"/>), and the buffer will be cleared.
        /// </summary>
        /// <param name="logLevel"><see cref="Logging.LogLevel"/> to mark the message with.</param>
        /// <param name="content">Content to push to the <see cref="Logger"/></param>
        public void Push(in LogLevel logLevel, in string content) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            LogEntry logEntry = new LogEntry(logLevel, DateTime.Now, content);
            Push(logEntry);
        }

        /// <summary>
        /// Pushes a <see cref="LogEntry"/> to the <see cref="Logger"/>.
        /// This will add the entry to an entry buffer. When the buffer is full, a callback
        /// will be invoked (<see cref="logBufferFullCallback"/>), and the buffer will be cleared.
        /// </summary>
        /// <param name="entry"><see cref="LogEntry"/> to push to the <see cref="Logger"/>.</param>
        /// <seealso cref="Push(in LogLevel, in string)"/>
        private void Push(in LogEntry entry) {
            if (ShouldPush(entry.logLevel)) {
                lock (_logBufferLock) { // lock the log buffer from being accessed
                    logBuffer[logIndex++] = entry; // insert the entry into the log buffer at the current log index and increment the log index
                    if (logIndex == capacity) { // if the log buffer is full, clear it
                        try {
                            logBufferFullCallback(this, logBuffer); // call the log buffer full callback
                        } catch (Exception exception) {
                            exception.Handle($"An unhandled exception occurred while clearing the log buffer for the \"{name}\" logger."); // handle any exception that occurs
                        }
                        Array.Clear(logBuffer, 0, capacity); // clear the log buffer array
                        logIndex = 0; // reset the log index to point to the first element of the log buffer array
                    }
                }
            }
            if (OnPushLogEntry != null) {
                try {
                    OnPushLogEntry.Invoke(entry);
                } catch (Exception exception) {
                    exception.Handle($"An unhandled exception occurred while invoking `{nameof(OnPushLogEntry)}` for the `{name}` logger.");
                }
            }
        }

        #endregion

        #region ShouldPush

        /// <returns>
        /// Returns <c>true</c> if the <see cref="Logger"/> should push a log entry with the specified <paramref name="logLevel"/>.
        /// </returns>
        public bool ShouldPush(in LogLevel logLevel) => logLevel.priority >= LogLevel.priority || LogLevel.None.Equals(logLevel);

        #endregion

        #region Flush

        /// <summary>
        /// Disposes of any unhandled <see cref="LogEntry"/> instances in the log buffer.
        /// </summary>
        public void Flush() {
            lock (_logBufferLock) {
                if (logIndex > 0) { // there are entries inside the logger
                    // create buffer for callback to process:
                    LogEntry[] buffer = new LogEntry[logIndex];
                    Array.Copy(logBuffer, 0, buffer, 0, logIndex);
                    // invoke callback:
                    try {
                        logBufferFullCallback(this, buffer); // attempt to handle the remaining elements in the logger
                    } catch (Exception exception) { // catch any unhandled exceptions
                        exception.Handle($"An unhandled exception occurred while handling all unhandled log entries while disposing logger \"{name}\"."); // handle
                    }
                    // clear log buffer:
                    Array.Clear(logBuffer, 0, capacity); // ensure the log buffer is cleared
                }
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes of any unhandled <see cref="LogEntry"/> instances in the log buffer.
        /// </summary>
        public void Dispose() {
            Flush();
        }

        #endregion

        #region Clear

        /// <summary>
        /// Clears the <see cref="Logger"/> <see cref="logBuffer"/> and executes the clear callback.
        /// </summary>
        public void Clear() {
            lock (_logBufferLock) {
                if (logIndex > 0) {
                    Array.Clear(logBuffer, 0, logIndex);
                    logIndex = 0;
                }
                try {
                    clearLoggerCallback.Invoke(this);
                } catch (Exception exception) {
                    exception.Handle($"An unhandled exception occurred while invoking `{nameof(clearLoggerCallback)}` for the `{name}` logger.");
                }
            }
        }

        #endregion

        #endregion

    }

}