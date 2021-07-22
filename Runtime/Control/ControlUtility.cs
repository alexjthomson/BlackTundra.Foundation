#if ENABLE_INPUT_SYSTEM

using System;

namespace BlackTundra.Foundation.Control {

    public static class ControlUtility {

        #region logic

        #region GetControlUser

        /// <returns>
        /// Returns the <see cref="ControlUser"/> controlling the <paramref name="controllable"/>; otherwise,
        /// <c>null</c> is returned.
        /// </returns>
        public static ControlUser GetControlUser(this IControllable controllable) {
            if (controllable == null) throw new ArgumentNullException("controllable");
            return ControlUser.FindControlUser(controllable);
        }

        #endregion

        #region GainControl

        public static bool GainControl(this IControllable controllable, ControlUser user = null) {
            if (controllable == null) throw new ArgumentNullException("controllable");
            if (user == null) { // no control user was provided
                user = ControlUser.main; // default to the main control user
                if (user == null) return false; // there are no control users
            }
            return user.GainControl(controllable);
        }

        #endregion

        #region RevokeControl

        public static void RevokeControl(this IControllable controllable) {
            if (controllable == null) throw new ArgumentNullException("controllable");
            ControlUser user = ControlUser.FindControlUser(controllable);
            if (user != null) user.RevokeControl(controllable);
        }

        #endregion

        #endregion

    }

}

#endif