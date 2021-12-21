using UnityEngine.SceneManagement;

namespace BlackTundra.Foundation {

    internal static class UnityEventLogger {

        #region constant

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter("SceneManager");

        #endregion

        #region logic

        #region Initialise

        internal static void Initialise() {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => ConsoleFormatter.Info(
                $"Loaded scene `{scene.name}` (buildIndex: {scene.buildIndex}, path: `{scene.path}`)."
            );
            SceneManager.sceneUnloaded += (Scene scene) => ConsoleFormatter.Info(
                $"Unloaded scene `{scene.name}` (buildIndex: {scene.buildIndex}, path: `{scene.path}`)."
            );
            SceneManager.activeSceneChanged += (Scene lastScene, Scene nextScene) => ConsoleFormatter.Info(
                $"Active scene changed `{lastScene.name}` (buildIndex: {lastScene.buildIndex}, path: `{lastScene.path}`) -> `{nextScene.name}` (buildIndex: {nextScene.buildIndex}, path: `{nextScene.path}`)."
            );
        }

        #endregion

        #endregion

    }

}