using System;

using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BlackTundra.Foundation.Utility {

    public static class ExceptionUtility {

        #region logic

        #region Handle

        public static void Handle(this Exception exception, in string message = null, in Object obj = null) {
            if (exception == null) throw new ArgumentNullException("exception");
            if (obj == null) {
                Debug.LogException(exception);
                if (message != null) Debug.LogError(message);
            } else {
                Debug.LogException(exception, obj);
                if (message != null) Debug.LogError(message, obj);
            }
        }

        #endregion

        #endregion

    }

}