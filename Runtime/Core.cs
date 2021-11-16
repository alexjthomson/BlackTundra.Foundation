#region namespace defines

// SYSTEM:

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

// ENGINE:

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

// FOUNDATION:

using BlackTundra.Foundation.Collections;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;

#if ENABLE_INPUT_SYSTEM
using BlackTundra.Foundation.Control;
#endif

// DEFINES:

using Object = UnityEngine.Object;
using Colour = BlackTundra.Foundation.ConsoleColour;
using System.Collections;
using System.Linq;
using BlackTundra.Foundation.Collections.Generic;

#endregion

namespace BlackTundra.Foundation {

    #region Core

    public static class Core {

        #region constant

        /// <summary>
        /// Name of the <see cref="Core"/> configuration object.
        /// </summary>
        private const string ConfigurationName = "core";

        /// <summary>
        /// Original value of <see cref="Time.fixedDeltaTime"/> on startup.
        /// </summary>
        internal static readonly float DefaultFixedDeltaTime = Time.fixedDeltaTime;

        /// <summary>
        /// <see cref="Queue{T}"/> used to enqueue methods to be executed on the main thread.
        /// </summary>
        private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

        #endregion

        #region delegate

        /// <summary>
        /// Used to call methods that should be called every frame.
        /// </summary>
        private delegate void UpdateDelegate();

        /// <summary>
        /// Used to call methods that should be called every frame and require the difference in time since the last frame.
        /// </summary>
        private delegate void UpdateDeltaTimeDelegate(float deltaTime);

        #endregion

        #region nested

        /// <summary>
        /// Describes the phase the <see cref="Core"/> is currently in.
        /// </summary>
        internal enum CorePhase : int {
            
            /// <summary>
            /// While the <see cref="Core"/> is <see cref="Idle"/>, it has not yet had <see cref="Initialise"/> called.
            /// </summary>
            Idle = 0,

            /// <summary>
            /// Stage 1 of the initialisation sequence.
            /// </summary>
            Init_Stage1 = 1,

            /// <summary>
            /// Stage 2 of the initialisation sequence.
            /// </summary>
            Init_Stage2 = 2,

            /// <summary>
            /// Stage 3 of the initialisation sequence.
            /// </summary>
            Init_Stage3 = 3,

            /// <summary>
            /// While the <see cref="Core"/> is <see cref="Running"/>, it has had <see cref="Initialise"/> called.
            /// </summary>
            Running = 4,

            /// <summary>
            /// While the <see cref="Core"/> is <see cref="Shutdown"/>, it is no longer active and will not run again.
            /// The application should terminate at this state.
            /// </summary>
            Shutdown = 5
        }

        #endregion

        #region variable

        private static CoreInstance instance;

        /// <summary>
        /// Phase that the <see cref="Core"/> is currently in.
        /// </summary>
        internal static CorePhase phase = CorePhase.Idle;

        /// <summary>
        /// <see cref="ConsoleWindow"/> instance.
        /// </summary>
        internal static ConsoleWindow consoleWindow = null;

        /// <summary>
        /// Tracks if the <see cref="consoleWindow"/> should be drawn or not.
        /// </summary>
        private static bool drawConsoleWindow = false;

        /// <summary>
        /// Object used to ensure all methods execute in the correct order.
        /// </summary>
        private static object coreLock = new object();

        /// <summary>
        /// Array of <see cref="UpdateDelegate"/> callbacks that should be called every frame.
        /// </summary>
        private static UpdateDelegate[] updateCallbacks;

        /// <summary>
        /// Array of <see cref="UpdateDeltaTimeDelegate"/> callbacks that should be called every frame.
        /// </summary>
        private static UpdateDeltaTimeDelegate[] updateDeltaTimeCallbacks;

        #endregion

        #region property

        public static Version Version { get; private set; } = Version.Invalid;

        /// <inheritdoc cref="consoleWindow"/>
        public static ConsoleWindow ConsoleWindow => consoleWindow;

        #endregion

        #region logic

        #region InitialisePostAssembliesLoaded

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#pragma warning disable IDE0051 // remove unused private members
        private static void InitialisePostAssembliesLoaded() {
#pragma warning restore IDE0051 // remove unused private members
            lock (coreLock) {

                if (phase != CorePhase.Idle) return;
                phase = CorePhase.Init_Stage1;
                string separator = new string('-', 64);
                Console.Empty(string.Concat(separator, ' ', DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), ' ', separator));
                Console.Trace("[Core] Init (1/3) STARTED.");

                #region bind play mode shutdown hook
#if UNITY_EDITOR
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
                #endregion

                #region find version
                try {
                    Version = Version.Parse(Application.version);
                } catch (FormatException exception) {
#if UNITY_EDITOR
                    Debug.LogWarning("Make sure the project version is formatted correctly: \"{major}.{minor}.{release}{release_type}\".", instance);
#endif
                    Quit(QuitReason.CoreSelfQuit, $"Failed to parse application version (version: \"{Application.version}\").", exception, true);
                    throw exception;
                }
                Console.Info(string.Concat("[Core] Version: ", Version.ToString()));
                #endregion

                FileSystem.Initialise(); // initialise file system
                Configuration configuration = FileSystem.LoadConfiguration(ConfigurationName); // load core configuration

                #region initialise console window

                if (configuration.ForceGet("console.window.enabled", false)) { // check if the in-game console window is enabled
                    try {
                        consoleWindow = new ConsoleWindow(
                            configuration.ForceGet("console.window.name", "Console"),
                            new Vector2(
                                configuration.ForceGet("console.window.width", -1.0f),
                                configuration.ForceGet("console.window.height", -1.0f)
                            ),
                            configuration.ForceGet("console.window.echo", true),
                            configuration.ForceGet("console.window.register_application_log_callback", true),
                            configuration.ForceGet("console.window.buffer_size", 256),
                            configuration.ForceGet("console.window.history_buffer_size", 32)
                        );
                    } catch (Exception exception) {
                        Quit(QuitReason.FatalCrash, "Failed to construct core console window.", exception, true);
                        return;
                    }
                    #region bind commands

                    // find delegate parameter types:
                    Type[] targetTypes = ObjectUtility.GetDelegateParameterTypes<Console.Command.CommandCallbackDelegate>();

                    // iterate console commands:
                    IEnumerable<MethodInfo> methods = ObjectUtility.GetDecoratedMethods<CommandAttribute>(); // get all console command attributes
                    CommandAttribute attribute;
                    Type[] methodParameterTypes;
                    foreach (MethodInfo method in methods) { // iterate each method
                        attribute = method.GetCustomAttribute<CommandAttribute>(); // get the command attribute on the method
                        string signature = string.Concat(method.DeclaringType.FullName, '.', method.Name); // build method signature
                        methodParameterTypes = ObjectUtility.GetMethodParameterTypes(method); // get method parameter types
                        if (methodParameterTypes.ContentEquals(targetTypes)) { // parameter cound matches target count
                            Console.Bind( // bind the method to the console as a command
                                attribute.name, // use the attribute name
                                (Console.Command.CommandCallbackDelegate)Delegate.CreateDelegate(typeof(Console.Command.CommandCallbackDelegate), method), // create delegate
                                attribute.description,
                                attribute.usage,
                                attribute.hidden
                            );
                            Console.Info(string.Concat("[Console] Bound \"", signature, "\" -> \"", attribute.name, "\".")); // log binding
                        } else {
                            string fatalMessage = string.Concat("Console failed to bind \"", signature, "\" -> \"", attribute.name, "\"."); // the command was not bound, create error message
#if UNITY_EDITOR
                            Debug.LogWarning($"Failed to bind method \"{signature}\" to console. Check the method signature matches that of \"{typeof(Console.Command.CommandCallbackDelegate).FullName}\".");
                            Debug.LogError(fatalMessage);
#endif
                            Console.Fatal(fatalMessage); // log the failure
                            Quit(QuitReason.FatalCrash, fatalMessage, null, true); // quit
                            return;
                        }
                    }

                    #endregion
                }

                #endregion

                #region set window size

                string fullscreenMode = configuration.ForceGet("player.window.fullscreen", "borderless");

                int windowWidth = configuration.ForceGet("player.window.size.x", 0);
                if (windowWidth <= 0) windowWidth = Screen.width;
                else windowWidth = Mathf.Clamp(windowWidth, 600, 7680);

                int windowHeight = configuration.ForceGet("player.window.size.y", 0);
                if (windowHeight <= 0) windowHeight = Screen.height;
                else windowHeight = Mathf.Clamp(windowHeight, 400, 4320);
                switch (fullscreenMode.ToLower()) {
                    case "windowed": {
                        Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.Windowed);
                        break;
                    }
                    case "borderless": {
                        Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.FullScreenWindow);
                        break;
                    }
                    case "fullscreen": {
                        Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.ExclusiveFullScreen);
                        break;
                    }
                    default: {
                        configuration["player.window.fullscreen"] = "borderless";
                        Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.FullScreenWindow);
                        break;
                    }
                }
                Console.Info($"[Core] Updated resolution (w:{windowWidth}px, h:{windowHeight}px, mode: \"{fullscreenMode}\").");

                #endregion

                try {
                    FileSystem.UpdateConfiguration(ConfigurationName, configuration);
                } catch (Exception exception) {
                    exception.Handle("Failed to save core configuration after initialisation.");
                }

                UnityEventLogger.Initialise();

                Console.Trace("[Core] Init (1/3) COMPLETE.");
                Console.Flush();
            }
        }

        #endregion

        #region InitialisePostSceneLoad

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#pragma warning disable IDE0051 // remove unread private members
        private static void InitialisePostSceneLoad() {
#pragma warning restore IDE0051 // remove unread private members
            lock (coreLock) {

                #region check phase
                if (phase != CorePhase.Init_Stage1) return;
                phase = CorePhase.Init_Stage2;
                #endregion

                Console.Trace("[Core] Init (2/3) STARTED.");

                #region update instance
                if (instance == null) {
                    instance = Object.FindObjectOfType<CoreInstance>();
                    if (instance == null) {
                        GameObject gameObject = new GameObject("Core", typeof(CoreInstance)) {
                            tag = "GameController",
                            layer = LayerMask.NameToLayer("Ignore Raycast"),
                            isStatic = true,
                            hideFlags = HideFlags.DontSave
                        };
                        Object.DontDestroyOnLoad(gameObject);
                        instance = gameObject.GetComponent<CoreInstance>();
                        Console.Info($"[Core] Instantiated \"CoreInstance\" GameObject.");
                    }
                }
                #endregion

                #region call initialise methods

                OrderedList<int, MethodInfo> methods = ObjectUtility.GetDecoratedMethodsOrdered<CoreInitialiseAttribute>();
                MethodInfo method;
                for (int i = 0; i < methods.Count; i++) {
                    method = methods[i];
                    string signature = $"{method.DeclaringType.FullName}.{method.Name}";
                    try {
                        method.Invoke(null, null);
                    } catch (Exception exception) {
                        Quit(QuitReason.FatalCrash, string.Concat("Failed to invoke \"", signature, "\"."), exception, true);
                        return;
                    }
                    Console.Info(string.Concat("[Core] Invoked \"", signature, "\"."));
                }

                #endregion

                #region gather update methods

                // find delegate signatures:
                Type[] updateParameterTypes = ObjectUtility.GetDelegateParameterTypes<UpdateDelegate>();
                Type[] updateDeltaTimeParameterTypes = ObjectUtility.GetDelegateParameterTypes<UpdateDeltaTimeDelegate>();

                // iterate methods:
                methods = ObjectUtility.GetDecoratedMethodsOrdered<CoreUpdateAttribute>();
                List<UpdateDelegate> updateList = new List<UpdateDelegate>();
                List<UpdateDeltaTimeDelegate> updateDeltaTimeList = new List<UpdateDeltaTimeDelegate>();
                Type[] methodParameterTypes;
                for (int i = 0; i < methods.Count; i++) {
                    method = methods[i];
                    methodParameterTypes = ObjectUtility.GetMethodParameterTypes(method);
                    if (methodParameterTypes.ContentEquals(updateParameterTypes))
                        updateList.Add((UpdateDelegate)Delegate.CreateDelegate(typeof(UpdateDelegate), method));
                    else if (methodParameterTypes.ContentEquals(updateDeltaTimeParameterTypes))
                        updateDeltaTimeList.Add((UpdateDeltaTimeDelegate)Delegate.CreateDelegate(typeof(UpdateDeltaTimeDelegate), method));
                    else {
                        Quit(QuitReason.FatalCrash, string.Concat("[Core] Failed to bind \"", method.DeclaringType.FullName, '.', method.Name, "\" to core update method."), null, true);
                        continue;
                    }
                    Console.Info(string.Concat("[Core] Bound \"", method.DeclaringType.FullName, '.', method.Name, "\" -> \"Core.Update\"."));
                }

                updateCallbacks = updateList.ToArray();
                updateDeltaTimeCallbacks = updateDeltaTimeList.ToArray();

                #endregion

                Console.Trace("[Core] Init (2/3) COMPLETE.");
                Console.Flush();
            }
        }

        #endregion

        #region InitialiseAwake

        /// <summary>
        /// Called by <see cref="CoreInstance.Start"/>.
        /// </summary>
        internal static void OnInstanceStart() {

            lock (coreLock) {

                #region check phase
                if (phase != CorePhase.Init_Stage2) return; // invalid entry point
                phase = CorePhase.Init_Stage3;
                #endregion

                Console.Trace("[Core] Init (3/3) STARTED.");

                phase = CorePhase.Running;

                Console.Trace("[Core] Init (3/3) COMPLETE.");
                Console.Flush();

                Console.Info("[Core] Init COMPLETE .");

            }

        }

        #endregion

        #region Quit

        public static void Quit(in QuitReason quitReason = QuitReason.Unknown, in string message = null, in Exception exception = null, in bool fatal = false) {
            lock (coreLock) {
                if (phase >= CorePhase.Shutdown) return; // already shutdown
                phase = CorePhase.Shutdown;
                string shutdownMessage = $"[Core] Shutdown (reason: \"{quitReason}\", fatal: {(fatal ? "true" : "false")})";
                if (message != null) shutdownMessage = string.Concat(shutdownMessage, ": ", message);
                if (fatal) Console.Fatal(shutdownMessage, exception);
                else if (exception != null) Console.Error(shutdownMessage, exception);
                else Console.Info(shutdownMessage);
                #region invoke terminate methods
                OrderedList<int, MethodInfo> methods = ObjectUtility.GetDecoratedMethodsOrdered<CoreTerminateAttribute>();
                MethodInfo method;
                for (int i = 0; i < methods.Count; i++) {
                    method = methods[i];
                    try {
                        method.Invoke(null, null);
                    } catch (Exception e) {
                        Console.Fatal(
                            string.Concat(
                                "[Core] Failed to invoke \"",
                                method.DeclaringType.FullName,
                                '.',
                                method.Name,
                                "\"."
                            ), e
                        );
                    }
                }
                #endregion
                Console.Flush();
                #region destroy core instance
                if (instance != null) {
                    try { Object.Destroy(instance.gameObject); } catch (Exception e) { e.Handle(); }
                }
                #endregion
                #region shutdown console
                try { Console.Shutdown(); } catch (Exception e) { e.Handle(); }
                #endregion
                #region shutdown application
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit((int)quitReason);
#endif
                #endregion
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Called by <see cref="CoreInstance.Update"/>.
        /// </summary>
        internal static void Update() {

            #region console window
            if (consoleWindow != null) { // console window instance exists
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current; // get the current keyboard
                if (keyboard != null) { // the current keyboard is not null
                    if (drawConsoleWindow) { // the console window should be drawn
                        if (keyboard.escapeKey.wasReleasedThisFrame) { // the escape key was released
                            consoleWindow.RevokeControl(true);
                            drawConsoleWindow = false; // stop drawing the console window
                        } else if (keyboard.enterKey.wasReleasedThisFrame) // the enter key was released
                            consoleWindow.ExecuteInput(); // execute the input of the debug console
                        else if (keyboard.upArrowKey.wasReleasedThisFrame) // the up arrow was released
                            consoleWindow.PreviousCommand(); // move to the previous command entered into the console window
                        else if (keyboard.downArrowKey.wasReleasedThisFrame) // the down arrow was released
                            consoleWindow.NextCommand(); // move to the next command entered into the console window
                    } else if (keyboard.slashKey.wasReleasedThisFrame) { // the console window is not currently active and the slash key was released
                        if (consoleWindow.GainControl(true)) { // gain control over the console window
                            Configuration configuration = FileSystem.LoadConfiguration(ConfigurationName);
                            consoleWindow.SetWindowSize(
                                configuration.ForceGet("console.window.width", -1.0f),
                                configuration.ForceGet("console.window.height", -1.0f)
                            );
                            FileSystem.UpdateConfiguration(ConfigurationName, configuration);
                            drawConsoleWindow = true; // start drawing the console window
                        }
                    }
                }
#else
                if (drawConsoleWindow) { // drawing console window
                    if (Input.GetKeyDown(KeyCode.Escape)) { // exit
                        drawConsoleWindow = false;
                    } else if (Input.GetKeyDown(KeyCode.Return)) { // execute
                        consoleWindow.ExecuteInput();
                    } else if (Input.GetKeyDown(KeyCode.UpArrow)) { // previous command
                        consoleWindow.PreviousCommand();
                    } else if (Input.GetKeyDown(KeyCode.DownArrow)) { // next command
                        consoleWindow.NextCommand();
                    }
                } else if (Input.GetKeyDown(KeyCode.Slash)) { // not drawing console, open console
                    drawConsoleWindow = true;
                    Configuration configuration = FileSystem.LoadConfiguration(ConfigurationName);
                    consoleWindow.SetWindowSize(
                        configuration.ForceGet("console.window.width", -1.0f),
                        configuration.ForceGet("console.window.height", -1.0f)
                    );
                    try {
                        FileSystem.UpdateConfiguration(ConfigurationName, configuration);
                    } catch (Exception exception) {
                        exception.Handle("Failed to save core configuration after initialisation.");
                    }
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
#endif
            }
            #endregion

            #region update callbacks

            int callbackCount = updateCallbacks.Length;
            if (callbackCount > 0) {
                UpdateDelegate callback;
                for (int i = callbackCount - 1; i >= 0; i--) {
                    callback = updateCallbacks[i];
                    try {
                        callback.Invoke();
                    } catch (Exception exception) {
                        MethodInfo methodInfo = callback.GetMethodInfo();
                        Console.Error(
                            string.Concat(
                                "An unhandled exception occurred while invoking \"",
                                methodInfo.DeclaringType.FullName,
                                '.',
                                methodInfo.Name,
                                "\"."
                            ), exception
                        );
                    }
                }
            }

            callbackCount = updateDeltaTimeCallbacks.Length;
            if (callbackCount > 0) {
                float deltaTime = Time.deltaTime;
                UpdateDeltaTimeDelegate callback;
                for (int i = callbackCount - 1; i >= 0; i--) {
                    callback = updateDeltaTimeCallbacks[i];
                    try {
                        callback.Invoke(deltaTime);
                    } catch (Exception exception) {
                        MethodInfo methodInfo = callback.GetMethodInfo();
                        Console.Error(
                            string.Concat(
                                "An unhandled exception occurred while invoking \"",
                                methodInfo.DeclaringType.FullName,
                                '.',
                                methodInfo.Name,
                                "\"."
                            ), exception
                        );
                    }
                }
            }

            #endregion

            #region clear execution queue

            if (ExecutionQueue.Count > 0) {
                lock (ExecutionQueue) {
                    while (ExecutionQueue.Count > 0) {
                        ExecutionQueue.Dequeue().Invoke();
                    }
                }
            }

            #endregion

        }

        #endregion

        #region OnGUI

        /// <summary>
        /// Called by <see cref="CoreInstance.OnGUI"/>.
        /// </summary>
        internal static void OnGUI() {

            #region console window
            if (drawConsoleWindow) consoleWindow.Draw();
            #endregion

        }

        #endregion

        #region OnPlayModeStateChanged
#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingPlayMode) {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                Quit(QuitReason.CoreDestroyed, "Play mode exited.");
            }
        }
#endif
        #endregion

        #region Validate

        /// <summary>
        /// Validates the current state of the application.
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Tools/Validate")]
#endif
        internal static void Validate() {
            IEnumerable<MethodInfo> methods = ObjectUtility.GetDecoratedMethods<CoreValidateAttribute>();
            foreach (MethodInfo method in methods) {
                string signature = $"{method.DeclaringType.FullName}.{method.Name}";
                if (ObjectUtility.GetMethodParameterTypes(method).Length != 0) {
                    Console.Fatal(string.Concat("[Core] Invalid validate method \"", signature, "\"."));
                    continue;
                }
                try {
                    method.Invoke(null, null);
                } catch (Exception exception) {
                    Console.Error(string.Concat("[Core] Failed to invoke validate method \"", signature, "\"."), exception);
                    continue;
                }
                Console.Info(string.Concat("[Core] Invoked \"", signature, "\"."));
            }
            Console.Info("[Core] Validate completed.");
        }

        #endregion

        #region Enqueue

        public static void Enqueue(in Action action) {
            lock (ExecutionQueue) {
                ExecutionQueue.Enqueue(action);
            }
        }

        public static void Enqueue(IEnumerator action) {
            lock (ExecutionQueue) {
                ExecutionQueue.Enqueue(() => { instance.StartCoroutine(action); });
            }
        }

        #endregion

        #endregion

    }

    #endregion

    #region CoreConsoleCommands

    /// <summary>
    /// Contains core console commands.
    /// </summary>
    internal static class CoreConsoleCommands {

        #region HelpCommand

        [Command(
            name: "help",
            description: "Displays a table of every command bound to the console.",
            usage:
            "help" +
            "\n\tDisplays every command and a short description of the command." +
            "\n\tflags:" +
            "\n\t\tu usage" +
            "\n\t\t\tDisplays the usage for each command in the command table." +
            "\n\t\ta all" +
            "\n\t\t\tDisplays hidden commands in the table." +
            "\nhelp {commands...}" +
            "\n\tcommands: Each argument should be an individual command you want a help message for.",
            hidden: false
        )]
        private static bool HelpCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) { // all commands
                bool all = info.HasFlag('a', "all");
                Console.Command[] commands = Console.GetCommands(all);
                bool usage = info.HasFlag('u', "usage");
                string[,] elements = new string[usage ? 3 : 2, commands.Length];
                Console.Command command;
                for (int i = 0; i < commands.Length; i++) {
                    command = commands[i];
                    elements[0, i] = command.name;
                    elements[1, i] = command.description != null
                        ? $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(command.description)}</color>"
                        : string.Empty;
                    if (usage) elements[2, i] = command.usage != null
                        ? $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(commands[i].usage)}</color>"
                        : string.Empty;
                }
                info.context.PrintTable(elements);
            } else { // list of commands
                List<string> rows = new List<string>(argumentCount);
                for (int r = 0; r < argumentCount; r++) {
                    string value = info.args[r];
                    if (value.IsNullOrWhitespace()) continue;
                    Console.Command command = Console.GetCommand(value);
                    if (command == null) {
                        rows.Add($"<color=#{Colour.Red.hex}><i>command not found: \"{value}\"</i></color>");
                    } else {
                        rows.Add($"<b>{command.name}</b>");
                        if (command.description != null) {
                            rows.Add($"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(command.description)}</color>");
                        }
                        if (command.usage != null) {
                            rows.Add("\nUsage:");
                            rows.Add($"<color=#{Colour.DarkGray.hex}>{ConsoleUtility.Escape(command.usage)}</color>");
                        }
                        if (r != argumentCount - 1) rows.Add("\n");
                    }
                }
                info.context.Print(rows.ToArray());
            }

            return true;

        }

        #endregion

        #region HistoryCommand

        [Command(
            name: "history",
            description: "Prints the command history buffer to the console.",
            usage: null,
            hidden: false
        )]
        private static bool HistoryCommand(CommandInfo info) {
            if (info.args.Count > 0) {
                info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            string[] history = info.context.CommandHistory; // get command history
            string value; // temporary value used to store the current command
            for (int i = history.Length - 1; i >= 0; i--) { // iterate command history
                value = history[i]; // get the current command
                if (value == null) continue;
                info.context.Print(info.context.DecorateCommand(value, new StringBuilder())); // print the command to the console
            }
            if (info.HasFlag('c', "clear")) info.context.ClearCommandHistory(); // clear the command history
            return true;
        }

        #endregion

        #region ClearCommand

        [Command(
            name: "clear",
            description: "Clears the console.",
            usage: null,
            hidden: false
        )]
        private static bool ClearCommand(CommandInfo info) {
            info.context.Clear();
            return true;
        }

        #endregion

        #region EchoCommand

        [Command(
            name: "echo",
            description: "Prints a message to the console.",
            usage: "echo \"{message}\"",
            hidden: false
        )]
        private static bool EchoCommand(CommandInfo info) {
            if (info.args.Count == 0) return false;
            StringBuilder stringBuilder = new StringBuilder(info.args.Count * 5);
            stringBuilder.Append(ConsoleUtility.Escape(info.args[0]));
            for (int i = 1; i < info.args.Count; i++) {
                stringBuilder.Append(' ');
                stringBuilder.Append(ConsoleUtility.Escape(info.args[i]));
            }
            info.context.Print(stringBuilder.ToString());
            return true;
        }

        #endregion

        #region CoreCommand

        [Command(
            name: "core",
            description: "Displays core and basic system information to the console.",
            usage: null,
            hidden: false
        )]
        private static bool CoreCommand(CommandInfo info) {
            if (info.args.Count > 0) {
                info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            info.context.PrintTable(
                new string[,] {
                    { "<b>Core Configuration</b>", string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Core Phase/State</color>", Core.phase.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Version</color>", Core.Version.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Compatibility Code</color>", Core.Version.ToCompatibilityCode().ToHex() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Console Logger Name</color>", Console.Logger.name },
                    { $"<color=#{Colour.Gray.hex}>Console Logger Capacity</color>", $"{Console.Logger.Count}/{Console.Logger.capacity}" },
                    { $"<color=#{Colour.Gray.hex}>Console Logger Context</color>", Console.Logger.Context != null ? Console.Logger.Context.FullName : "null"},
                    { $"<color=#{Colour.Gray.hex}>Console Logger IsRootLogger</color>", Console.Logger.IsRootLogger ? "true" : "false"},
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Console Commands</color>", Console.TotalCommands.ToString() },
                    { string.Empty, string.Empty },
                    { "<b>Application Information</b>", string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Build GUID</color>", Application.buildGUID },
                    { $"<color=#{Colour.Gray.hex}>Version</color>", Application.unityVersion },
                    { $"<color=#{Colour.Gray.hex}>Platform</color>", Application.platform.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Sandbox Type</color>", Application.sandboxType.ToString() },
                    { $"<color=#{Colour.Gray.hex}>System Language</color>", Application.systemLanguage.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Run In Background</color>", Application.runInBackground ? "Enabled" : "Disabled" },
                    { $"<color=#{Colour.Gray.hex}>Background Loading Priority</color>", Application.backgroundLoadingPriority.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Batch Mode</color>", Application.isBatchMode ? "Enabled" : "Disabled" },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Console Log Path</color>", Application.consoleLogPath },
                    { $"<color=#{Colour.Gray.hex}>Data Path</color>", Application.dataPath },
                    { $"<color=#{Colour.Gray.hex}>Persistent Data Path</color>", Application.persistentDataPath },
                    { $"<color=#{Colour.Gray.hex}>Streaming Assets Path</color>", Application.streamingAssetsPath },
                    { $"<color=#{Colour.Gray.hex}>Temporary Cache Path</color>", Application.temporaryCachePath },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Target Frame Rate</color>", Application.targetFrameRate.ToString() },
                    { string.Empty, string.Empty },
                    { "<b>System Information</b>", string.Empty },
                    { $"<color=#{Colour.Gray.hex}>System Name</color>", SystemInfo.deviceName },
                    { $"<color=#{Colour.Gray.hex}>System ID</color>", SystemInfo.deviceUniqueIdentifier },
                    { $"<color=#{Colour.Gray.hex}>System Type</color>", SystemInfo.deviceType.ToString() },
                    { $"<color=#{Colour.Gray.hex}>System Memory</color>", SystemInfo.systemMemorySize.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Processor Type</color>", SystemInfo.processorType },
                    { $"<color=#{Colour.Gray.hex}>Processor Count</color>", SystemInfo.processorCount.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Processor Frequency</color>", SystemInfo.processorFrequency.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>OS</color>", SystemInfo.operatingSystem },
                    { $"<color=#{Colour.Gray.hex}>OS Family</color>", SystemInfo.operatingSystemFamily.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Graphics Device ID</color>", SystemInfo.graphicsDeviceID.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Graphics Device Name</color>", SystemInfo.graphicsDeviceName },
                    { $"<color=#{Colour.Gray.hex}>Graphics Device Type</color>", SystemInfo.graphicsDeviceType.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Graphics Device Version</color>", SystemInfo.graphicsDeviceVersion },
                    { $"<color=#{Colour.Gray.hex}>Graphics Memory Size</color>", SystemInfo.graphicsMemorySize.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Graphics Multi-Threaded</color>", SystemInfo.graphicsMultiThreaded.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Rendering Threading Mode</color>", SystemInfo.renderingThreadingMode.ToString() },
                }, false, true
            );
            return true;
        }

        #endregion

        #region QuitCommand

        [Command(
            name: "quit",
            description: "Force quits the application.",
            usage: null,
            hidden: false
        )]
        private static bool QuitCommand(CommandInfo info) {
            if (info.args.Count > 0) {
                info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            Core.Quit(QuitReason.UserConsole);
            return true;
        }

        #endregion

        #region TimeCommand

        [Command(
            name: "time",
            description: "Displays application time information and allows for modification of application time properties.",
            usage:
            "time" +
                "\n\tDisplays application time information." +
            "\ntime set timescale {value}" +
                "\n\tAllows modification of the application timescale. The default value is 1.0." +
                "\n\tA lower value means time will travel slower, a larger number means time travels faster." +
                "\n\tThis value cannot be lower than 0.0 but has no maximum range; however, large timescales may cause performance issues.",
            hidden: false
        )]
        private static bool TimeCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) { // no arguments, display timing information
                info.context.PrintTable(
                    new string[,] {
                        { $"<color=#{Colour.Gray.hex}>Time</color>", Time.time.ToString() },
                        { $"<color=#{Colour.Gray.hex}>Unscaled Time</color>", Time.unscaledTime.ToString() },
                        { $"<color=#{Colour.Gray.hex}>Real Time Since Startup</color>", Time.realtimeSinceStartup.ToString() },
                        { $"<color=#{Colour.Gray.hex}>Time Since Scene Load</color>", Time.timeSinceLevelLoad.ToString() },
                        { string.Empty, string.Empty },
                        { $"<color=#{Colour.Gray.hex}>Time Scale</color>", Time.timeScale.ToString() },
                        { $"<color=#{Colour.Gray.hex}>Frame Rate</color>", (1.0f / Time.deltaTime).ToString() },
                        { $"<color=#{Colour.Gray.hex}>Fixed Update Rate</color>", (1.0f / Time.fixedDeltaTime).ToString() },
                        { $"<color=#{Colour.Gray.hex}>Fixed Delta Time</color>", Time.fixedDeltaTime.ToString() },
                        { $"<color=#{Colour.Gray.hex}>Default Fixed Delta Time</color>", Core.DefaultFixedDeltaTime.ToString() }
                    }, false, true
                );
            } else {
                string arg = info.args[0];
                switch (arg.ToLower()) {
                    case "set": {
                        if (argumentCount != 3) {
                            info.context.Print("Usage: time set {property} {value}");
                            return false;
                        } else if (float.TryParse(info.args[2], out float value)) {
                            switch (info.args[1].ToLower()) {
                                case "timescale": {
                                    if (value < 0.0f) {
                                        info.context.Print("The timescale property cannot be less than zero.");
                                        return false;
                                    }
                                    info.context.Print($"Time Scale {Time.timeScale} -> {value}.");
                                    Time.timeScale = value;
                                    float fixedDeltaTime = value * Core.DefaultFixedDeltaTime;
                                    info.context.Print($"Fixed Delta Time {Time.fixedDeltaTime} -> {fixedDeltaTime}.");
                                    Time.fixedDeltaTime = fixedDeltaTime;
                                    return true;
                                }
                                default: {
                                    info.context.Print(string.Concat("Invalid property: ", ConsoleUtility.Escape(info.args[1])));
                                    return false;
                                }
                            }
                        } else {
                            info.context.Print(string.Concat("Invalid value: ", ConsoleUtility.Escape(info.args[2])));
                            return false;
                        }
                    }
                    default: {
                        info.context.Print(ConsoleUtility.UnknownArgumentMessage(arg));
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region ConfigCommand

        [Command(
            name: "config",
            description: "Provides the ability to modify configuration entry values through the console.",
            usage:
            "config" +
                "\n\tDisplays a list of every configuration file in the local game configuration directory." +
                "\nconfig {file}" +
                "\n\tDisplays every configuration entry in a configuration file." +
                "\n\tfile: Name of the file (or a full or partial path) of the configuration file to view." +
            "\nconfig {file} {key}" +
                "\n\tDisplays the value of a configuration entry in a specified configuration file." +
                "\n\tfile: Name of the file (or a full or partial path) of the configuration file to view." +
                "\n\tkey: Name of the entry in the configuration file to view." +
            "\nconfig {file} {key} {value}" +
                "\n\tOverrides a key-value-pair in a specified configuration entry and saves the changes to the configuration file." +
                "\n\tfile: Name of the file (or a full or partial path) of the configuration file to edit." +
                "\n\tkey: Name of the entry in the configuration file to edit." +
                "\n\tvalue: New value to assign to the configuration entry.",
            hidden: false
        )]
        private static bool ConfigCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                const string ConfigSearchPattern = "*" + FileSystem.ConfigExtension;
                info.context.Print(FileSystem.GetFiles(ConfigSearchPattern)); // no arguments specified, list all config files
            } else { // a config file was specified
                #region search for files
                string customPattern = '*' + info.args[0]; // create a custom pattern for matching against the requested file
                if (!info.args[0].EndsWith(FileSystem.ConfigExtension)) // check for config extension
                    customPattern = string.Concat(customPattern, FileSystem.ConfigExtension); // ensure the pattern ends with the config extension
                string[] files = FileSystem.GetFiles(customPattern);
                #endregion
                if (files.Length == 0) // multiple files found
                    info.context.Print($"No configuration entry found for \"{ConsoleUtility.Escape(info.args[0])}\".");
                else if (files.Length > 1) { // multiple files found
                    info.context.Print($"Multiple configuration files found for \"{ConsoleUtility.Escape(info.args[0])}\":");
                    info.context.Print(files);
                } else { // only one file found (this is what the user wants)
                    #region load config
                    FileSystemReference fsr = new FileSystemReference(files[0], false, false); // get file system reference to config file
                    Configuration configuration = FileSystem.LoadConfiguration(fsr); // load the target configuration
                    #endregion
                    if (argumentCount == 1) { // no further arguments; therefore, display every configuration entry to the console
                        #region list config entries
                        int entryCount = configuration.Length;
                        string[,] elements = new string[3, entryCount];
                        ConfigurationEntry entry;
                        for (int i = 0; i < entryCount; i++) {
                            entry = configuration[i];
                            elements[0, i] = $"<color=#{Colour.Red.hex}>{StringUtility.ToHex(entry.hash)}</color>";
                            elements[1, i] = $"<color=#{Colour.Gray.hex}>{entry.key}</color>";
                            elements[2, i] = ConsoleUtility.Escape(entry.value);
                        }
                        info.context.Print(ConsoleUtility.Escape(fsr.AbsolutePath));
                        info.context.PrintTable(elements);
                        #endregion
                    } else { // an additional argument, this sepecifies an entry to target
                        string targetEntry = info.args[1]; // get the entry to edit
                        if (argumentCount == 2) { // no further arguments; therefore, display the value of the target entry
                            #region display target value
                            var entry = configuration[targetEntry];
                            info.context.Print(entry != null
                                ? ConsoleUtility.Escape(entry.ToString())
                                : $"\"{ConsoleUtility.Escape(targetEntry)}\" not found in \"{ConsoleUtility.Escape(info.args[0])}\"."
                            );
                            #endregion
                        } else if (configuration[targetEntry] != null) { // more arguments, further arguments should override the value of the entry
                            #region construct new value
                            StringBuilder valueBuilder = new StringBuilder((argumentCount - 2) * 7);
                            valueBuilder.Append(info.args[2]); // append the first argument
                            for (int i = 3; i < argumentCount; i++) { // there are more arguments
                                valueBuilder.Append(' ');
                                valueBuilder.Append(info.args[i]);
                            }
                            string finalValue = valueBuilder.ToString();
                            #endregion
                            #region override target value
                            configuration[targetEntry] = finalValue;
                            FileSystem.UpdateConfiguration(fsr, configuration);
                            info.context.Print(ConsoleUtility.Escape(configuration[targetEntry]));
                            #endregion
                        } else {
                            info.context.Print($"\"{ConsoleUtility.Escape(targetEntry)}\" not found in \"{ConsoleUtility.Escape(info.args[0])}\".");
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region CommandDebugCommand
#if UNITY_EDITOR
        [Command(
            name: "cdebug",
            description: "Prints the information about a command. This is used to test the command parsing system works correctly.",
            usage: null,
            hidden: true
        )]
        private static bool CommandDebugCommand(CommandInfo info) {
            List<string> table = new List<string>() {
                $"<b>Command</b>¬{info.command.name}",
                $"<color=#{Colour.Gray.hex}>Description</color>¬{info.command.description}",
                $"<color=#{Colour.Gray.hex}>Usage</color>¬{info.command.usage}",
                $"<color=#{Colour.Gray.hex}>Callback</color>¬{info.command.callback.GetMethodInfo().Name}",
                $"<b>Arguments</b>¬{info.args.Count}"
            };
            for (int i = 0; i < info.args.Count; i++) table.Add($"{i}¬{info.args[i]}");
            table.Add($"<b>Flags</b>¬{info.flags.Count}");
            for (int i = 0; i < info.flags.Count; i++) table.Add($"{i}¬{info.flags[i]}");
            info.context.PrintTable(table.ToArray(), '¬');
            return true;
        }
#endif
        #endregion

        #region ValidateCommand

        [Command(
            name: "validate",
            description: "Validates the current application state.",
            usage: null,
            hidden: true
        )]
        private static bool ValidateCommand(CommandInfo info) {
            if (info.args.Count > 0) {
                info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            Core.Validate();
            return true;
        }

        #endregion

    }

    #endregion

}