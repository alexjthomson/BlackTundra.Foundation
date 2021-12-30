using System.Collections;

using UnityEngine;

namespace BlackTundra.Foundation.SceneManagement {

    [DisallowMultipleComponent]
    sealed class SceneManagerInstance : MonoBehaviour {

        #region logic

        #region WaitForCurrentOperation

        internal void WaitForCurrentOperation() {
            StartCoroutine(WaitForCurrentOperationInternal());
        }

        #endregion

        #region WaitForCurrentOperationInternal

        private IEnumerator WaitForCurrentOperationInternal() {
            SceneOperation sceneOperation = SceneManager.currentOperation;
            AsyncOperation asyncOperation = sceneOperation.operation;
            while (!asyncOperation.isDone) {
                yield return null;
            }
            sceneOperation.OnCompleted();
        }

        #endregion

        #endregion

    }

}