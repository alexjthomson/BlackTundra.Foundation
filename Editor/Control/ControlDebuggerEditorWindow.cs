#if ENABLE_INPUT_SYSTEM

using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.Editor.Utility;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.Foundation.Editor.Control {

    public sealed class ControlInspectorEditorWindow : CustomWindow {

        #region logic

        #region CreateWindow

        [MenuItem("Window/Analysis/Control Debugger")]
        private static void CreateWindow() {
            ControlInspectorEditorWindow window = GetWindow<ControlInspectorEditorWindow>("Control Debugger");
            window.Show();
        }

        #endregion

        #region OnGUI

        protected sealed override void OnGUI() {
            EditorLayout.Title("Control Debugger");
            if (!Application.isPlaying) { // application is not playing, therefore the game is not running
                EditorLayout.Warning("Application is not in play mode; therefore, the control system will not be active.");
            }
            IControllable[] controlStackBuffer = ControlManager.ControlStack.ToArray();
            int stackSize = controlStackBuffer.Length;
            EditorLayout.Info("Stack Size: " + stackSize);
            IControllable current;
            for (int i = 0; i < stackSize; i++) {
                current = controlStackBuffer[i];
                EditorLayout.StartVerticalBox();
                if (current == null) {
                    EditorLayout.Title("null");
                } else {
                    Behaviour behaviour = current as Behaviour;
                    if (behaviour != null) {
                        EditorLayout.Title(behaviour.name);
                        EditorLayout.ReferenceField(behaviour, true);
                        EditorLayout.ReferenceField(behaviour.gameObject, true);
                    } else {
                        EditorLayout.Title(current.GetType().Name);
                    }
                }
                EditorLayout.EndVerticalBox();
            }
        }

        #endregion

        #endregion

    }

}
#endif