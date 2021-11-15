using System;

using UnityEngine;

namespace BlackTundra.Foundation {

#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Console/Window Settings", fileName = "ConsoleWindowSettings", order = -1)]
#endif
    [Serializable]
    public sealed class ConsoleWindowSettings : ScriptableObject {

        #region variable

        /// <summary>
        /// Font to use for the debug console.
        /// </summary>
        [SerializeField]
        internal Font font = null;

        /// <summary>
        /// Font size to use fot the debug console.
        /// </summary>
#if UNITY_EDITOR
        [Range(6, 32)]
#endif
        [SerializeField]
        internal int fontSize = 12;

        #endregion

    }

}