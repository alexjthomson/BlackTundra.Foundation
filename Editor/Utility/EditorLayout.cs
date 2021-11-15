using System;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditorInternal;

namespace BlackTundra.Foundation.Editor.Utility {

    public static class EditorLayout {

        #region constant

        private static readonly string[] BoolPopupOptions = new string[] { "True", "False" };

        #endregion

        #region nested

        private enum ObjectType {
            Null,
            String,
            Integer,
            Single,
            Double,
            Boolean,
            Vector2,
            Vector3,
            Vector4
        }

        #endregion

        #region variable

        private static GUIStyle foldoutStyle = null;

        private static GUIStyle xButtonStyle = null;

        #endregion

        #region property

        internal static GUIStyle FoldoutStyle {
            get {
                if (foldoutStyle == null) {
                    foldoutStyle = new GUIStyle(EditorStyles.foldout) {
                        fontStyle = FontStyle.Bold
                    };
                }
                return foldoutStyle;
            }
        }

        internal static GUIStyle XButtonStyle {
            get {
                if (xButtonStyle == null) {
                    xButtonStyle = new GUIStyle(GUI.skin.button) {
                        fontSize = 8,
                        alignment = TextAnchor.MiddleCenter,
                        contentOffset = Vector2.zero,
                        imagePosition = ImagePosition.TextOnly,
                        wordWrap = false,
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                return xButtonStyle;
            }
        }

        #endregion

        #region logic

        #region Title

        public static void Title(in string text) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        public static void Title(in string text, in float width) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Width(width));

        #endregion

        #region Label

        public static void Label(in string value) => EditorGUILayout.LabelField(value);

        public static void Label(in string value, in float width) => EditorGUILayout.LabelField(value, GUILayout.Width(width));

        public static void Label(in GUIContent content, in string value) => EditorGUILayout.LabelField(content, value);

        #endregion

        #region Toolbar

        public static int Toolbar(in int selectedIndex, in string[] options) => GUILayout.Toolbar(selectedIndex, options);

        public static int Toolbar(in int selectedIndex, in GUIContent[] options) => GUILayout.Toolbar(selectedIndex, options);

        //public static void Toolbar(ref int selectedIndex, in string[] options) => selectedIndex = GUILayout.Toolbar(selectedIndex, options);

        #endregion

        #region Foldout

        public static bool Foldout(in GUIContent content, in bool foldout) => EditorGUILayout.Foldout(foldout, content, FoldoutStyle);

        public static bool Foldout(in GUIContent content, in bool foldout, in bool toggleOnLabelClick) => EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, FoldoutStyle);

        public static bool Foldout(in GUIContent content, in bool foldout, in bool toggleOnLabelClick, in GUIStyle style) => EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, style);

        public static bool Foldout(in string content, in bool foldout) => EditorGUILayout.Foldout(foldout, content, FoldoutStyle);

        public static bool Foldout(in string content, in bool foldout, in bool toggleOnLabelClick) => EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, FoldoutStyle);

        public static bool Foldout(in string content, in bool foldout, in bool toggleOnLabelClick, in GUIStyle style) => EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, style);

        #endregion

        #region Space

        public static void Space() => EditorGUILayout.Space();

        #endregion

        #region TextField

        public static string TextField(in string value) => EditorGUILayout.TextField(value);

        public static string TextField(in string label, in string value) => EditorGUILayout.TextField(label, value);

        public static string TextField(in GUIContent content, in string value) => EditorGUILayout.TextField(content, value);

        #endregion

        #region TextAreaField

        public static string TextAreaField(in string content, in string value) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            string temp = EditorGUILayout.TextArea(value);
            EditorGUILayout.EndVertical();
            return temp;
        }

        public static string TextAreaField(in GUIContent content, in string value) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
            string temp = EditorGUILayout.TextArea(value);
            EditorGUILayout.EndVertical();
            return temp;
        }

        #endregion

        #region ReferenceField

        public static T ReferenceField<T>(in T value, in bool allowSceneObjects = false) where T : UnityEngine.Object => (T)EditorGUILayout.ObjectField(value, typeof(T), allowSceneObjects);

        public static T ReferenceField<T>(in string content, in T value, in bool allowSceneObjects = false) where T : UnityEngine.Object => (T)EditorGUILayout.ObjectField(content, value, typeof(T), allowSceneObjects);

        public static T ReferenceField<T>(in GUIContent content, in T value, in bool allowSceneObjects = false) where T : UnityEngine.Object => (T)EditorGUILayout.ObjectField(content, value, typeof(T), allowSceneObjects);

        #endregion

        #region Slider

        public static float Slider(in GUIContent content, in float value, in float min, in float max) => EditorGUILayout.Slider(content, value, min, max);

        #endregion

        #region IntegerField

        public static int IntegerField(in int value) => EditorGUILayout.IntField(value);
        public static int IntegerField(in string content, in int value) => EditorGUILayout.IntField(content, value);
        public static int IntegerField(in GUIContent content, in int value) => EditorGUILayout.IntField(content, value);

        public static int IntegerField(in string content, in int value, in int min) => Mathf.Max(EditorGUILayout.IntField(content, value), min);
        public static int IntegerField(in GUIContent content, in int value, in int min) => Mathf.Max(EditorGUILayout.IntField(content, value), min);

        public static int IntegerField(in string content, in int value, in int min, in int max) => Mathf.Clamp(EditorGUILayout.IntField(content, value), min, max);
        public static int IntegerField(in GUIContent content, in int value, in int min, in int max) => Mathf.Clamp(EditorGUILayout.IntField(content, value), min, max);

        #endregion

        #region FloatField

        public static float FloatField(in float value) => EditorGUILayout.FloatField(value);
        public static float FloatField(in string content, in float value) => EditorGUILayout.FloatField(content, value);
        public static float FloatField(in GUIContent content, in float value) => EditorGUILayout.FloatField(content, value);

        public static float FloatField(in GUIContent content, in float value, in float min) => Mathf.Max(EditorGUILayout.FloatField(content, value), min);

        public static float FloatField(in GUIContent content, in float value, in float min, in float max) => Mathf.Clamp(EditorGUILayout.FloatField(content, value), min, max);

        #endregion

        #region BooleanField

        public static bool BooleanField(in bool value) => EditorGUILayout.Toggle(value);

        public static bool BooleanField(in string content, in bool value) => EditorGUILayout.Toggle(content, value);

        public static bool BooleanField(in GUIContent content, in bool value) => EditorGUILayout.Toggle(content, value);

        #endregion

        #region ObjectField

        public static object ObjectField(object value, in float height) => ObjectField(value, GUILayout.Height(height));

        public static object ObjectField(object value, params GUILayoutOption[] options) {

            ObjectType type = GetObjectType(value);
            EditorGUILayout.BeginHorizontal();
            ObjectType newType = EnumField(type, GUILayout.Width(80.0f));
            if (type != newType) value = ConvertObjectType(value, type, newType);
            switch (newType) {
                case ObjectType.Null: EditorGUILayout.LabelField("null", options); value = null; break;
                case ObjectType.String: value = EditorGUILayout.TextField((string)value, options); break;
                case ObjectType.Integer: value = EditorGUILayout.IntField((int)value, options); break;
                case ObjectType.Single: value = EditorGUILayout.FloatField((float)value, options); break;
                case ObjectType.Double: value = EditorGUILayout.DoubleField((double)value, options); break;
                case ObjectType.Boolean: value = EditorGUILayout.Toggle((bool)value, options); break;
                case ObjectType.Vector2: value = Vector2Field((Vector2)value, options); break;
                case ObjectType.Vector3: value = Vector3Field((Vector3)value, options); break;
                case ObjectType.Vector4: value = Vector4Field((Vector4)value, options); break;
                default: EditorGUILayout.LabelField("unknown", options); break;
            }
            EditorGUILayout.EndHorizontal();
            return value;

        }

        #endregion

        #region GetObjectType

        private static ObjectType GetObjectType(in object value) {

            if (value == null) return ObjectType.Null;
            else if (value is string) return ObjectType.String;
            else if (value is int) return ObjectType.Integer;
            else if (value is float) return ObjectType.Single;
            else if (value is double) return ObjectType.Double;
            else if (value is bool) return ObjectType.Boolean;
            else if (value is Vector2) return ObjectType.Vector2;
            else if (value is Vector3) return ObjectType.Vector3;
            else if (value is Vector4) return ObjectType.Vector4;
            else return ObjectType.Null;

        }

        #endregion

        #region ConvertObjectType

        private static object ConvertObjectType(in object value, in ObjectType currentType, in ObjectType targetType) {

            if (targetType == ObjectType.Null) return null;
            if (targetType == currentType) return value;

            switch (currentType) {

                case ObjectType.Null: {

                    switch (targetType) {

                        case ObjectType.String: return string.Empty;
                        case ObjectType.Integer: return 0;
                        case ObjectType.Single: return 0.0f;
                        case ObjectType.Double: return 0.0;
                        case ObjectType.Boolean: return false;
                        case ObjectType.Vector2: return Vector2.zero;
                        case ObjectType.Vector3: return Vector3.zero;
                        case ObjectType.Vector4: return Vector4.zero;
                        default: return value;

                    }

                }

                case ObjectType.String: {

                    string str = (string)value;
                    switch (targetType) {

                        case ObjectType.Integer: return int.TryParse(str, out int i) ? i : 0;
                        case ObjectType.Single: return float.TryParse(str, out float f) ? f : 0.0f;
                        case ObjectType.Double: return double.TryParse(str, out double d) ? d : 0.0;
                        case ObjectType.Boolean: return str != null && (str.ToLower() == "true" || str == "1");
                        case ObjectType.Vector2: return Vector2.zero;
                        case ObjectType.Vector3: return Vector3.zero;
                        case ObjectType.Vector4: return Vector4.zero;
                        default: return value;

                    }

                }

                case ObjectType.Integer: {

                    int i = (int)value;
                    switch (targetType) {

                        case ObjectType.String: return i.ToString();
                        case ObjectType.Single: return (float)i;
                        case ObjectType.Double: return (double)i;
                        case ObjectType.Boolean: return i != 0;
                        case ObjectType.Vector2: return new Vector2(0.0f, i);
                        case ObjectType.Vector3: return new Vector3(0.0f, i, 0.0f);
                        case ObjectType.Vector4: return new Vector4(0.0f, 0.0f, 0.0f, i);
                        default: return value;

                    }

                }

                case ObjectType.Single: {

                    float f = (float)value;
                    switch (targetType) {

                        case ObjectType.String: return f.ToString();
                        case ObjectType.Integer: return (int)f;
                        case ObjectType.Double: return (double)f;
                        case ObjectType.Boolean: return ((int)f) != 0;
                        case ObjectType.Vector2: return new Vector2(0.0f, f);
                        case ObjectType.Vector3: return new Vector3(0.0f, f, 0.0f);
                        case ObjectType.Vector4: return new Vector4(0.0f, 0.0f, 0.0f, f);
                        default: return value;

                    }

                }

                case ObjectType.Double: {

                    double d = (double)value;
                    switch (targetType) {

                        case ObjectType.String: return d.ToString();
                        case ObjectType.Integer: return (int)d;
                        case ObjectType.Single: return (float)d;
                        case ObjectType.Boolean: return ((int)d) != 0;
                        case ObjectType.Vector2: return new Vector2(0.0f, (float)d);
                        case ObjectType.Vector3: return new Vector3(0.0f, (float)d, 0.0f);
                        case ObjectType.Vector4: return new Vector4(0.0f, 0.0f, 0.0f, (float)d);
                        default: return value;

                    }

                }

                case ObjectType.Boolean: {

                    bool b = (bool)value;
                    switch (targetType) {

                        case ObjectType.String: return b ? "true" : "false";
                        case ObjectType.Integer: return b ? 1 : 0;
                        case ObjectType.Single: return b ? 1.0f : 0.0f;
                        case ObjectType.Double: return b ? 1.0 : 0.0;
                        case ObjectType.Vector2: return b ? Vector2.one : Vector2.zero;
                        case ObjectType.Vector3: return b ? Vector3.one : Vector3.zero;
                        case ObjectType.Vector4: return b ? Vector4.one : Vector4.zero;
                        default: return value;

                    }

                }

                case ObjectType.Vector2: {

                    Vector2 v = (Vector2)value;
                    switch (targetType) {

                        case ObjectType.String: return v.ToString();
                        case ObjectType.Integer: return v.y;
                        case ObjectType.Single: return v.y;
                        case ObjectType.Double: return v.y;
                        case ObjectType.Boolean: return false;
                        case ObjectType.Vector3: return new Vector3(v.x, v.y, 0.0f);
                        case ObjectType.Vector4: return new Vector4(v.x, v.y, 0.0f, 0.0f);
                        default: return value;

                    }

                }

                case ObjectType.Vector3: {

                    Vector3 v = (Vector3)value;
                    switch (targetType) {

                        case ObjectType.String: return v.ToString();
                        case ObjectType.Integer: return v.y;
                        case ObjectType.Single: return v.y;
                        case ObjectType.Double: return v.y;
                        case ObjectType.Boolean: return false;
                        case ObjectType.Vector2: return new Vector2(v.x, v.y);
                        case ObjectType.Vector4: return new Vector4(v.x, v.y, 0.0f, 0.0f);
                        default: return value;

                    }

                }

                case ObjectType.Vector4: {

                    Vector4 v = (Vector4)value;
                    switch (targetType) {

                        case ObjectType.String: return v.ToString();
                        case ObjectType.Integer: return v.w;
                        case ObjectType.Single: return v.w;
                        case ObjectType.Double: return v.w;
                        case ObjectType.Boolean: return false;
                        case ObjectType.Vector2: return new Vector2(v.x, v.y);
                        case ObjectType.Vector3: return new Vector3(v.x, v.y, 0.0f);
                        default: return value;

                    }

                }

                default: return value;

            }

        }

        #endregion

        #region EnumField

        public static T EnumField<T>(in T value) where T : Enum => (T)EditorGUILayout.EnumPopup(value);
        public static T EnumField<T>(in T value, params GUILayoutOption[] options) where T : Enum => (T)EditorGUILayout.EnumPopup(value, options);
        public static T EnumField<T>(in string content, in T value) where T : Enum => (T)EditorGUILayout.EnumPopup(content, value);
        public static T EnumField<T>(in string content, in T value, params GUILayoutOption[] options) where T : Enum => (T)EditorGUILayout.EnumPopup(content, value, options);
        public static T EnumField<T>(in GUIContent content, in T value) where T : Enum => (T)EditorGUILayout.EnumPopup(content, value);
        public static T EnumField<T>(in GUIContent content, in T value, params GUILayoutOption[] options) where T : Enum => (T)EditorGUILayout.EnumPopup(content, value, options);

        #endregion

        #region DropdownField

        public static int DropdownField(in int value, in string[] options, in int[] optionValues) => EditorGUILayout.IntPopup(value, options, optionValues);
        public static int DropdownField(in string content, in int value, in string[] options, in int[] optionValues) => EditorGUILayout.IntPopup(content, value, options, optionValues);
        public static int DropdownField(in GUIContent content, in int value, in GUIContent[] options, in int[] optionValues) => EditorGUILayout.IntPopup(content, value, options, optionValues);

        #endregion

        #region LayerMaskField

        public static LayerMask LayerMaskField(in LayerMask layerMask) {
            int currentValue = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
            int newValue = EditorGUILayout.MaskField(currentValue, InternalEditorUtility.layers);
            return newValue != currentValue ? InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newValue) : layerMask;
        }

        public static LayerMask LayerMaskField(in string content, in LayerMask layerMask) {
            int currentValue = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
            int newValue = EditorGUILayout.MaskField(content, currentValue, InternalEditorUtility.layers);
            return newValue != currentValue ? InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newValue) : layerMask;
        }

        public static LayerMask LayerMaskField(in GUIContent content, in LayerMask layerMask) {
            int currentValue = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
            int newValue = EditorGUILayout.MaskField(content, currentValue, InternalEditorUtility.layers);
            return newValue != currentValue ? InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(newValue) : layerMask;
        }

        #endregion

        #region MaskField

        public static int MaskField<T>(in string content, in int mask) => EditorGUILayout.MaskField(content, mask, Enum.GetNames(typeof(T)));
        public static int MaskField<T>(in GUIContent content, in int mask) => EditorGUILayout.MaskField(content, mask, Enum.GetNames(typeof(T)));
        public static int MaskField(in string content, in int mask, in string[] names) => EditorGUILayout.MaskField(content, mask, names);
        public static int MaskField(in GUIContent content, in int mask, in string[] names) => EditorGUILayout.MaskField(content, mask, names);
        public static int MaskField(in string content, in int mask, in int[] values, in string[] names) {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (names == null) throw new ArgumentNullException(nameof(names));
            int entryCount = values.Length;
            if (entryCount != names.Length) throw new ArgumentException(nameof(names));
            int value = 0;
            for (int i = entryCount - 1; i >= 0; i--) {
                if ((mask & values[i]) != 0) { // mask bit set
                    value |= 1 << i; // set current bit
                }
            }
            value = EditorGUILayout.MaskField(content, value, names);
            int finalValue = 0;
            for (int i = entryCount - 1; i >= 0; i--) {
                if ((value & (1 << i)) != 0) {
                    finalValue |= values[i];
                }
            }
            return finalValue;
        }

        #endregion

        #region VersionField

        public static Version VersionField(in Version version) {

            EditorGUILayout.BeginHorizontal();
            ushort major = (ushort)Mathf.Clamp(EditorGUILayout.IntField(version.major), 0, ushort.MaxValue);
            ushort minor = (ushort)Mathf.Clamp(EditorGUILayout.IntField(version.minor, GUILayout.Width(32.0f)), 0, ushort.MaxValue);
            ushort patch = (ushort)Mathf.Clamp(EditorGUILayout.IntField(version.release, GUILayout.Width(32.0f)), 0, ushort.MaxValue);
            Version.ReleaseType type = (Version.ReleaseType)EditorGUILayout.EnumPopup(version.type, GUILayout.Width(64.0f));
            EditorGUILayout.EndHorizontal();

            if (major != version.major || minor != version.minor || patch != version.release || type != version.type) return new Version(major, minor, patch, type);
            return version;

        }

        #endregion

        #region SceneBuildIndexField

        public static int SceneBuildIndexField(in GUIContent content, in int buildIndex) {

            EditorGUILayout.BeginHorizontal();

            int newBuildIndex = content != null ? EditorGUILayout.IntField(content, buildIndex) : EditorGUILayout.IntField(buildIndex);
            int totalScenes = SceneManager.sceneCountInBuildSettings;
            if (totalScenes == 0) EditorGUILayout.LabelField("No Scenes in Build");
            else if (newBuildIndex >= totalScenes) EditorGUILayout.LabelField("Out of Bounds");
            else {

                if (newBuildIndex < 0) newBuildIndex = 0;
                Scene scene = SceneManager.GetSceneByBuildIndex(newBuildIndex);
                EditorGUILayout.LabelField(scene.IsValid() ? scene.name : "Invalid Scene");

            }

            EditorGUILayout.EndHorizontal();

            return newBuildIndex;

        }

        #endregion

        #region Field

        public static void Field(ref object value) {

            if (value == null) EditorGUILayout.SelectableLabel("null", GUILayout.Width(128), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            else if (value is string stringValue) value = EditorGUILayout.TextField(stringValue);
            else if (value is int intValue) value = EditorGUILayout.IntField(intValue, GUILayout.Width(128));
            else if (value is float floatValue) value = EditorGUILayout.FloatField(floatValue, GUILayout.Width(128));
            else if (value is double doubleValue) value = EditorGUILayout.DoubleField(doubleValue, GUILayout.Width(128));
            else if (value is bool boolValue) value = EditorGUILayout.Popup(boolValue ? 0 : 1, BoolPopupOptions, GUILayout.Width(128)) == 0;
            else if (value is char charValue) {

                string input = EditorGUILayout.TextField(charValue.ToString(), GUILayout.Width(128));
                if (input.Length > 0) value = input[0];

            } else if (value is byte byteValue) EditorGUILayout.SelectableLabel(byteValue.ToString(), GUILayout.Width(128), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            else if (value is uint uintValue) value = (uint)EditorGUILayout.LongField(uintValue, GUILayout.Width(128));
            else if (value is short shortValue) value = (short)EditorGUILayout.IntField(shortValue, GUILayout.Width(128));
            else if (value is ushort ushortValue) value = (ushort)EditorGUILayout.IntField(ushortValue, GUILayout.Width(128));
            else if (value is long longValue) value = EditorGUILayout.LongField(longValue, GUILayout.Width(128));
            else if (value is ulong ulongValue) EditorGUILayout.SelectableLabel(string.Format("{0} (unsigned long) (editing not supported)", ulongValue), GUILayout.Width(128), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            else if (value is LayerMask layermaskValue) value = LayerMaskField(layermaskValue);
            else if (value is Version versionValue) value = VersionField(versionValue);
            else if (value is Vector2 vector2Value) value = Vector2Field(vector2Value);
            else if (value is Vector3 vector3Value) value = Vector3Field(vector3Value);
            else if (value is Vector4 vector4Value) value = Vector4Field(vector4Value);
            else EditorGUILayout.SelectableLabel("Unknown Type", GUILayout.Width(128), GUILayout.Height(EditorGUIUtility.singleLineHeight));

        }

        #endregion

        #region InspectorField

        public static bool InspectorField(in UnityEngine.Object obj, in string fieldName) {

            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");

            return InspectorField(new SerializedObject(obj), fieldName);

        }

        public static bool InspectorField(in SerializedObject obj, in string fieldName) {

            if (obj == null) throw new ArgumentNullException("obj");
            if (fieldName == null) throw new ArgumentNullException("fieldName");

            SerializedProperty tps = obj.FindProperty(fieldName);
            if (tps == null) throw new NullReferenceException("Coudn't find serialized property: " + fieldName);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(tps, true);
            if (EditorGUI.EndChangeCheck()) {
                obj.ApplyModifiedProperties();
                return true;
            }

            return false;

        }

        #endregion

        #region Vector2Field

        public static Vector2 Vector2Field(in Vector2 value, params GUILayoutOption[] options) {
            EditorGUILayout.BeginHorizontal(options);
            EditorGUILayout.LabelField("x", GUILayout.Width(8.0f));
            float x = EditorGUILayout.FloatField(value.x, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("y", GUILayout.Width(8.0f));
            float y = EditorGUILayout.FloatField(value.y, GUILayout.Width(48.0f));
            EditorGUILayout.EndHorizontal();
            return new Vector2(x, y);
        }

        public static Vector2 Vector2Field(in string content, in Vector2 value) => EditorGUILayout.Vector2Field(content, value);

        public static Vector2 Vector2Field(in GUIContent content, in Vector2 value) => EditorGUILayout.Vector2Field(content, value);

        #endregion

        #region Vector3Field

        public static Vector3 Vector3Field(in Vector3 value, params GUILayoutOption[] options) {
            EditorGUILayout.BeginHorizontal(options);
            EditorGUILayout.LabelField("x", GUILayout.Width(8.0f));
            float x = EditorGUILayout.FloatField(value.x, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("y", GUILayout.Width(8.0f));
            float y = EditorGUILayout.FloatField(value.y, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("z", GUILayout.Width(8.0f));
            float z = EditorGUILayout.FloatField(value.z, GUILayout.Width(48.0f));
            EditorGUILayout.EndHorizontal();
            return new Vector3(x, y, z);
        }

        public static Vector3 Vector3Field(in string content, in Vector3 value) => EditorGUILayout.Vector3Field(content, value);

        public static Vector3 Vector3Field(in GUIContent content, in Vector3 value) => EditorGUILayout.Vector3Field(content, value);

        #endregion

        #region Vector4Field

        public static Vector4 Vector4Field(in Vector4 value, params GUILayoutOption[] options) {
            EditorGUILayout.BeginHorizontal(options);
            EditorGUILayout.LabelField("x", GUILayout.Width(8.0f));
            float x = EditorGUILayout.FloatField(value.x, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("y", GUILayout.Width(8.0f));
            float y = EditorGUILayout.FloatField(value.y, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("z", GUILayout.Width(8.0f));
            float z = EditorGUILayout.FloatField(value.z, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("w", GUILayout.Width(8.0f));
            float w = EditorGUILayout.FloatField(value.w, GUILayout.Width(48.0f));
            EditorGUILayout.EndHorizontal();
            return new Vector4(x, y, z, w);
        }

        public static Vector4 Vector4Field(in string content, in Vector4 value) => EditorGUILayout.Vector4Field(content, value);

        public static Vector4 Vector4Field(in GUIContent content, in Vector4 value) => EditorGUILayout.Vector4Field(content, value);

        #endregion

        #region Vector2IntField

        public static Vector2Int Vector2IntField(in Vector2Int value, params GUILayoutOption[] options) {
            EditorGUILayout.BeginHorizontal(options);
            EditorGUILayout.LabelField("x", GUILayout.Width(8.0f));
            int x = EditorGUILayout.IntField(value.x, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("y", GUILayout.Width(8.0f));
            int y = EditorGUILayout.IntField(value.y, GUILayout.Width(48.0f));
            EditorGUILayout.EndHorizontal();
            return new Vector2Int(x, y);
        }

        public static Vector2Int Vector2IntField(in string content, in Vector2Int value) => EditorGUILayout.Vector2IntField(content, value);

        public static Vector2Int Vector2IntField(in string content, in int x, in int y) => EditorGUILayout.Vector2IntField(content, new Vector2Int(x, y));

        public static Vector2Int Vector2IntField(in GUIContent content, in Vector2Int value) => EditorGUILayout.Vector2IntField(content, value);

        public static Vector2Int Vector2IntField(in GUIContent content, in int x, in int y) => EditorGUILayout.Vector2IntField(content, new Vector2Int(x, y));

        #endregion

        #region Vector3IntField

        public static Vector3Int Vector3IntField(in Vector3Int value, params GUILayoutOption[] options) {
            EditorGUILayout.BeginHorizontal(options);
            EditorGUILayout.LabelField("x", GUILayout.Width(8.0f));
            int x = EditorGUILayout.IntField(value.x, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("y", GUILayout.Width(8.0f));
            int y = EditorGUILayout.IntField(value.y, GUILayout.Width(48.0f));
            EditorGUILayout.LabelField("z", GUILayout.Width(8.0f));
            int z = EditorGUILayout.IntField(value.z, GUILayout.Width(48.0f));
            EditorGUILayout.EndHorizontal();
            return new Vector3Int(x, y, z);
        }

        public static Vector3Int Vector3IntField(in string content, in Vector3Int value) => EditorGUILayout.Vector3IntField(content, value);

        public static Vector3Int Vector3IntField(in string content, in int x, int y, int z) => EditorGUILayout.Vector3IntField(content, new Vector3Int(x, y, z));

        public static Vector3Int Vector3IntField(in GUIContent content, in Vector3Int value) => EditorGUILayout.Vector3IntField(content, value);

        public static Vector3Int Vector3IntField(in GUIContent content, in int x, int y, int z) => EditorGUILayout.Vector3IntField(content, new Vector3Int(x, y, z));

        #endregion

        #region Button

        public static bool Button(in string text) => GUILayout.Button(text);
        public static bool Button(in string text, in float height) => GUILayout.Button(text, GUILayout.Height(height));
        public static bool Button(in string text, params GUILayoutOption[] options) => GUILayout.Button(text, options);
        public static bool Button(in GUIContent content, in string text) => GUILayout.Button(content, text);
        public static bool Button(in GUIContent content, in string text, params GUILayoutOption[] options) => GUILayout.Button(content, text, options);

        #endregion

        #region XButton

        // close (x) button
        public static bool XButton(in string text = "\u2715", in float width = 20.0f, in float height = 18.0f) => GUILayout.Button(text, XButtonStyle, GUILayout.Width(width), GUILayout.Height(height));

        #endregion

        #region UpButton

        public static bool UpButton(in float width = 20.0f, in float height = 18.0f) => GUILayout.Button("\u25b2", XButtonStyle, GUILayout.Width(width), GUILayout.Height(height));

        #endregion

        #region DownButton

        public static bool DownButton(in float width = 20.0f, in float height = 18.0f) => GUILayout.Button("\u25bc", XButtonStyle, GUILayout.Width(width), GUILayout.Height(height));

        #endregion

        #region StartScrollView

        public static void StartScrollView(ref float height) {

            height = EditorGUILayout.BeginScrollView(
                new Vector2(
                    0.0f,
                    height
                ),
                GUILayout.ExpandHeight(true)
            ).y;

        }

        public static void StartScrollView(ref float height, in float width) {

            height = EditorGUILayout.BeginScrollView(
                new Vector2(
                    0.0f,
                    height
                ),
                GUILayout.ExpandHeight(true),
                GUILayout.Width(width)
            ).y;

        }

        public static void StartScrollView(ref Vector2 value) {

            value = EditorGUILayout.BeginScrollView(
                value,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true)
            );

        }

        public static void StartScrollView(ref Vector2 value, in Vector2 size) {

            value = EditorGUILayout.BeginScrollView(
                value,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true),
                GUILayout.Width(size.x),
                GUILayout.Height(size.y)
            );

        }

        #endregion

        #region EndScrollView

        public static void EndScrollView() => EditorGUILayout.EndScrollView();

        #endregion

        #region StartHorizontalBox

        public static void StartHorizontalBox() => EditorGUILayout.BeginHorizontal("box");

        public static void StartHorizontalBox(in float height) => EditorGUILayout.BeginHorizontal("box", GUILayout.Height(height));

        public static void StartHorizontalBox(in float width, in float height) => EditorGUILayout.BeginHorizontal("box", GUILayout.Width(width), GUILayout.Height(height));

        public static void StartHorizontalBox(params GUILayoutOption[] options) => EditorGUILayout.BeginHorizontal("box", options);

        #endregion

        #region EndHorizontalBox

        public static void EndHorizontalBox() => EndHorizontal();

        #endregion

        #region StartHorizontal

        public static void StartHorizontal() => EditorGUILayout.BeginHorizontal();

        public static void StartHorizontal(in float height) => EditorGUILayout.BeginHorizontal(GUILayout.Height(height));

        public static void StartHorizontal(in float width, in float height) => EditorGUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(height));

        public static void StartHorizontal(params GUILayoutOption[] options) => EditorGUILayout.BeginHorizontal(options);

        #endregion

        #region EndHorizontal

        public static void EndHorizontal() => EditorGUILayout.EndHorizontal();

        #endregion

        #region StartVerticalBox

        public static void StartVerticalBox() => EditorGUILayout.BeginVertical("box");

        public static void StartVerticalBox(in float width) => EditorGUILayout.BeginVertical("box", GUILayout.Width(width));

        public static void StartVerticalBox(in float width, in float height) => EditorGUILayout.BeginVertical("box", GUILayout.Width(width), GUILayout.Height(height));

        public static void StartVerticalBox(params GUILayoutOption[] options) => EditorGUILayout.BeginVertical("box", options);

        #endregion

        #region EndVerticalBox

        public static void EndVerticalBox() => EndVertical();

        #endregion

        #region StartVertical

        public static void StartVertical() => EditorGUILayout.BeginVertical();

        public static void StartVertical(in float width) => EditorGUILayout.BeginVertical(GUILayout.Width(width));

        public static void StartVertical(in float width, in float height) => EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.Height(height));

        public static void StartVertical(params GUILayoutOption[] options) => EditorGUILayout.BeginVertical(options);

        #endregion

        #region EndVertical

        public static void EndVertical() => EditorGUILayout.EndVertical();

        #endregion

        #region Info

        public static void Info(in string message) => EditorGUILayout.HelpBox(message, MessageType.Info, true);

        #endregion

        #region Warning

        public static void Warning(in string message) => EditorGUILayout.HelpBox(message, MessageType.Warning, true);

        #endregion

        #region Error

        public static void Error(in string message) => EditorGUILayout.HelpBox(message, MessageType.Error);

        #endregion

        #endregion

    }

}