#if ENABLE_INPUT_SYSTEM

using System;
using System.Runtime.InteropServices;

namespace BlackTundra.Foundation.Control {

    /// <summary>
    /// Describes control features that can be enabled/disabled after a major
    /// control change has occured. Namely after
    /// <see cref="IControllable.OnControlGained(in ControlUser)"/> and
    /// <see cref="IControllable.OnControlRevoked(in ControlUser)"/>.
    /// </summary>
    /// <seealso cref="IControllable.OnControlGained(in ControlUser)"/>
    /// <seealso cref="IControllable.OnControlRevoked(in ControlUser)"/>
    [ComVisible(true)]
    [Flags]
    public enum ControlFlags : uint {

        /// <summary>
        /// Empty flag.
        /// </summary>
        None = 0u,

        /// <summary>
        /// Locks the cursor to the center of the application.
        /// </summary>
        LockCursor = 1u << 0,

        /// <summary>
        /// Hides the cursor so it cannot be seen.
        /// </summary>
        HideCursor = 1u << 1,

    }

    public static class ControlFlagsUtility {
        public static void Apply(this ControlFlags flags) => ControlUser.ControlFlags = flags;
    }

}

#endif