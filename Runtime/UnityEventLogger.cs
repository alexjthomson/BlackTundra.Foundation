using UnityEngine.SceneManagement;

namespace BlackTundra.Foundation {

    internal static class UnityEventLogger {

        #region logic

        #region Initialise

        internal static void Initialise() {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => Console.Info(
                $"[SceneManager] Loaded scene \"{scene.name}\" (buildIndex: {scene.buildIndex}, path: \"{scene.path}\")."
            );
            SceneManager.sceneUnloaded += (Scene scene) => Console.Info(
                $"[SceneManager] Unloaded scene \"{scene.name}\" (buildIndex: {scene.buildIndex}, path: \"{scene.path}\")."
            );
            SceneManager.activeSceneChanged += (Scene lastScene, Scene nextScene) => Console.Info(
                $"[SceneManager] Active scene changed \"{lastScene.name}\" (buildIndex: {lastScene.buildIndex}, path: \"{lastScene.path}\") -> \"{nextScene.name}\" (buildIndex: {nextScene.buildIndex}, path: \"{nextScene.path}\")."
            );
        }

        #endregion

        #endregion

    }

}