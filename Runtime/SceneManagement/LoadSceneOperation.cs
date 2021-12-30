using System;

using UnityEngine;

namespace BlackTundra.Foundation.SceneManagement {

    public sealed class LoadSceneOperation : SceneOperation {

        #region delegate

        public delegate void LoadSceneOperationCallbackDelegate(in int sceneIndex);

        #endregion

        #region variable

        private readonly int sceneIndex;

        private readonly LoadSceneOperationCallbackDelegate callback;

        #endregion

        #region constructor

        internal LoadSceneOperation(in int sceneIndex, in AsyncOperation operation, in LoadSceneOperationCallbackDelegate callback) : base(operation) {
            this.callback = callback;
        }

        #endregion

        #region logic

        protected internal sealed override void OnCompleted() {
            if (callback != null) {
                try {
                    callback.Invoke(sceneIndex);
                } catch (Exception exception) {
                    SceneManager.ConsoleFormatter.Error(
                        $"Failed to invoke {nameof(LoadSceneOperation)} callback.",
                        exception
                    );
                }
            }
        }

        #endregion

    }

}