using UnityEngine;

namespace BlackTundra.Foundation.SceneManagement {

    public abstract class SceneOperation {

        #region variable

        public readonly AsyncOperation operation;

        #endregion

        #region property

        public float progress => operation.progress;

        #endregion

        #region constructor

        protected SceneOperation(in AsyncOperation operation) {
            this.operation = operation;
        }

        #endregion

        #region logic

        protected internal abstract void OnCompleted();

        #endregion

    }

}