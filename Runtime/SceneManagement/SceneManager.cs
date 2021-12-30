using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using InternalSceneManager = UnityEngine.SceneManagement.SceneManager;

namespace BlackTundra.Foundation.SceneManagement {

    public static class SceneManager {

        #region constant

        internal static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(SceneManager));

        #endregion

        #region variable

        /// <summary>
        /// Current <see cref="SceneOperation"/> being performed by the <see cref="SceneManager"/>.
        /// </summary>
        internal static SceneOperation currentOperation;

        /// <summary>
        /// <see cref="SceneManagerInstance"/> responsible for handling the <see cref="currentOperation"/>.
        /// </summary>
        private static SceneManagerInstance instance = null;

        #endregion

        #region logic

        #region LoadScene

        public static void LoadScene(in int sceneIndex) {
            InternalSceneManager.LoadScene(sceneIndex, new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.None));
        }

        public static void LoadScene(in Scene scene) {
            InternalSceneManager.LoadScene(scene.buildIndex, new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.None));
        }

        public static void LoadScene(in int sceneIndex, LoadSceneMode loadSceneMode) {
            InternalSceneManager.LoadScene(sceneIndex, new LoadSceneParameters(loadSceneMode, LocalPhysicsMode.None));
        }

        public static void LoadScene(in Scene scene, LoadSceneMode loadSceneMode) {
            InternalSceneManager.LoadScene(scene.buildIndex, new LoadSceneParameters(loadSceneMode, LocalPhysicsMode.None));
        }

        public static void LoadScene(in int sceneIndex, in LoadSceneMode loadSceneMode, in LocalPhysicsMode localPhysicsMode) {
            InternalSceneManager.LoadScene(sceneIndex, new LoadSceneParameters(loadSceneMode, localPhysicsMode));
        }

        public static void LoadScene(in Scene scene, in LoadSceneMode loadSceneMode, in LocalPhysicsMode localPhysicsMode) {
            InternalSceneManager.LoadScene(scene.buildIndex, new LoadSceneParameters(loadSceneMode, localPhysicsMode));
        }

        #endregion

        #region LoadSceneAsync

        public static void LoadSceneAsync(in Scene scene, in LoadSceneOperation.LoadSceneOperationCallbackDelegate callback) {
            LoadSceneInternal(
                scene.buildIndex,
                new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.None),
                callback
            );
        }

        public static void LoadSceneAsync(in Scene scene, in LoadSceneParameters parameters, in LoadSceneOperation.LoadSceneOperationCallbackDelegate callback) {
            LoadSceneInternal(scene.buildIndex, parameters, callback);
        }

        #endregion

        #region LoadSceneInternal

        private static void LoadSceneInternal(
            in int sceneIndex,
            in LoadSceneParameters parameters,
            in LoadSceneOperation.LoadSceneOperationCallbackDelegate callback
        ) {
            if (currentOperation != null) throw new Exception($"{nameof(SceneManager)} already has an active operation.");
            currentOperation = new LoadSceneOperation(
                sceneIndex,
                InternalSceneManager.LoadSceneAsync(sceneIndex, parameters),
                callback
            );
            UpdateInstance();
            instance.WaitForCurrentOperation();
        }

        #endregion

        #region UpdateInstance

        private static void UpdateInstance() {
            if (instance != null) return;
            GameObject gameObject = new GameObject(
                nameof(SceneManagerInstance),
                typeof(SceneManagerInstance)
            ) {
                layer = LayerMask.NameToLayer("Ignore Raycast"),
                isStatic = true,
                hideFlags = HideFlags.DontSave
            };
            instance = gameObject.GetComponent<SceneManagerInstance>();
        }

        #endregion

        #endregion

    }

}