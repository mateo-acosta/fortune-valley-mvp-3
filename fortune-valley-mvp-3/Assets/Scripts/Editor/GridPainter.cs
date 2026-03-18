using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Advanced painting tools for the grid editor.
    /// Provides fill, line, and rectangle tools.
    /// </summary>
    public static class GridPainter
    {
        // ═══════════════════════════════════════════════════════════════
        // FILL TOOL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Flood fill from a starting position.
        /// Changes all connected tiles of the same type to the new type.
        /// </summary>
        public static void FloodFill(GridMapData mapData, int startX, int startY, TileType newType)
        {
            if (!mapData.IsValidPosition(startX, startY))
            {
                return;
            }

            TileType originalType = mapData.GetTileType(startX, startY);

            // Don't fill if the type is already the same
            if (originalType == newType)
            {
                return;
            }

            Undo.RecordObject(mapData, "Flood Fill");

            // BFS flood fill
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            queue.Enqueue(new Vector2Int(startX, startY));

            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();

                if (visited.Contains(pos))
                {
                    continue;
                }

                if (!mapData.IsValidPosition(pos.x, pos.y))
                {
                    continue;
                }

                if (mapData.GetTileType(pos) != originalType)
                {
                    continue;
                }

                visited.Add(pos);
                mapData.SetTileType(pos, newType);

                // Add neighbors
                queue.Enqueue(new Vector2Int(pos.x + 1, pos.y));
                queue.Enqueue(new Vector2Int(pos.x - 1, pos.y));
                queue.Enqueue(new Vector2Int(pos.x, pos.y + 1));
                queue.Enqueue(new Vector2Int(pos.x, pos.y - 1));
            }

            EditorUtility.SetDirty(mapData);
        }

        // ═══════════════════════════════════════════════════════════════
        // LINE TOOL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw a line between two points using Bresenham's algorithm.
        /// </summary>
        public static void DrawLine(GridMapData mapData, int x0, int y0, int x1, int y1, TileType type, int brushSize = 1)
        {
            Undo.RecordObject(mapData, "Draw Line");

            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                PaintBrush(mapData, x0, y0, type, brushSize);

                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int e2 = 2 * err;

                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            EditorUtility.SetDirty(mapData);
        }

        // ═══════════════════════════════════════════════════════════════
        // RECTANGLE TOOL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Draw a filled rectangle.
        /// </summary>
        public static void FillRectangle(GridMapData mapData, int x0, int y0, int x1, int y1, TileType type)
        {
            Undo.RecordObject(mapData, "Fill Rectangle");

            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (mapData.IsValidPosition(x, y))
                    {
                        mapData.SetTileType(x, y, type);
                    }
                }
            }

            EditorUtility.SetDirty(mapData);
        }

        /// <summary>
        /// Draw a rectangle outline.
        /// </summary>
        public static void DrawRectangleOutline(GridMapData mapData, int x0, int y0, int x1, int y1, TileType type, int brushSize = 1)
        {
            Undo.RecordObject(mapData, "Draw Rectangle");

            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);

            // Top and bottom edges
            for (int x = minX; x <= maxX; x++)
            {
                PaintBrush(mapData, x, minY, type, brushSize);
                PaintBrush(mapData, x, maxY, type, brushSize);
            }

            // Left and right edges
            for (int y = minY + 1; y < maxY; y++)
            {
                PaintBrush(mapData, minX, y, type, brushSize);
                PaintBrush(mapData, maxX, y, type, brushSize);
            }

            EditorUtility.SetDirty(mapData);
        }

        // ═══════════════════════════════════════════════════════════════
        // ROAD TOOLS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Create a road along one axis (horizontal or vertical in grid space).
        /// </summary>
        public static void CreateRoad(GridMapData mapData, int startX, int startY, int length, bool horizontal, int width = 1)
        {
            Undo.RecordObject(mapData, "Create Road");

            for (int i = 0; i < length; i++)
            {
                for (int w = 0; w < width; w++)
                {
                    int x = horizontal ? startX + i : startX + w;
                    int y = horizontal ? startY + w : startY + i;

                    if (mapData.IsValidPosition(x, y))
                    {
                        mapData.SetTileType(x, y, TileType.Road);
                    }
                }
            }

            EditorUtility.SetDirty(mapData);
        }

        /// <summary>
        /// Create a city block pattern (roads around the perimeter).
        /// </summary>
        public static void CreateCityBlock(GridMapData mapData, int x, int y, int width, int height, TileType interiorType = TileType.Lot)
        {
            Undo.RecordObject(mapData, "Create City Block");

            // Fill interior
            for (int dy = 1; dy < height - 1; dy++)
            {
                for (int dx = 1; dx < width - 1; dx++)
                {
                    if (mapData.IsValidPosition(x + dx, y + dy))
                    {
                        mapData.SetTileType(x + dx, y + dy, interiorType);
                    }
                }
            }

            // Draw road perimeter
            DrawRectangleOutline(mapData, x, y, x + width - 1, y + height - 1, TileType.Road, 1);

            EditorUtility.SetDirty(mapData);
        }

        // ═══════════════════════════════════════════════════════════════
        // BRUSH HELPER
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Paint a single tile or brush area.
        /// </summary>
        private static void PaintBrush(GridMapData mapData, int centerX, int centerY, TileType type, int brushSize)
        {
            int halfBrush = brushSize / 2;

            for (int dy = -halfBrush; dy <= halfBrush; dy++)
            {
                for (int dx = -halfBrush; dx <= halfBrush; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (mapData.IsValidPosition(x, y))
                    {
                        mapData.SetTileType(x, y, type);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PATTERN GENERATORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Generate a basic city grid pattern with roads and lots.
        /// </summary>
        public static void GenerateBasicCityPattern(GridMapData mapData, int blockSize = 5, int roadWidth = 1)
        {
            Undo.RecordObject(mapData, "Generate City Pattern");

            // Start with all empty
            mapData.ClearAll();

            int stride = blockSize + roadWidth;

            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    bool isRoadX = (x % stride) < roadWidth;
                    bool isRoadY = (y % stride) < roadWidth;

                    if (isRoadX || isRoadY)
                    {
                        mapData.SetTileType(x, y, TileType.Road);
                    }
                    else
                    {
                        // Alternate between Lot and Building for variety
                        bool isLot = ((x / stride) + (y / stride)) % 3 == 0;
                        mapData.SetTileType(x, y, isLot ? TileType.Lot : TileType.Building);
                    }
                }
            }

            // Add some parks
            int parkSpacing = stride * 3;
            for (int py = blockSize; py < mapData.Height - blockSize; py += parkSpacing)
            {
                for (int px = blockSize; px < mapData.Width - blockSize; px += parkSpacing)
                {
                    FillRectangle(mapData, px, py, px + blockSize - 1, py + blockSize - 1, TileType.Park);
                }
            }

            EditorUtility.SetDirty(mapData);
        }
    }
}
