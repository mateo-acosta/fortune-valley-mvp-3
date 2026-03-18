using UnityEngine;
using UnityEditor;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Custom inspector for GridMapData.
    /// Provides quick access to editing tools.
    /// </summary>
    [CustomEditor(typeof(GridMapData))]
    public class GridMapDataEditor : UnityEditor.Editor
    {
        private GridMapData _mapData;

        private void OnEnable()
        {
            _mapData = (GridMapData)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Info", EditorStyles.boldLabel);

            // Show grid statistics
            if (_mapData.Config != null)
            {
                EditorGUILayout.LabelField($"Size: {_mapData.Width} x {_mapData.Height}");
                EditorGUILayout.LabelField($"Total Tiles: {_mapData.Width * _mapData.Height}");

                EditorGUILayout.Space(5);

                // Tile counts
                EditorGUILayout.LabelField("Tile Counts:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
                {
                    int count = _mapData.CountTilesOfType(type);
                    if (count > 0)
                    {
                        EditorGUILayout.LabelField($"{type}: {count}");
                    }
                }

                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a Grid Config to initialize the map.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            // Initialize button
            EditorGUILayout.BeginHorizontal();

            if (_mapData.Config != null)
            {
                if (GUILayout.Button("Initialize/Reset", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Initialize Grid",
                        "This will reset all tiles to Empty. Continue?",
                        "Initialize", "Cancel"))
                    {
                        Undo.RecordObject(_mapData, "Initialize Grid");
                        _mapData.Initialize(_mapData.Config);
                        EditorUtility.SetDirty(_mapData);
                    }
                }
            }

            if (GUILayout.Button("Open Grid Editor", GUILayout.Height(25)))
            {
                var window = EditorWindow.GetWindow<GridEditorWindow>();
                window.Show();

                // Set this map data in the editor
                // The window will need to reference this asset
            }

            EditorGUILayout.EndHorizontal();

            // Pattern generation
            if (_mapData.Config != null)
            {
                EditorGUILayout.Space(5);

                if (GUILayout.Button("Generate City Pattern", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Generate City Pattern",
                        "This will replace all tiles with a city grid pattern. Continue?",
                        "Generate", "Cancel"))
                    {
                        GridPainter.GenerateBasicCityPattern(_mapData);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Custom inspector for IsometricGridConfig.
    /// </summary>
    [CustomEditor(typeof(IsometricGridConfig))]
    public class IsometricGridConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            var config = (IsometricGridConfig)target;

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total tiles: {config.GridWidth * config.GridHeight}");
            EditorGUILayout.LabelField($"World size: {config.GridWidth * config.TileWidth:F1} x {config.GridHeight * config.TileHeight:F1}");
        }
    }

    /// <summary>
    /// Custom inspector for PlaceableAsset.
    /// </summary>
    [CustomEditor(typeof(Core.PlaceableAsset))]
    public class PlaceableAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            var asset = (Core.PlaceableAsset)target;

            // Show allowed placement summary
            EditorGUILayout.LabelField("Placement Summary", EditorStyles.boldLabel);

            if (asset.AllowedTileTypes != null && asset.AllowedTileTypes.Length > 0)
            {
                string allowed = string.Join(", ", asset.AllowedTileTypes);
                EditorGUILayout.LabelField($"Can place on: {allowed}");
            }
            else
            {
                EditorGUILayout.LabelField("Can place on: Any (except Empty)");
            }

            EditorGUILayout.LabelField($"Blocks placement: {asset.BlocksPlacement}");
            EditorGUILayout.LabelField($"Allow stacking: {asset.AllowStacking}");

            // Prefab preview
            if (asset.Prefab != null)
            {
                EditorGUILayout.Space(5);
                var preview = AssetPreview.GetAssetPreview(asset.Prefab);
                if (preview != null)
                {
                    GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
                }
            }
        }
    }
}
