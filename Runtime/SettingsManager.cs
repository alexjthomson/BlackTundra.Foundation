using BlackTundra.Foundation.Utility;

using System;
#if !UNITY_EDITOR
using System.Collections.Generic;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace BlackTundra.Foundation {

    /// <summary>
    /// Class responsible for managing <see cref="ScriptableObject"/> assets that store
    /// settings for different elements of the project. These assets cannot be modifed
    /// after the application is built so is ideal for settings that need to be tweaked
    /// only during development time.
    /// </summary>
    public static class SettingsManager {

        #region constant

#if !UNITY_EDITOR
        private static readonly Dictionary<string, ScriptableObject> SettingsCache = new Dictionary<string, ScriptableObject>();
#endif

        #endregion

        #region logic

        #region SetSettings
#if UNITY_EDITOR

        /// <summary>
        /// Saves/overrides a settings object.
        /// </summary>
        /// <remarks>This is an editor only method.</remarks>
        public static void SetSettings<T>(in T settings) where T : ScriptableObject => SetSettings(typeof(T).Name, settings);

        /// <summary>
        /// Saves/overrides a settings object.
        /// </summary>
        /// <remarks>This is an editor only method.</remarks>
        public static void SetSettings<T>(in string name, in T settings) where T : ScriptableObject {

            #region validate arguments
            if (name == null) throw new ArgumentNullException("name");
            if (name.IsNullOrWhitespace()) throw new ArgumentException("name");
            if (settings == null) throw new ArgumentNullException("settings");
            #endregion

            #region check local directory structure
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Settings")) {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Settings");
            }
            #endregion

            AssetDatabase.CreateAsset(settings, string.Concat("Assets/Resources/Settings/", name, ".asset"));
            AssetDatabase.SaveAssets();

        }

#endif
        #endregion

        #region GetSettings

        /// <summary>
        /// Gets a settings object.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="ScriptableObject"/> storing the settings.</typeparam>
        /// <remarks>
        /// This method is the same as the <see cref="GetSettings{T}(in string)"/> method. It uses the
        /// name of the <typeparamref name="T"/> type as the name of the settings object.
        /// </remarks>
        public static T GetSettings<T>() where T : ScriptableObject => GetSettings<T>(typeof(T).Name);

        /// <summary>
        /// Gets settings by name.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="ScriptableObject"/> storing the settings.</typeparam>
        /// <param name="name">Name of the settings object.</param>
        public static T GetSettings<T>(in string name) where T : ScriptableObject {

            #region validate arguments
            if (name == null) throw new ArgumentNullException("name");
            if (name.IsNullOrWhitespace()) throw new ArgumentException("name");
            #endregion

#if UNITY_EDITOR
            T settings = Resources.Load<T>(string.Concat("Settings/", name));
            if (settings != null) return settings;
            settings = ScriptableObject.CreateInstance<T>();
            SetSettings(name, settings);
            return settings;
#else
            if (SettingsCache.TryGetValue(name, out ScriptableObject cachedSettings)) // search the settings cache for the settings
                return (T)cachedSettings; // return cached settings
            else { // not found in cached settings
                T settings = Resources.Load<T>(string.Concat("Settings/", name)); // load the settings from the resource location
                SettingsCache.Add(name, settings); // store the settings in the settings cache
                return settings; // return the settings
            }
#endif

        }

        #endregion

        #endregion

    }

}