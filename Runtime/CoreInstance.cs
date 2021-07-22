using UnityEngine;

namespace BlackTundra.Foundation {
    /// <summary>
    /// <see cref="MonoBehaviour"/> class responsible for initialising the <see cref="Core"/> static class.
    /// </summary>
    [DisallowMultipleComponent]
    sealed class CoreInstance : MonoBehaviour {
#pragma warning disable IDE0051 // remove unused private members
        private void Awake() => Core.InitialiseAwake();
        private void OnDestroy() => Core.Terminate();
        private void Update() => Core.Update();
        private void OnGUI() => Core.OnGUI();
#pragma warning restore IDE0051 // remove unused private members
    }
}
