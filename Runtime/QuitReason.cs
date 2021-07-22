namespace BlackTundra.Foundation {

    /// <summary>
    /// Describes how the application is quit.
    /// A negative quit reason is related to an error or crash.
    /// A positive quit reason (including zero) is an expected quit.
    /// </summary>
    public enum QuitReason : int {

        /// <summary>
        /// Unknown quit reason.
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// The game crashed.
        /// </summary>
        Crash = -2,

        /// <summary>
        /// The game encountered a fatal exception and therefore crashed.
        /// </summary>
        FatalCrash = -3,

        /// <summary>
        /// The core was destroyed.
        /// </summary>
        CoreDestroyed = -4,

        /// <summary>
        /// The core was shutdown.
        /// </summary>
        CoreShutdown = -5,

        /// <summary>
        /// The core automatically shut itself down.
        /// </summary>
        CoreSelfQuit = -6,

        /// <summary>
        /// A system outside of the core has requested that the application be quit.
        /// </summary>
        InternalSelfQuit = -7,

        /// <summary>
        /// The user closed the game.
        /// </summary>
        User = 0,

        /// <summary>
        /// The user closed the game through the console.
        /// </summary>
        UserConsole = 1

    }

}