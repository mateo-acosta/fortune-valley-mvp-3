using UnityEngine;
using UnityEditor;
using FortuneValley.Grid;
using FortuneValley.Core;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Handles drag-and-drop asset placement in the grid editor.
    /// Validates asset placement against tile type rules.
    /// </summary>
    public static class AssetPlacer
    {
        // ═══════════════════════════════════════════════════════════════
        // PLACEMENT VALIDATION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if an asset can be placed at the given grid position.
        /// </summary>
        public static bool CanPlace(PlaceableAsset asset, GridMapData mapData, int x, int y)
        {
            if (asset == null || mapData == null)
            {
                return false;
            }

            if (!mapData.IsValidPosition(x, y))
            {
                return false;
            }

            // Check tile type compatibility
            TileType tileType = mapData.GetTileType(x, y);
            if (!asset.CanPlaceOn(tileType))
            {
                return false;
            }

            // Check if position is blocked by existing asset
            if (asset.BlocksPlacement && mapData.HasAssetAt(x, y))
            {
                var existingAsset = mapData.GetAssetAt(x, y);
                if (existingAsset.asset != null && existingAsset.asset.BlocksPlacement)
                {
                    // Allow stacking only if both assets allow it
                    if (!asset.AllowStacking || !existingAsset.asset.AllowStacking)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if an asset can be placed at the given grid position.
        /// </summary>
        public static bool CanPlace(PlaceableAsset asset, GridMapData mapData, Vector2Int pos)
        {
            return CanPlace(asset, mapData, pos.x, pos.y);
        }

        /// <summary>
        /// Get a reason why placement failed.
        /// </summary>
        public static string GetPlacementError(PlaceableAsset asset, GridMapData mapData, int x, int y)
        {
            if (asset == null)
            {
                return "No asset selected";
            }

            if (mapData == null)
            {
                return "No map data";
            }

            if (!mapData.IsValidPosition(x, y))
            {
                return "Position out of bounds";
            }

            TileType tileType = mapData.GetTileType(x, y);
            if (!asset.CanPlaceOn(tileType))
            {
                string allowedTypes = string.Join(", ", asset.AllowedTileTypes);
                return $"Cannot place {asset.DisplayName} on {tileType}. Allowed: {allowedTypes}";
            }

            if (mapData.HasAssetAt(x, y))
            {
                var existingAsset = mapData.GetAssetAt(x, y);
                if (existingAsset.asset != null)
                {
                    return $"Tile already contains: {existingAsset.asset.DisplayName}";
                }
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // PLACEMENT EXECUTION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Place an asset at the given grid position.
        /// </summary>
        public static bool PlaceAsset(PlaceableAsset asset, GridMapData mapData, int x, int y, float rotation = 0f)
        {
            if (!CanPlace(asset, mapData, x, y))
            {
                return false;
            }

            Undo.RecordObject(mapData, $"Place {asset.DisplayName}");
            mapData.PlaceAsset(x, y, asset, rotation);

            EditorUtility.SetDirty(mapData);
            return true;
        }

        /// <summary>
        /// Remove an asset from the given grid position.
        /// </summary>
        public static bool RemoveAsset(GridMapData mapData, int x, int y)
        {
            if (mapData == null || !mapData.IsValidPosition(x, y))
            {
                return false;
            }

            if (!mapData.HasAssetAt(x, y))
            {
                return false;
            }

            Undo.RecordObject(mapData, "Remove Asset");
            mapData.RemoveAsset(x, y);

            EditorUtility.SetDirty(mapData);
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // DRAG AND DROP HANDLING
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Process drag and drop in the scene view.
        /// Returns true if a drop was handled.
        /// </summary>
        public static bool HandleDragAndDrop(GridMapData mapData, IsometricGridConfig config)
        {
            if (mapData == null || config == null)
            {
                return false;
            }

            Event e = Event.current;

            // Check if we're dragging a PlaceableAsset
            PlaceableAsset draggedAsset = GetDraggedAsset();

            if (draggedAsset == null)
            {
                return false;
            }

            // Get the target grid position
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float t = -ray.origin.y / ray.direction.y;

            if (t <= 0)
            {
                return false;
            }

            Vector3 hitPoint = ray.origin + ray.direction * t;
            Vector2Int gridPos = IsometricUtils.WorldToGrid(hitPoint, config);

            if (!IsometricUtils.IsInBounds(gridPos, config))
            {
                return false;
            }

            switch (e.type)
            {
                case EventType.DragUpdated:
                    // Show preview and update visual feedback
                    bool canPlace = CanPlace(draggedAsset, mapData, gridPos);
                    DragAndDrop.visualMode = canPlace
                        ? DragAndDropVisualMode.Copy
                        : DragAndDropVisualMode.Rejected;

                    // Draw preview
                    DrawPlacementPreview(draggedAsset, gridPos, config, canPlace);

                    e.Use();
                    return true;

                case EventType.DragPerform:
                    if (CanPlace(draggedAsset, mapData, gridPos))
                    {
                        DragAndDrop.AcceptDrag();
                        PlaceAsset(draggedAsset, mapData, gridPos.x, gridPos.y);
                        e.Use();
                        return true;
                    }
                    else
                    {
                        string error = GetPlacementError(draggedAsset, mapData, gridPos.x, gridPos.y);
                        EditorUtility.DisplayDialog("Cannot Place Asset", error, "OK");
                    }
                    break;

                case EventType.DragExited:
                    // Clean up preview
                    break;
            }

            return false;
        }

        /// <summary>
        /// Get the PlaceableAsset being dragged, if any.
        /// </summary>
        private static PlaceableAsset GetDraggedAsset()
        {
            var objects = DragAndDrop.objectReferences;

            foreach (var obj in objects)
            {
                if (obj is PlaceableAsset asset)
                {
                    return asset;
                }
            }

            return null;
        }

        /// <summary>
        /// Draw a preview of where the asset will be placed.
        /// </summary>
        private static void DrawPlacementPreview(PlaceableAsset asset, Vector2Int gridPos, IsometricGridConfig config, bool isValid)
        {
            Vector3 center = IsometricUtils.GridToWorld(gridPos, config);
            Vector3[] corners = IsometricUtils.GetTileCorners(gridPos, config);

            // Draw highlight on target tile
            Color previewColor = isValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
            Handles.color = previewColor;
            Handles.DrawAAConvexPolygon(corners);

            // Draw outline
            Handles.color = isValid ? Color.green : Color.red;
            Handles.DrawAAPolyLine(3f, corners[0], corners[1], corners[2], corners[3], corners[0]);

            // Draw asset name label
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = isValid ? Color.green : Color.red },
                alignment = TextAnchor.MiddleCenter
            };

            Handles.Label(center + Vector3.up * 0.5f, asset.DisplayName, style);

            // Draw icon or prefab preview if available
            if (asset.Prefab != null)
            {
                // Draw a simple cube to represent the asset
                Handles.color = new Color(1, 1, 1, 0.3f);
                Handles.CubeHandleCap(
                    0,
                    center + Vector3.up * asset.HeightOffset,
                    Quaternion.identity,
                    0.3f * asset.Scale,
                    EventType.Repaint
                );
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // SCENE VISUALIZATION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw all placed assets in the scene view.
        /// </summary>
        public static void DrawPlacedAssets(GridMapData mapData, IsometricGridConfig config)
        {
            if (mapData == null || config == null)
            {
                return;
            }

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    var assetData = mapData.GetAssetAt(x, y);
                    if (assetData?.asset != null)
                    {
                        DrawAssetGizmo(assetData.asset, x, y, config, assetData.rotation);
                    }
                }
            }
        }

        /// <summary>
        /// Draw a gizmo for a placed asset.
        /// </summary>
        private static void DrawAssetGizmo(PlaceableAsset asset, int x, int y, IsometricGridConfig config, float rotation)
        {
            Vector3 center = IsometricUtils.GridToWorld(x, y, config);
            Vector3 position = center + Vector3.up * asset.HeightOffset;

            // Draw based on category
            Color color = GetCategoryColor(asset.Category);
            Handles.color = color;

            // Draw a simple marker
            float size = 0.2f * asset.Scale;
            Quaternion rot = Quaternion.Euler(0, rotation + asset.DefaultRotation, 0);

            Handles.CubeHandleCap(0, position, rot, size, EventType.Repaint);

            // Draw label
            Handles.Label(position + Vector3.up * 0.2f, asset.DisplayName, EditorStyles.miniLabel);
        }

        /// <summary>
        /// Get a color for an asset category.
        /// </summary>
        private static Color GetCategoryColor(AssetCategory category)
        {
            return category switch
            {
                AssetCategory.Road => new Color(0.4f, 0.4f, 0.4f),
                AssetCategory.Park => new Color(0.2f, 0.8f, 0.2f),
                AssetCategory.Building => new Color(0.3f, 0.5f, 0.9f),
                AssetCategory.Vehicle => new Color(0.9f, 0.5f, 0.2f),
                AssetCategory.Decoration => new Color(0.8f, 0.8f, 0.3f),
                AssetCategory.Special => new Color(0.9f, 0.2f, 0.9f),
                _ => Color.white
            };
        }
    }
}
