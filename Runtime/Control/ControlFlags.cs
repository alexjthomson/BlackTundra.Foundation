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
    public enum ControlFlags : int {

        /// <summary>
        /// Empty flag.
        /// </summary>
        None = 0,

        /// <summary>
        /// Locks the cursor to the center of the application.
        /// </summary>
        LockCursor = 1 << 0,

        /// <summary>
        /// Hides the cursor so it cannot be seen.
        /// </summary>
        HideCursor = 1 << 1,

    }

    public static class ControlFlagsUtility {
        public static void Apply(this ControlFlags controlFlags) => ControlManager.ControlFlags = controlFlags;
    }

}