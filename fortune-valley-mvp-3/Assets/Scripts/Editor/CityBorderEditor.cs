using UnityEngine;
using UnityEditor;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Custom editor for CityBorder component.
    /// Provides buttons to generate and clear the border.
    /// </summary>
    [CustomEditor(typeof(CityBorder))]
    public class CityBorderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("Border Generation", EditorStyles.boldLabel);

            CityBorder border = (CityBorder)target;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate Border", GUILayout.Height(30)))
            {
                Undo.RegisterFullObjectHierarchyUndo(border.gameObject, "Generate Border");
                border.GenerateBorder();
                EditorUtility.SetDirty(border);
            }

            if (GUILayout.Button("Clear Border", GUILayout.Height(30)))
            {
                Undo.RegisterFullObjectHierarchyUndo(border.gameObject, "Clear Border");
                border.ClearBorder();
                EditorUtility.SetDirty(border);
            }

            EditorGUILayout.EndHorizontal();

            // Show bounds info
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Camera Bounds Info", EditorStyles.boldLabel);

            Bounds bounds = border.CameraBounds;
            EditorGUILayout.LabelField($"Center: {bounds.center}");
            EditorGUILayout.LabelField($"Size: {bounds.size}");
            EditorGUILayout.LabelField($"Min: {bounds.min}");
            EditorGUILayout.LabelField($"Max: {bounds.max}");
        }
    }
}
