using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Collections.Generic;

using Object = UnityEngine.Object;
using Colour = BlackTundra.Foundation.ConsoleColour;

namespace BlackTundra.Foundation {

    public static class Core {

        #region constant

        /// <summary>
        /// Object used to ensure all methods execute in the correct order.
        /// </summary>
        private static readonly object CoreLock = new();

        /// <summary>
        /// Name of the <see cref="Core"/> configuration object.
        /// </summary>
        public const string ConfigurationName = "core";

        /// <summary>
        /// Original value of <see cref="Time.fixedDeltaTime"/> on startup.
        /// </summary>
        internal static readonly float DefaultFixedDeltaTime = Time.fixedDeltaTime;

        /// <summary>
        /// <see cref="Queue{T}"/> used to enqueue methods to be executed on the main thread.
        /// </summary>
        private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

        /// <summary>
        /// Command line flags used to launch the application.
        /// </summary>
        /// <remarks>
        /// Flags must have an `-` character prefix in the command line when the application is launched. Flags are not stored
        /// with the `-` character prefix in this <see cref="HashSet{T}"/>.
        /// </remarks>
        private static readonly HashSet<string> LaunchFlagHashSet = new HashSet<string>();

        /// <summary>
        /// Launch flag that indicates the application was launched in headless mode.
        /// </summary>
        private const string HeadlessLaunchFlag = "batchmode";

        /// <summary>
        /// <see cref="ConsoleFormatter"/> used for logging by the <see cref="Core"/>.
        /// </summary>
        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(Core));

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
        /// Array of <see cref="UpdateDelegate"/> callbacks that should be called every frame.
        /// </summary>
        private static UpdateDelegate[] updateCallbacks;

        /// <summary>
        /// Array of <see cref="UpdateDeltaTimeDelegate"/> callbacks that should be called every frame.
        /// </summary>
        private static UpdateDeltaTimeDelegate[] updateDeltaTimeCallbacks;

        /// <summary>
        /// Tracks if the application was launched in headless mode.
        /// </summary>
        private static bool headless = false;

        #endregion

        #region property
        
        /// <summary>
        /// <see cref="Version"/> of the application.
        /// </summary>
        public static Version Version { get; private set; } = Version.Invalid;

        /// <summary>
        /// Tracks if the <see cref="Core"/> is running.
        /// </summary>
        public static bool IsRunning { get; private set; } = false;

        /// <summary>
        /// <c>true</c> if the application was launched in headless mode.
        /// </summary>
        public static bool IsHeadless => headless;

        #endregion

        #region logic

        #region InitialisePostAssembliesLoaded

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InitialisePostAssembliesLoaded() {
            lock (CoreLock) {

                if (phase != CorePhase.Idle) return;
                phase = CorePhase.Init_Stage1;

                #region initialise file system

                /*
                 * The file system should be initialised first since logging relies on the file system.
                 * This means the file system needs to be able to be initialised without using any logging.
                 * The file system code should therefore be highly stable and any code that needs logging
                 * should be executed after this initialisation.
                 */

                FileSystem.Initialise();
                Console.Flush(); // flush the console (ensure buffer is written before initialisation starts)

                #endregion

                #region process configuration

                // load core configuration:
                Configuration configuration = Configuration.GetConfiguration(ConfigurationName);
                configuration.Load(); // load the configuration
                // check for persistent log file:
                if (!configuration.ForceGet("console.logger.persistent", false)) { // logger is not persistent
                    Console.Logger.Clear(); // clear console logger
                }
                // assign logger log level:
                Console.LoggerLogLevel = configuration.ForceGet(
                    Console.LoggerLogLevelEntryName,
                    Console.LoggerLogLevelDefaultValue
                );
                // save configuration:
                if (configuration.IsDirty) configuration.Save();

                #endregion

                #region create new log entry

                string separator = new string('-', 64); // create separator string
                Console.Empty(string.Concat(separator, ' ', DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), ' ', separator)); // format a timestamp for the console

                #endregion

                // start initialisation:
                ConsoleFormatter.Trace("Init (1/3) STARTED.");

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
                    Debug.LogWarning("Make sure the project version is formatted correctly: `{major}.{minor}.{release}{release_type}`.", instance);
#endif
                    Quit(QuitReason.CoreSelfQuit, $"Failed to parse application version (version: `{Application.version}`).", exception, true);
                    throw exception;
                }
                ConsoleFormatter.Info(string.Concat("Version: ", Version.ToString()));
                #endregion

                #region process command line arguments

                string[] arguments = Environment.GetCommandLineArgs();
                string argument;
                for (int i = arguments.Length - 1; i >= 0; i--) {
                    argument = arguments[i];
                    if (argument.Length > 1 && argument[0] == '-') { // argument is a flag
                        LaunchFlagHashSet.Add(argument[1..].ToLower()); // add the flag to the hash set
                    }
                }
                headless = HasLaunchFlag(HeadlessLaunchFlag); // check if application was launched in headless mode

                #endregion

                #region initialise console window

                if (!headless) { // not in headless mode, therefore console window should be initialized
                    try {
                        ConsoleWindow.Initialise();
                    } catch (Exception exception) {
                        Quit(QuitReason.FatalCrash, "Failed to construct core console window.", exception, true);
                        return;
                    }
                }

                #endregion

                #region initialise platform
#if USE_STEAMWORKS
                try {
                    Platform.Steamworks.SteamManager.Initialise();
                } catch (Exception exception) {
                    Quit(QuitReason.FatalCrash, "Failed to initialise Steamworks.NET.", exception, true);
                    return;
                }
#endif
                #endregion

                #region bind commands

                if (!headless) { // not headless

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
                            ConsoleFormatter.Info(string.Concat("Bound `", signature, "` -> `", attribute.name, "`.")); // log binding
                        } else {
                            string fatalMessage = string.Concat("Console failed to bind `", signature, "` -> `", attribute.name, "`."); // the command was not bound, create error message
#if UNITY_EDITOR
                            Debug.LogWarning($"Failed to bind method `{signature}` to console. Check the method signature matches that of `{typeof(Console.Command.CommandCallbackDelegate).FullName}`.");
                            Debug.LogError(fatalMessage);
#endif
                            ConsoleFormatter.Fatal(fatalMessage); // log the failure
                            Quit(QuitReason.FatalCrash, fatalMessage, null, true); // quit
                            return;
                        }
                    }

                }

                #endregion

                UnityEventLogger.Initialise();

                ConsoleFormatter.Trace("Init (1/3) COMPLETE.");
                Console.Flush();
            }
        }

        #endregion

        #region InitialisePostSceneLoad

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitialisePostSceneLoad() {
            IsRunning = true;
            lock (CoreLock) {

                #region check phase
                if (phase != CorePhase.Init_Stage1) return;
                phase = CorePhase.Init_Stage2;
                #endregion

                ConsoleFormatter.Trace("Init (2/3) STARTED.");

                #region update instance
                if (instance == null) {
                    instance = Object.FindObjectOfType<CoreInstance>();
                    if (instance == null) {
                        GameObject gameObject = new GameObject(
                            "__" + nameof(CoreInstance),
                            typeof(CoreInstance)
                        ) {
                            tag = "GameController",
                            layer = LayerMask.NameToLayer("Ignore Raycast"),
                            isStatic = true,
                            hideFlags = HideFlags.HideAndDontSave
                        };
                        Object.DontDestroyOnLoad(gameObject);
                        instance = gameObject.GetComponent<CoreInstance>();
                        ConsoleFormatter.Info($"Instantiated `{nameof(CoreInstance)}` GameObject.");
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
                        Quit(QuitReason.FatalCrash, string.Concat("Failed to invoke `", signature, "`."), exception, true);
                        return;
                    }
                    ConsoleFormatter.Info(string.Concat("Invoked `", signature, "`."));
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
                        Quit(QuitReason.FatalCrash, string.Concat("Failed to bind \"", method.DeclaringType.FullName, '.', method.Name, "\" to core update method."), null, true);
                        continue;
                    }
                    ConsoleFormatter.Info(string.Concat("Bound `", method.DeclaringType.FullName, '.', method.Name, "` -> `Core.Update`."));
                }

                updateCallbacks = updateList.ToArray();
                updateDeltaTimeCallbacks = updateDeltaTimeList.ToArray();

                #endregion

                ConsoleFormatter.Trace("Init (2/3) COMPLETE.");
                Console.Flush();
            }
        }

        #endregion

        #region OnInstanceStart

        /// <summary>
        /// Called by <see cref="CoreInstance.Start"/>.
        /// </summary>
        internal static void OnInstanceStart() {
            lock (CoreLock) {
                #region check phase
                if (phase != CorePhase.Init_Stage2) return; // invalid entry point
                phase = CorePhase.Init_Stage3;
                #endregion
                ConsoleFormatter.Trace("Init (3/3) STARTED.");
                phase = CorePhase.Running;
                ConsoleFormatter.Trace("Init (3/3) COMPLETE.");
                Console.Flush();
                ConsoleFormatter.Info("Init COMPLETE.");
            }
        }

        #endregion

        #region Quit

        public static void Quit(in QuitReason quitReason = QuitReason.Unknown, in string message = null, in Exception exception = null, in bool fatal = false) {
            lock (CoreLock) {
                if (phase >= CorePhase.Shutdown) return; // already shutdown
                phase = CorePhase.Shutdown;
                string shutdownMessage = $"Shutdown (reason: `{quitReason}`, fatal: {(fatal ? "true" : "false")})";
                if (message != null) shutdownMessage = string.Concat(shutdownMessage, ": ", message);
                if (fatal) ConsoleFormatter.Fatal(shutdownMessage, exception);
                else if (exception != null) ConsoleFormatter.Error(shutdownMessage, exception);
                else ConsoleFormatter.Info(shutdownMessage);
                #region invoke terminate methods
                OrderedList<int, MethodInfo> methods = ObjectUtility.GetDecoratedMethodsOrdered<CoreTerminateAttribute>();
                MethodInfo method;
                for (int i = 0; i < methods.Count; i++) {
                    method = methods[i];
                    try {
                        method.Invoke(null, null);
                    } catch (Exception e) {
                        ConsoleFormatter.Fatal(
                            string.Concat(
                                "Failed to invoke `",
                                method.DeclaringType.FullName,
                                '.',
                                method.Name,
                                "`."
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
                #region shutdown console window
                try { ConsoleWindow.Terminate(); } catch (Exception e) { e.Handle(); }
                #endregion
                #region shutdown application
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit((int)quitReason);
#endif
                #endregion
            }
            IsRunning = false;
        }

        #endregion

        #region Update

        /// <summary>
        /// Called by <see cref="CoreInstance.Update"/>.
        /// </summary>
        internal static void Update() {

            ConsoleWindow.Update();

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
                        ConsoleFormatter.Error(
                            string.Concat(
                                "An unhandled exception occurred while invoking `",
                                methodInfo.DeclaringType.FullName,
                                '.',
                                methodInfo.Name,
                                "`."
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
                        ConsoleFormatter.Error(
                            string.Concat(
                                "An unhandled exception occurred while invoking `",
                                methodInfo.DeclaringType.FullName,
                                '.',
                                methodInfo.Name,
                                "`."
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
            ConsoleWindow.Draw();
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
                    ConsoleFormatter.Fatal(string.Concat("Invalid validate method \"", signature, "\"."));
                    continue;
                }
                try {
                    method.Invoke(null, null);
                } catch (Exception exception) {
                    ConsoleFormatter.Error(string.Concat("Failed to invoke validate method \"", signature, "\"."), exception);
                    continue;
                }
                ConsoleFormatter.Info(string.Concat("Invoked \"", signature, "\"."));
            }
            ConsoleFormatter.Info("Validate completed.");
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

        #region HasLaunchFlag

        /// <returns>
        /// Returns <c>true</c> if the application was launched with a specified <paramref name="argument"/>.
        /// </returns>
        /// <remarks>
        /// Launch flags must have an `-` character prefix in the command line when the application is launched.
        /// The `-` character should not be included in the <paramref name="argument"/> parameter.
        /// </remarks>
        public static bool HasLaunchFlag(in string argument) {
            return LaunchFlagHashSet.Contains(argument.ToLower());
        }

        #endregion

        #endregion

        #region default commands

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
                ConsoleWindow.PrintTable(elements);
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
                ConsoleWindow.Print(rows.ToArray());
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
                ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            string[] history = ConsoleWindow.CommandHistory; // get command history
            string value; // temporary value used to store the current command
            for (int i = history.Length - 1; i >= 0; i--) { // iterate command history
                value = history[i]; // get the current command
                if (value == null) continue;
                ConsoleWindow.Print(ConsoleWindow.DecorateCommand(value, new StringBuilder())); // print the command to the console
            }
            if (info.HasFlag('c', "clear")) ConsoleWindow.ClearCommandHistory(); // clear the command history
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
            ConsoleWindow.Clear();
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
            ConsoleWindow.Print(stringBuilder.ToString());
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
                ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            ConsoleWindow.PrintTable(
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
                    { $"<color=#{Colour.Gray.hex}>Render Pipeline</color>", QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline.name : "None" },
                    { $"<color=#{Colour.Gray.hex}>Render Quality</color>", QualitySettings.GetQualityLevel().ToString() },
                    { $"<color=#{Colour.Gray.hex}>Active Colour Space</color>", QualitySettings.activeColorSpace.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Anisotropic Filtering</color>", QualitySettings.anisotropicFiltering.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Anti-aliasing</color>", QualitySettings.antiAliasing.ToString() },
                    { $"<color=#{Colour.Gray.hex}>LOD Bias</color>", QualitySettings.lodBias.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Pixel Light Count</color>", QualitySettings.pixelLightCount.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Realtime Reflection Probes</color>", QualitySettings.realtimeReflectionProbes ? "Enabled" : "Disabled" },
                    { $"<color=#{Colour.Gray.hex}>Shadow Distance</color>", QualitySettings.shadowDistance.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Target Frame Rate</color>", Application.targetFrameRate.ToString() },
                    { string.Empty, string.Empty },
                    { "<b>Platform Information</b>", string.Empty },
#if USE_STEAMWORKS
                    { $"<color=#{Colour.Gray.hex}>Application ID</color>", Platform.Steamworks.SteamManager.AppID.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Steam Auth Ticket Size</color>", Platform.Steamworks.SteamManager.SteamAuthTicketSize.ToString() },
                    { string.Empty, string.Empty },
                    { $"<color=#{Colour.Gray.hex}>Steam ID</color>", Platform.Steamworks.SteamManager.SteamID.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Display Name</color>", Platform.Steamworks.SteamManager.DisplayName.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Online Status</color>", Platform.Steamworks.SteamManager.OnlineStatus.ToString() },
                    { $"<color=#{Colour.Gray.hex}>Friend Count</color>", Platform.Steamworks.SteamManager.FriendCount.ToString() },
#else
                    { "<i>unknown platform</i>", string.Empty },
#endif
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
                ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
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
                ConsoleWindow.PrintTable(
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
                            ConsoleWindow.Print("Usage: time set {property} {value}");
                            return false;
                        } else if (float.TryParse(info.args[2], out float value)) {
                            switch (info.args[1].ToLower()) {
                                case "timescale": {
                                    if (value < 0.0f) {
                                        ConsoleWindow.Print("The timescale property cannot be less than zero.");
                                        return false;
                                    }
                                    ConsoleWindow.Print($"Time Scale {Time.timeScale} -> {value}.");
                                    Time.timeScale = value;
                                    float fixedDeltaTime = value * Core.DefaultFixedDeltaTime;
                                    ConsoleWindow.Print($"Fixed Delta Time {Time.fixedDeltaTime} -> {fixedDeltaTime}.");
                                    Time.fixedDeltaTime = fixedDeltaTime;
                                    return true;
                                }
                                default: {
                                    ConsoleWindow.Print(string.Concat("Invalid property: ", ConsoleUtility.Escape(info.args[1])));
                                    return false;
                                }
                            }
                        } else {
                            ConsoleWindow.Print(string.Concat("Invalid value: ", ConsoleUtility.Escape(info.args[2])));
                            return false;
                        }
                    }
                    default: {
                        ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(arg));
                        return false;
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
            ConsoleWindow.PrintTable(table.ToArray(), '¬');
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
                ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                return false;
            }
            Core.Validate();
            return true;
        }

        #endregion

        #endregion

    }

}