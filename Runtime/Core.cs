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
using BlackTundra.Foundation.System;
using BlackTundra.Foundation.Utility;

#if ENABLE_INPUT_SYSTEM
using BlackTundra.Foundation.Control;
#endif

#if USE_STEAMWORKS
using BlackTundra.Foundation.Platform.Steamworks;
#endif

// DEFINES:

using Object = UnityEngine.Object;
using Colour = BlackTundra.Foundation.ConsoleColour;

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

        #endregion

        #region property

        public static Version Version { get; private set; } = Version.Invalid;

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
                Console.Info("[Core] Init (1/3) STARTED.");

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
                    ParameterInfo[] targetParameterInfo = SystemUtility.GetDelegateInfo<Console.Command.CommandCallbackDelegate>().GetParameters(); // get delegate parameters
                    int targetParameterCount = targetParameterInfo.Length; // get the number of parameters in the delegate
                    Type[] targetTypes = new Type[targetParameterCount]; // create a buffer of target types, these will match up with the parameters
                    for (int i = targetParameterCount - 1; i >= 0; i--) // iterate through the parameters
                        targetTypes[i] = targetParameterInfo[i].GetType(); // assign the type corresponding to the current parameter
                    
                    // iterate console commands:
                    IEnumerable<MethodInfo> methods = SystemUtility.GetMethods<CommandAttribute>(); // get all console command attributes
                    CommandAttribute attribute;
                    foreach (MethodInfo method in methods) { // iterate each method
                        attribute = method.GetCustomAttribute<CommandAttribute>(); // get the command attribute on the method
                        string signature = string.Concat(method.DeclaringType.FullName, '.', method.Name); // build method signature
                        ParameterInfo[] parameters = method.GetParameters(); // get method parameter info
                        if (parameters.Length == targetParameterCount) { // parameter cound matches target count
                            bool match = true; // track if the parameters match
                            for (int i = targetParameterCount - 1; i >= 0; i--) { // iterate through parameters
                                if (!targetTypes[i].Equals(parameters[i].GetType())) { // check the parameter matches
                                    match = false; // parameter does not match
                                    break; // stop iterating parameters here
                                }
                            }
                            if (match) { // the parameters match
                                Console.Bind( // bind the method to the console as a command
                                    attribute.name, // use the attribute name
                                    (Console.Command.CommandCallbackDelegate)Delegate.CreateDelegate(typeof(Console.Command.CommandCallbackDelegate), method), // create delegate
                                    attribute.description,
                                    attribute.usage
                                );
                                Console.Info(string.Concat("[Console] Bound \"", signature, "\" -> \"", attribute.name, "\".")); // log binding
                                continue; // move to next method
                            }
                        }
                        string fatalMessage = string.Concat("Console failed to bind \"", signature, "\" -> \"", attribute.name, "\"."); // the command was not bound, create error message
#if UNITY_EDITOR
                        Debug.LogWarning($"Failed to bind method \"{signature}\" to console. Check the method signature matches that of \"{typeof(Console.Command.CommandCallbackDelegate).FullName}\".");
                        Debug.LogError(fatalMessage);
#endif
                        Console.Fatal(fatalMessage); // log the failure
                        Quit(QuitReason.FatalCrash, fatalMessage, null, true); // quit
                        return;
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
                Console.Info($"[Application] Updated resolution (w:{windowWidth}px, h:{windowHeight}px, mode: \"{fullscreenMode}\").");

                #endregion

                try {
                    FileSystem.UpdateConfiguration(ConfigurationName, configuration);
                } catch (Exception exception) {
                    exception.Handle("Failed to save core configuration after initialisation.");
                }

                UnityEventLogger.Initialise();

                Console.Info("[Core] Init (1/3) COMPLETE.");
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

                Console.Info("[Core] Init (2/3) STARTED.");

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

                Console.Info("[Core] Init (2/3) COMPLETE.");
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

                Console.Info("[Core] Init (3/3) STARTED.");

                #region call initialise methods

                IEnumerable<MethodInfo> methods = SystemUtility.GetMethods<CoreInitialiseAttribute>();
                foreach (MethodInfo method in methods) {
                    string signature = $"{method.DeclaringType.FullName}.{method.Name}";
                    Console.Info(string.Concat("[Core] Invoking \"", signature, "\"."));
                    try {
                        method.Invoke(null, null);
                    } catch (Exception exception) {
                        Quit(QuitReason.FatalCrash, string.Concat("Failed to invoke \"", signature, "\"."), exception, true);
                        return;
                    }
                    Console.Info(string.Concat("[Core] Invoked \"", signature, "\"."));
                }

                #endregion

                Console.Info("[Core] Init (3/3) COMPLETE.");
                Console.Flush();

                phase = CorePhase.Running;
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
                Console.Flush();
                #region shutdown steamworks
#if USE_STEAMWORKS
                try { SteamManager.Shutdown(); } catch (Exception e) { e.Handle(); } // try to shut down steamworks
#endif
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

            #region steamworks
#if USE_STEAMWORKS
            // NOTE: this may require a try catch in the future
            SteamManager.Update(); // update the steam manager
#endif
            #endregion

            #region console window
            if (consoleWindow != null) { // console window instance exists
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current; // get the current keyboard
                if (keyboard != null) { // the current keyboard is not null
                    if (drawConsoleWindow) { // the console window should be drawn
                        if (keyboard.escapeKey.wasReleasedThisFrame) { // the escape key was released
                            consoleWindow.RevokeControl();
                            drawConsoleWindow = false; // stop drawing the console window
                        } else if (keyboard.enterKey.wasReleasedThisFrame) // the enter key was released
                            consoleWindow.ExecuteInput(); // execute the input of the debug console
                        else if (keyboard.upArrowKey.wasReleasedThisFrame) // the up arrow was released
                            consoleWindow.PreviousCommand(); // move to the previous command entered into the console window
                        else if (keyboard.downArrowKey.wasReleasedThisFrame) // the down arrow was released
                            consoleWindow.NextCommand(); // move to the next command entered into the console window
                    } else if (keyboard.slashKey.wasReleasedThisFrame) { // the console window is not currently active and the slash key was released
                        ControlUser user = ControlUser.FindControlUser(keyboard); // get the control user using the current keyboard
                        if (user != null && user.GainControl(consoleWindow, true)) { // gain control over the console window
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
            "help",
            "Displays a table of every command bound to the console.",
            "help" +
            "\nhelp {commands...}" +
            "\n\tcommands: Each argument should be an individual command you want a help message for."
        )]
        private static bool HelpCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
            int argumentCount = info.args.Count;
            if (argumentCount == 0) { // all commands
                Console.Command[] commands = Console.GetCommands();
                bool all = info.HasFlag('a', "all");
                string[,] elements = new string[all ? 3 : 2, commands.Length];
                for (int r = 0; r < commands.Length; r++) {
                    elements[0, r] = commands[r].name;
                    elements[1, r] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(commands[r].description)}</color>";
                    if (all) elements[2, r] = $"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(commands[r].usage)}</color>";
                }
                console.PrintTable(elements);
            } else { // list of commands
                List<string> rows = new List<string>(argumentCount);
                for (int r = 0; r < argumentCount; r++) {
                    string value = info.args[r];
                    if (value.IsNullOrWhitespace()) continue;
                    Console.Command command = Console.GetCommand(value);
                    rows.Add($"<b>{command.name}</b>");
                    rows.Add($"<color=#{Colour.Gray.hex}>{ConsoleUtility.Escape(command.description)}</color>");
                    rows.Add("\nUsage:");
                    rows.Add($"<color=#{Colour.DarkGray.hex}>{ConsoleUtility.Escape(command.usage)}</color>");
                    if (r != argumentCount - 1) rows.Add("\n");
                }
                console.Print(rows.ToArray());
            }

            return true;

        }

        #endregion

        #region HistoryCommand

        [Command("history", "Prints the command history buffer to the console.")]
        private static bool HistoryCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
            if (info.args.Count > 0) {
                console.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            string[] history = console.CommandHistory; // get command history
            string value; // temporary value used to store the current command
            for (int i = history.Length - 1; i >= 0; i--) { // iterate command history
                value = history[i]; // get the current command
                if (value == null) continue;
                console.Print(console.DecorateCommand(value, new StringBuilder())); // print the command to the console
            }
            if (info.HasFlag('c', "clear")) console.ClearCommandHistory(); // clear the command history
            return true;
        }

        #endregion

        #region ClearCommand

        [Command("clear", "Clears the console.")]
        private static bool ClearCommand(in CommandInfo info) {
            Core.consoleWindow.Clear();
            return true;
        }

        #endregion

        #region EchoCommand

        [Command("echo", "Prints a message to the console.", "echo \"{message}\"")]
        private static bool EchoCommand(in CommandInfo info) {
            if (info.args.Count == 0) return false;
            StringBuilder stringBuilder = new StringBuilder(info.args.Count * 5);
            stringBuilder.Append(ConsoleUtility.Escape(info.args[0]));
            for (int i = 1; i < info.args.Count; i++) {
                stringBuilder.Append(' ');
                stringBuilder.Append(ConsoleUtility.Escape(info.args[i]));
            }
            Core.consoleWindow.Print(stringBuilder.ToString());
            return true;
        }

        #endregion

        #region CoreCommand

        [Command("core", "Displays core and basic system information to the console.")]
        private static bool CoreCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
            if (info.args.Count > 0) {
                console.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            console.PrintTable(
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

        [Command("quit", "Force quits the game.")]
        private static bool QuitCommand(in CommandInfo info) {
            if (info.args.Count > 0) {
                Core.consoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            Core.Quit(QuitReason.UserConsole);
            return true;
        }

        #endregion

        #region TimeCommand

        [Command(
            "time",
            "Displays application time information and allows for modification of application time properties.",
            "time" +
                "\n\tDisplays application time information." +
            "\ntime set timescale {value}" +
                "\n\tAllows modification of the application timescale. The default value is 1.0." +
                "\n\tA lower value means time will travel slower, a larger number means time travels faster." +
                "\n\tThis value cannot be lower than 0.0 but has no maximum range; however, large timescales may cause performance issues."
        )]
        private static bool TimeCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
            int argumentCount = info.args.Count;
            if (argumentCount == 0) { // no arguments, display timing information
                console.PrintTable(
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
                switch (info.args[0].ToLower()) {
                    case "set": {
                        if (argumentCount != 3) {
                            console.Print("Usage: time set {property} {value}");
                            return false;
                        } else if (float.TryParse(info.args[2], out float value)) {
                            switch (info.args[1].ToLower()) {
                                case "timescale": {
                                    if (value < 0.0f) {
                                        console.Print("The timescale property cannot be less than zero.");
                                        return false;
                                    }
                                    console.Print($"Time Scale {Time.timeScale} -> {value}.");
                                    Time.timeScale = value;
                                    float fixedDeltaTime = value * Core.DefaultFixedDeltaTime;
                                    console.Print($"Fixed Delta Time {Time.fixedDeltaTime} -> {fixedDeltaTime}.");
                                    Time.fixedDeltaTime = fixedDeltaTime;
                                    return true;
                                }
                                default: {
                                    console.Print(string.Concat("Invalid property: ", ConsoleUtility.Escape(info.args[1])));
                                    return false;
                                }
                            }
                        } else {
                            console.Print(string.Concat("Invalid value: ", ConsoleUtility.Escape(info.args[2])));
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region ConfigCommand

        [Command(
            "config",
            "Provides the ability to modify configuration entry values through the console.",
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
                "\n\tvalue: New value to assign to the configuration entry."
        )]
        private static bool ConfigCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                const string ConfigSearchPattern = "*" + FileSystem.ConfigExtension;
                console.Print(FileSystem.GetFiles(ConfigSearchPattern)); // no arguments specified, list all config files
            } else { // a config file was specified
                #region search for files
                string customPattern = '*' + info.args[0]; // create a custom pattern for matching against the requested file
                if (!info.args[0].EndsWith(FileSystem.ConfigExtension)) // check for config extension
                    customPattern = string.Concat(customPattern, FileSystem.ConfigExtension); // ensure the pattern ends with the config extension
                string[] files = FileSystem.GetFiles(customPattern);
                #endregion
                if (files.Length == 0) // multiple files found
                    console.Print($"No configuration entry found for \"{ConsoleUtility.Escape(info.args[0])}\".");
                else if (files.Length > 1) { // multiple files found
                    console.Print($"Multiple configuration files found for \"{ConsoleUtility.Escape(info.args[0])}\":");
                    console.Print(files);
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
                        console.Print(ConsoleUtility.Escape(fsr.AbsolutePath));
                        console.PrintTable(elements);
                        #endregion
                    } else { // an additional argument, this sepecifies an entry to target
                        string targetEntry = info.args[1]; // get the entry to edit
                        if (argumentCount == 2) { // no further arguments; therefore, display the value of the target entry
                            #region display target value
                            var entry = configuration[targetEntry];
                            console.Print(entry != null
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
                            console.Print(ConsoleUtility.Escape(configuration[targetEntry]));
                            #endregion
                        } else {
                            console.Print($"\"{ConsoleUtility.Escape(targetEntry)}\" not found in \"{ConsoleUtility.Escape(info.args[0])}\".");
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region CommandDebugCommand
#if UNITY_EDITOR

        [Command("cdebug", "Prints the information about a command. This is used to test the command parsing system works correctly.")]
        private static bool CommandDebugCommand(in CommandInfo info) {
            ConsoleWindow console = Core.consoleWindow;
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
            console.PrintTable(table.ToArray(), '¬');
            return true;
        }

#endif
        #endregion

    }

    #endregion

}