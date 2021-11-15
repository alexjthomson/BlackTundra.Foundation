using BlackTundra.Foundation.Utility;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.Foundation.Editor.Utility {

    [CustomPropertyDrawer(typeof(NormalDistribution))]
    public sealed class NormalDistributionPropertyDrawer : PropertyDrawer {

        #region constant

        private static readonly GUIContent MeanGUIContent = new GUIContent("AVG");
        private static readonly GUIContent SDGUIContent = new GUIContent("SD");

        #endregion

        #region logic

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1.0f : 2.0f);
        }

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var _mean = property.FindPropertyRelative(nameof(NormalDistribution.mean));
            var _standardDeviation = property.FindPropertyRelative(nameof(NormalDistribution.standardDeviation));
            EditorGUI.BeginProperty(position, label, property);
            Rect column = position;
            float meanLabelWidth = 28.0f;
            float sdLabelWidth = 20.0f;
            float valueWidth = (position.width - meanLabelWidth - sdLabelWidth) * 0.5f;
            column.width = meanLabelWidth;
            EditorGUI.LabelField(column, MeanGUIContent);
            column.x += column.width;
            column.width = valueWidth;
            EditorGUI.PropertyField(column, _mean, GUIContent.none);
            column.x += column.width;
            column.width = sdLabelWidth;
            EditorGUI.LabelField(column, SDGUIContent);
            column.x += column.width;
            column.width = valueWidth;
            EditorGUI.PropertyField(column, _standardDeviation, GUIContent.none);
            EditorGUI.EndProperty();
        }

        #endregion

    }

    [CustomPropertyDrawer(typeof(ClampedNormalDistribution))]
    public sealed class ClampedNormalDistributionPropertyDrawer : PropertyDrawer {

        #region constant

        private static readonly GUIContent RangeGUIContent = new GUIContent("Range");

        #endregion

        #region logic

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1.0f : 2.0f);
        }

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var _distribution = property.FindPropertyRelative(nameof(ClampedNormalDistribution.distribution));
            var _lowerClamp = property.FindPropertyRelative(nameof(ClampedNormalDistribution.lowerClamp));
            var _upperClamp = property.FindPropertyRelative(nameof(ClampedNormalDistribution.upperClamp));
            EditorGUI.BeginProperty(position, label, property);
            Rect column = position;
            float labelWidth = position.width * 0.35f;
            float valueWidth = position.width - labelWidth;
            float distributionWidth = valueWidth * 0.6f;
            float clampWidth = valueWidth - distributionWidth;
            float clampLabelWidth = 38.0f;
            float clampPropertyWidth = (clampWidth - clampLabelWidth) * 0.5f;
            column.width = labelWidth;
            EditorGUI.LabelField(position, label);
            column.x += column.width;
            column.width = distributionWidth;
            EditorGUI.PropertyField(column, _distribution, GUIContent.none);
            column.x += column.width;
            column.width = clampLabelWidth;
            EditorGUI.LabelField(column, RangeGUIContent);
            column.x += column.width;
            column.width = clampPropertyWidth;
            EditorGUI.PropertyField(column, _lowerClamp, GUIContent.none);
            column.x += column.width;
            column.width = clampPropertyWidth;
            EditorGUI.PropertyField(column, _upperClamp, GUIContent.none);
            EditorGUI.EndProperty();
        }

        #endregion

    }

}