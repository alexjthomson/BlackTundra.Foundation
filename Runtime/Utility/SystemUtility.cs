#if UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

namespace BlackTundra.Foundation.System {

    /// <summary>
    /// Class used to call system functions.
    /// This will have different implementations of some methods for different operating systems.
    /// </summary>
    public static class SystemUtility {

        #region constant

        /// <summary>
        /// Number of ticks that pass per second.
        /// </summary>
#if UNITY_STANDALONE_WIN
        public const long TicksPerSecond = 1000L;
#endif

        #endregion

        #region logic

        #region GetTickCount64
        // add code for other operating systems here
#if UNITY_STANDALONE_WIN

        [DllImport("kernel32")]
        public static extern ulong GetTickCount64();

#endif
        #endregion

        #endregion

    }

}