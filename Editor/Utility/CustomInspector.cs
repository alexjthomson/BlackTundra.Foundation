using UnityEditor;

using UnityEngine;

namespace BlackTundra.Foundation.Editor.Utility {

    public abstract class CustomInspector : UnityEditor.Editor {

        #region variable

        protected bool modified = false;

        #endregion

        #region property

        public bool AutoSave { get; protected set; } = false;

        #endregion

        #region logic

        #region OnInspectorGUI

        public sealed override void OnInspectorGUI() {
            bool autoSave = AutoSave;
            bool isPlaying = Application.isPlaying;
            if (isPlaying && autoSave) EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            DrawInspector();
            if (!isPlaying && ((autoSave && EditorGUI.EndChangeCheck()) || modified)) {
                MarkAsDirty(target, serializedObject);
                modified = false;
            }
        }

        #endregion

        #region DrawInspector

        /// <summary>
        /// Called when the inspector is expected to be drawn.
        /// </summary>
        protected abstract void DrawInspector();

        #endregion

        #region MarkAsDirty

        /// <summary>
        /// Marks the object that CustomInspector is targeting as dirty.
        /// </summary>
        public virtual void MarkAsDirty() => modified = true;

        public static void MarkAsDirty(in Object target, in SerializedObject serializedObject = null) {
            Undo.FlushUndoRecordObjects();
            serializedObject?.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        #endregion

        #region IsPrefab

        public static bool IsPrefab(in GameObject gameObject) {
            return gameObject != null
                && PrefabUtility.GetPrefabAssetType(gameObject) == PrefabAssetType.Regular
                && PrefabUtility.GetPrefabInstanceHandle(gameObject) == null
                && !gameObject.scene.IsValid();
        }

        #endregion

        #endregion

    }

}