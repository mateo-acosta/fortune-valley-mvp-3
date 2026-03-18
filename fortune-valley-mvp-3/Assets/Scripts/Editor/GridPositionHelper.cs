using UnityEngine;
using UnityEditor;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Editor helper that displays grid position information for selected objects.
    /// Helps with manually aligning objects to the isometric grid.
    /// </summary>
    [InitializeOnLoad]
    public static class GridPositionHelper
    {
        // Default grid config values (used if no config asset exists)
        private const float DEFAULT_TILE_WIDTH = 1.0f;
        private const float DEFAULT_TILE_HEIGHT = 0.5f;

        static GridPositionHelper()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            // Repaint scene view to update gizmos
            SceneView.RepaintAll();
        }

        /// <summary>
        /// Draw grid position info in the Scene view for selected objects.
        /// </summary>
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawGridPositionGizmo(Transform transform, GizmoType gizmoType)
        {
            if (transform == null) return;

            Vector3 worldPos = transform.position;
            Vector2Int gridPos = WorldToGrid(worldPos);
            Vector3 snappedPos = GridToWorld(gridPos.x, gridPos.y);

            // Draw snap target position
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireCube(snappedPos, new Vector3(0.8f, 0.1f, 0.4f));

            // Draw line from current to snap position
            if (Vector3.Distance(worldPos, snappedPos) > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(worldPos, snappedPos);
            }

            // Draw label with grid info
            string label = $"Grid: ({gridPos.x}, {gridPos.y})\nSnap: ({snappedPos.x:F2}, {snappedPos.z:F2})";
            Handles.Label(worldPos + Vector3.up * 2f, label, EditorStyles.whiteBoldLabel);
        }

        /// <summary>
        /// Convert world position to grid coordinates.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float halfWidth = DEFAULT_TILE_WIDTH * 0.5f;
            float halfHeight = DEFAULT_TILE_HEIGHT * 0.5f;

            float gridX = (worldPos.x / halfWidth + worldPos.z / halfHeight) * 0.5f;
            float gridY = (worldPos.z / halfHeight - worldPos.x / halfWidth) * 0.5f;

            return new Vector2Int(Mathf.RoundToInt(gridX), Mathf.RoundToInt(gridY));
        }

        /// <summary>
        /// Convert grid coordinates to world position.
        /// </summary>
        public static Vector3 GridToWorld(int gridX, int gridY)
        {
            float halfWidth = DEFAULT_TILE_WIDTH * 0.5f;
            float halfHeight = DEFAULT_TILE_HEIGHT * 0.5f;

            float worldX = (gridX - gridY) * halfWidth;
            float worldZ = (gridX + gridY) * halfHeight;

            return new Vector3(worldX, 0f, worldZ);
        }
    }

    /// <summary>
    /// Custom inspector extension that shows grid position for any GameObject.
    /// </summary>
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    public class TransformGridPositionEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _defaultEditor;

        private void OnEnable()
        {
            // Get the default Transform editor
            _defaultEditor = CreateEditor(targets, typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.TransformInspector"));
        }

        private void OnDisable()
        {
            if (_defaultEditor != null)
            {
                DestroyImmediate(_defaultEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            // Draw default Transform inspector
            _defaultEditor.OnInspectorGUI();

            EditorGUILayout.Space(10);

            // Draw grid position info
            EditorGUILayout.LabelField("Grid Position", EditorStyles.boldLabel);

            Transform t = (Transform)target;
            Vector2Int gridPos = GridPositionHelper.WorldToGrid(t.position);
            Vector3 snappedPos = GridPositionHelper.GridToWorld(gridPos.x, gridPos.y);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Grid Coordinates: ({gridPos.x}, {gridPos.y})");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Snapped Position: ({snappedPos.x:F3}, {snappedPos.z:F3})");
            EditorGUILayout.EndHorizontal();

            // Snap button
            EditorGUILayout.Space(5);
            if (GUILayout.Button("Snap to Grid", GUILayout.Height(25)))
            {
                Undo.RecordObject(t, "Snap to Grid");
                t.position = new Vector3(snappedPos.x, t.position.y, snappedPos.z);
            }

            // Custom position input
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Set Grid Position:", GUILayout.Width(110));

            int newX = EditorGUILayout.IntField(gridPos.x, GUILayout.Width(50));
            int newY = EditorGUILayout.IntField(gridPos.y, GUILayout.Width(50));

            if (GUILayout.Button("Apply", GUILayout.Width(50)))
            {
                Vector3 newWorldPos = GridPositionHelper.GridToWorld(newX, newY);
                Undo.RecordObject(t, "Set Grid Position");
                t.position = new Vector3(newWorldPos.x, t.position.y, newWorldPos.z);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
