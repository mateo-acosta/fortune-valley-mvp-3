using UnityEngine;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Static utility class for isometric coordinate conversions.
    /// Handles conversion between grid coordinates and world positions.
    /// </summary>
    public static class IsometricUtils
    {
        /// <summary>
        /// Convert grid coordinates to world position.
        /// Uses standard isometric projection.
        /// </summary>
        /// <param name="gridX">Grid X coordinate</param>
        /// <param name="gridY">Grid Y coordinate</param>
        /// <param name="tileWidth">Tile width in world units</param>
        /// <param name="tileHeight">Tile height in world units</param>
        /// <returns>World position (Y is always 0)</returns>
        public static Vector3 GridToWorld(int gridX, int gridY, float tileWidth, float tileHeight)
        {
            // Isometric projection formula:
            // worldX = (gridX - gridY) * (tileWidth / 2)
            // worldZ = (gridX + gridY) * (tileHeight / 2)
            float halfWidth = tileWidth * 0.5f;
            float halfHeight = tileHeight * 0.5f;

            float worldX = (gridX - gridY) * halfWidth;
            float worldZ = (gridX + gridY) * halfHeight;

            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// Convert grid coordinates to world position using config.
        /// </summary>
        public static Vector3 GridToWorld(int gridX, int gridY, IsometricGridConfig config)
        {
            return GridToWorld(gridX, gridY, config.TileWidth, config.TileHeight);
        }

        /// <summary>
        /// Convert grid coordinates to world position using config.
        /// </summary>
        public static Vector3 GridToWorld(Vector2Int gridPos, IsometricGridConfig config)
        {
            return GridToWorld(gridPos.x, gridPos.y, config.TileWidth, config.TileHeight);
        }

        /// <summary>
        /// Convert world position to grid coordinates.
        /// Inverse of the isometric projection.
        /// </summary>
        /// <param name="worldPos">World position</param>
        /// <param name="tileWidth">Tile width in world units</param>
        /// <param name="tileHeight">Tile height in world units</param>
        /// <returns>Grid coordinates (may be fractional, use rounding)</returns>
        public static Vector2 WorldToGridFloat(Vector3 worldPos, float tileWidth, float tileHeight)
        {
            // Inverse isometric projection:
            // gridX = (worldX / halfWidth + worldZ / halfHeight) / 2
            // gridY = (worldZ / halfHeight - worldX / halfWidth) / 2
            float halfWidth = tileWidth * 0.5f;
            float halfHeight = tileHeight * 0.5f;

            float gridX = (worldPos.x / halfWidth + worldPos.z / halfHeight) * 0.5f;
            float gridY = (worldPos.z / halfHeight - worldPos.x / halfWidth) * 0.5f;

            return new Vector2(gridX, gridY);
        }

        /// <summary>
        /// Convert world position to grid coordinates (integer).
        /// Rounds to nearest tile.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, float tileWidth, float tileHeight)
        {
            Vector2 floatGrid = WorldToGridFloat(worldPos, tileWidth, tileHeight);
            return new Vector2Int(
                Mathf.RoundToInt(floatGrid.x),
                Mathf.RoundToInt(floatGrid.y)
            );
        }

        /// <summary>
        /// Convert world position to grid coordinates using config.
        /// </summary>
        public static Vector2Int WorldToGrid(Vector3 worldPos, IsometricGridConfig config)
        {
            return WorldToGrid(worldPos, config.TileWidth, config.TileHeight);
        }

        /// <summary>
        /// Snap a world position to the nearest tile center.
        /// </summary>
        public static Vector3 SnapToGrid(Vector3 worldPos, float tileWidth, float tileHeight)
        {
            Vector2Int gridPos = WorldToGrid(worldPos, tileWidth, tileHeight);
            return GridToWorld(gridPos.x, gridPos.y, tileWidth, tileHeight);
        }

        /// <summary>
        /// Snap a world position to the nearest tile center using config.
        /// </summary>
        public static Vector3 SnapToGrid(Vector3 worldPos, IsometricGridConfig config)
        {
            return SnapToGrid(worldPos, config.TileWidth, config.TileHeight);
        }

        /// <summary>
        /// Check if grid coordinates are within bounds.
        /// </summary>
        public static bool IsInBounds(int gridX, int gridY, int width, int height)
        {
            return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
        }

        /// <summary>
        /// Check if grid coordinates are within bounds using config.
        /// </summary>
        public static bool IsInBounds(Vector2Int gridPos, IsometricGridConfig config)
        {
            return IsInBounds(gridPos.x, gridPos.y, config.GridWidth, config.GridHeight);
        }

        /// <summary>
        /// Get the four corner positions of a tile in world space.
        /// Useful for drawing tile outlines.
        /// </summary>
        public static Vector3[] GetTileCorners(int gridX, int gridY, float tileWidth, float tileHeight)
        {
            Vector3 center = GridToWorld(gridX, gridY, tileWidth, tileHeight);
            float halfW = tileWidth * 0.5f;
            float halfH = tileHeight * 0.5f;

            // Isometric diamond corners (clockwise from top)
            return new Vector3[]
            {
                center + new Vector3(0, 0, halfH),       // Top
                center + new Vector3(halfW, 0, 0),       // Right
                center + new Vector3(0, 0, -halfH),      // Bottom
                center + new Vector3(-halfW, 0, 0)       // Left
            };
        }

        /// <summary>
        /// Get the four corner positions of a tile using config.
        /// </summary>
        public static Vector3[] GetTileCorners(Vector2Int gridPos, IsometricGridConfig config)
        {
            return GetTileCorners(gridPos.x, gridPos.y, config.TileWidth, config.TileHeight);
        }
    }
}
