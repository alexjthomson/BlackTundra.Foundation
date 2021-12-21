using BlackTundra.Foundation.Utility;

using System;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.Foundation.Editor.Utility {

    public sealed class EditorMenu<T> {

        #region constant

        public const float MinListWidth = 64.0f;

        #endregion

        #region delegate

        /// <summary>
        /// Used for drawing an entry to the list.
        /// </summary>
        public delegate void DrawMenuEntryDelegate(in int index, in T entry);

        /// <summary>
        /// Used for drawing the content from an entry to the menu.
        /// </summary>
        /// <returns>Title of the content.</returns>
        public delegate string DrawMenuEntryContentDelegate(in int index, in T entry);

        /// <summary>
        /// Used to create a new entry.
        /// </summary>
        public delegate T DrawMenuCreateEntryDelegate();

        /// <summary>
        /// Used when en entry is removed.
        /// </summary>
        public delegate void DrawMenuRemoveEntryDelegate(in T entry);

        #endregion

        #region variable

        private readonly DrawMenuEntryDelegate drawEntryCallback;
        private readonly DrawMenuEntryContentDelegate drawContentCallback;
        private readonly DrawMenuCreateEntryDelegate createEntryCallback;
        private readonly DrawMenuRemoveEntryDelegate removeEntryCallback;

        private readonly float listWidth;
        private readonly bool allowListModification;

        private float listScrollHeight;
        private float contentScrollHeight;

        private int selectedIndex;
        private string title;

        #endregion

        #region constructor

        public EditorMenu(
            in DrawMenuEntryDelegate drawEntryCallback,
            in DrawMenuEntryContentDelegate drawContentCallback,
            in float listWidth = 384.0f,
            in bool allowListModification = false,
            in DrawMenuCreateEntryDelegate createEntryCallback = null,
            in DrawMenuRemoveEntryDelegate removeEntryCallback = null
        ) {

            this.drawEntryCallback = drawEntryCallback ?? throw new ArgumentNullException("drawEntryCallback");
            this.drawContentCallback = drawContentCallback ?? throw new ArgumentNullException("drawContentCallback");
            this.createEntryCallback = allowListModification ? createEntryCallback : null;
            this.removeEntryCallback = allowListModification ? removeEntryCallback : null;

            this.listWidth = listWidth > MinListWidth ? listWidth : MinListWidth;
            this.allowListModification = allowListModification;

            listScrollHeight = 0.0f;
            contentScrollHeight = 0.0f;

            selectedIndex = -1;
            title = string.Empty;

        }

        #endregion

        #region logic

        public T[] Draw(T[] array) {

            if (array == null) throw new ArgumentNullException("array");

            EditorGUILayout.BeginHorizontal();

            #region draw list

            EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));

            #region list controls

            if (createEntryCallback != null) {

                EditorGUILayout.BeginHorizontal();

                if (EditorLayout.Button("Add First")) array = array.AddFirst(createEntryCallback());
                if (EditorLayout.Button("Add Last")) array = array.AddLast(createEntryCallback());

                EditorGUILayout.EndHorizontal();

            }

            #endregion

            #region draw list

            EditorLayout.StartScrollView(ref listScrollHeight);

            for (int i = 0; i < array.Length; i++) {

                EditorGUILayout.BeginHorizontal("box");
                drawEntryCallback(i, array[i]);

                if (allowListModification) {

                    bool stop = false;

                    if (EditorLayout.XButton("/\\")) {

                        //
                        stop = true;

                    } else if (EditorLayout.XButton("\\/")) {

                        //
                        stop = true;

                    } else if (EditorLayout.XButton()) {

                        array = array.RemoveAt(i, out T temp);
                        removeEntryCallback?.Invoke(temp);
                        stop = true;

                    }

                    if (stop) {

                        GUI.FocusControl(null);
                        EditorGUILayout.EndHorizontal();
                        break;

                    }

                }

                if (EditorLayout.XButton("S")) {

                    selectedIndex = selectedIndex == i ? -1 : i;
                    GUI.FocusControl(null);

                }

                EditorGUILayout.EndHorizontal();

            }

            EditorLayout.EndScrollView();

            #endregion

            EditorGUILayout.EndVertical();

            #endregion

            #region draw content

            EditorGUILayout.BeginVertical();

            if (selectedIndex < 0 || selectedIndex >= array.Length) { // no item selected (or invalid item selected)

                EditorLayout.Title("No Entry Selected");
                EditorLayout.Info("Select an entry by pressing the 'S' button.");

                title = string.Empty;

            } else {

                EditorLayout.StartScrollView(ref contentScrollHeight);

                EditorLayout.StartHorizontalBox();

                bool previous = EditorLayout.XButton("<");
                bool next = EditorLayout.XButton(">");

                EditorLayout.Title(string.Format("{0} - {1}", selectedIndex.ToString(), title));
                bool deselect = EditorLayout.XButton();

                EditorLayout.EndHorizontalBox();

                title = drawContentCallback(selectedIndex, array[selectedIndex]);

                EditorLayout.EndScrollView();

                if (deselect) {
                    selectedIndex = -1;
                    title = string.Empty;
                } else if (next || previous) {
                    if (next) selectedIndex++; else selectedIndex--;
                    title = string.Empty;
                    GUI.FocusControl(null);
                }

            }

            EditorGUILayout.EndVertical();

            #endregion

            EditorGUILayout.EndHorizontal();

            return array;

        }

        #endregion

    }

}