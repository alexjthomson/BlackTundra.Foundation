#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.Foundation.Collections.Generic;

namespace BlackTundra.Foundation.Editor.Control {

    public sealed class ControlInspectorEditorWindow : CustomWindow {

        #region variable

        private readonly Dictionary<ControlUser, bool> foldoutStates = new Dictionary<ControlUser, bool>();

        #endregion

        #region logic

        #region CreateWindow

        [MenuItem("Window/Analysis/Control Debugger")]
        private static void CreateWindow() {
            ControlInspectorEditorWindow window = GetWindow<ControlInspectorEditorWindow>("Control Debugger");
            window.Show();
        }

        #endregion

        #region OnDestroy

        private void OnDestroy() {
            foreach (ControlUser device in foldoutStates.Keys) {
                device.OnControlStackModified -= Repaint;
            }
        }

        #endregion

        #region OnGUI

        protected sealed override void OnGUI() {
            
            EditorLayout.Title("Control Debugger");
            if (!Application.isPlaying) { // application is not playing, therefore the game is not running
                EditorLayout.Warning("Application is not in play mode; therefore, the control system will not be active.");
                return;
            }

            PackedBuffer<ControlUser> controlBuffer = ControlUser.ControlUserBuffer;
            foreach (ControlUser controlUser in controlBuffer) {

                if (!foldoutStates.TryGetValue(controlUser, out bool lastFoldoutState)) { // try get the state of the foldout for this control device
                    foldoutStates[controlUser] = lastFoldoutState; // not found, add the device to the control device dictionary
                    controlUser.OnControlStackModified += Repaint;
                }
                bool foldoutState = EditorLayout.Foldout($"ControlDevice [id: {controlUser.id}]", lastFoldoutState);
                if (foldoutState != lastFoldoutState) foldoutStates[controlUser] = foldoutState; // update the foldout state

                if (foldoutState) { // the foldout is open
                    EditorLayout.Info($"Is Primary: {(ControlUser.main == controlUser ? "true" : "false")}");
                    EditorLayout.Title("Control Stack");
                    IControllable[] controlStack = controlUser.GetControlStack();
                    for (int i = 0; i < controlStack.Length; i++) {
                        EditorLayout.StartVerticalBox();
                        IControllable controllable = controlStack[i];
                        if (controllable is Component component) { // controllable is a component
                            if (component != null) {
                                EditorLayout.Info(
                                    $"GameObject Name: {component.name}\n" +
                                    $"GameObject Tag: {component.tag}\n" +
                                    $"Component Type: {component.GetType().Name}"
                                );
                            } else {
                                EditorLayout.Error("Null Reference");
                            }
                            EditorLayout.ReferenceField(component);
                        } else if (controllable is object obj) { // controllable is unknown type
                            EditorLayout.Info($"Object Type: {obj.GetType().Name}");
                        } else {
                            EditorLayout.Info("Unknown Type");
                        }
                        EditorLayout.EndVerticalBox();
                    }

                }

            }

        }

        #endregion

        #endregion

    }

}
#endif