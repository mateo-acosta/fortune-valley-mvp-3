using System;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Stores the tile type data for the city grid.
    /// This ScriptableObject persists the level layout between sessions.
    /// </summary>
    [CreateAssetMenu(fileName = "CityMap", menuName = "Fortune Valley/Grid/City Map")]
    public class GridMapData : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // TILE DATA
        // ═══════════════════════════════════════════════════════════════

        [Header("Grid Configuration")]
        [Tooltip("Reference to grid configuration")]
        [SerializeField] private IsometricGridConfig _config;

        [Header("Tile Data")]
        [Tooltip("Flattened array of tile types (row-major order)")]
        [SerializeField] private TileType[] _tiles;

        [Header("Placed Assets")]
        [Tooltip("References to placed assets at each tile position")]
        [SerializeField] private PlacedAssetData[] _placedAssets;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public IsometricGridConfig Config => _config;

        public int Width => _config != null ? _config.GridWidth : 30;
        public int Height => _config != null ? _config.GridHeight : 30;

        // ═══════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the grid with default empty tiles.
        /// Call this when creating a new map.
        /// </summary>
        public void Initialize(IsometricGridConfig config)
        {
            _config = config;
            int size = Width * Height;
            _tiles = new TileType[size];
            _placedAssets = new PlacedAssetData[size];

            // All tiles start empty
            for (int i = 0; i < size; i++)
            {
                _tiles[i] = TileType.Empty;
                _placedAssets[i] = new PlacedAssetData();
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Ensure tile arrays are properly sized.
        /// </summary>
        public void ValidateArrays()
        {
            int size = Width * Height;

            if (_tiles == null || _tiles.Length != size)
            {
                var oldTiles = _tiles;
                _tiles = new TileType[size];

                // Copy old data if possible
                if (oldTiles != null)
                {
                    int copyCount = Mathf.Min(oldTiles.Length, size);
                    Array.Copy(oldTiles, _tiles, copyCount);
                }
            }

            if (_placedAssets == null || _placedAssets.Length != size)
            {
                var oldAssets = _placedAssets;
                _placedAssets = new PlacedAssetData[size];

                // Initialize new entries
                for (int i = 0; i < size; i++)
                {
                    _placedAssets[i] = new PlacedAssetData();
                }

                // Copy old data if possible
                if (oldAssets != null)
                {
                    int copyCount = Mathf.Min(oldAssets.Length, size);
                    Array.Copy(oldAssets, _placedAssets, copyCount);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // TILE TYPE METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Convert grid coordinates to array index.
        /// </summary>
        private int GetIndex(int x, int y)
        {
            return y * Width + x;
        }

        /// <summary>
        /// Check if coordinates are valid.
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Get the tile type at the given position.
        /// </summary>
        public TileType GetTileType(int x, int y)
        {
            if (!IsValidPosition(x, y))
            {
                return TileType.Empty;
            }

            ValidateArrays();
            return _tiles[GetIndex(x, y)];
        }

        /// <summary>
        /// Get the tile type at the given position.
        /// </summary>
        public TileType GetTileType(Vector2Int pos)
        {
            return GetTileType(pos.x, pos.y);
        }

        /// <summary>
        /// Set the tile type at the given position.
        /// </summary>
        public void SetTileType(int x, int y, TileType type)
        {
            if (!IsValidPosition(x, y))
            {
                return;
            }

            ValidateArrays();
            _tiles[GetIndex(x, y)] = type;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Set the tile type at the given position.
        /// </summary>
        public void SetTileType(Vector2Int pos, TileType type)
        {
            SetTileType(pos.x, pos.y, type);
        }

        // ═══════════════════════════════════════════════════════════════
        // PLACED ASSET METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get placed asset data at the given position.
        /// </summary>
        public PlacedAssetData GetAssetAt(int x, int y)
        {
            if (!IsValidPosition(x, y))
            {
                return null;
            }

            ValidateArrays();
            return _placedAssets[GetIndex(x, y)];
        }

        /// <summary>
        /// Get placed asset data at the given position.
        /// </summary>
        public PlacedAssetData GetAssetAt(Vector2Int pos)
        {
            return GetAssetAt(pos.x, pos.y);
        }

        /// <summary>
        /// Place an asset at the given position.
        /// </summary>
        public void PlaceAsset(int x, int y, FortuneValley.Core.PlaceableAsset asset, float rotation = 0f)
        {
            if (!IsValidPosition(x, y))
            {
                return;
            }

            ValidateArrays();
            int index = GetIndex(x, y);
            _placedAssets[index] = new PlacedAssetData
            {
                asset = asset,
                rotation = rotation
            };

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Remove the asset at the given position.
        /// </summary>
        public void RemoveAsset(int x, int y)
        {
            if (!IsValidPosition(x, y))
            {
                return;
            }

            ValidateArrays();
            _placedAssets[GetIndex(x, y)] = new PlacedAssetData();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Check if an asset is placed at the given position.
        /// </summary>
        public bool HasAssetAt(int x, int y)
        {
            var data = GetAssetAt(x, y);
            return data != null && data.asset != null;
        }

        // ═══════════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Clear all tiles to empty.
        /// </summary>
        public void ClearAll()
        {
            ValidateArrays();
            int size = Width * Height;

            for (int i = 0; i < size; i++)
            {
                _tiles[i] = TileType.Empty;
                _placedAssets[i] = new PlacedAssetData();
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Fill a rectangular area with a tile type.
        /// </summary>
        public void FillRect(int startX, int startY, int width, int height, TileType type)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    SetTileType(x, y, type);
                }
            }
        }

        /// <summary>
        /// Get count of tiles with specific type.
        /// </summary>
        public int CountTilesOfType(TileType type)
        {
            ValidateArrays();
            int count = 0;
            foreach (var tile in _tiles)
            {
                if (tile == type) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Data for a placed asset on a tile.
    /// </summary>
    [Serializable]
    public class PlacedAssetData
    {
        public FortuneValley.Core.PlaceableAsset asset;
        public float rotation;
    }
}
