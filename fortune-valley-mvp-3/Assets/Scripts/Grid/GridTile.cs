using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Runtime representation of a single tile in the grid.
    /// Used for scene visualization and click detection.
    /// </summary>
    public class GridTile : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════

        [Header("Tile Data")]
        [SerializeField] private Vector2Int _gridPosition;
        [SerializeField] private TileType _tileType;

        [Header("References")]
        [SerializeField] private CityLotDefinition _linkedLot;
        [SerializeField] private GameObject _placedAssetInstance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public Vector2Int GridPosition => _gridPosition;
        public TileType TileType => _tileType;
        public CityLotDefinition LinkedLot => _linkedLot;
        public bool HasPlacedAsset => _placedAssetInstance != null;

        // ═══════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize the tile with grid data.
        /// </summary>
        public void Initialize(Vector2Int gridPos, TileType type)
        {
            _gridPosition = gridPos;
            _tileType = type;
            gameObject.name = $"Tile_{gridPos.x}_{gridPos.y}";
        }

        /// <summary>
        /// Link this tile to a CityLotDefinition.
        /// Only valid for tiles of type Lot.
        /// </summary>
        public void LinkToCityLot(CityLotDefinition lot)
        {
            if (_tileType != TileType.Lot)
            {
                Debug.LogWarning($"[GridTile] Cannot link lot to non-Lot tile at {_gridPosition}");
                return;
            }

            _linkedLot = lot;
        }

        // ═══════════════════════════════════════════════════════════════
        // ASSET MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Place an asset on this tile.
        /// </summary>
        public void PlaceAsset(FortuneValley.Core.PlaceableAsset asset, float rotation = 0f)
        {
            // Remove existing asset first
            ClearAsset();

            if (asset == null || asset.Prefab == null)
            {
                return;
            }

            // Instantiate the prefab
            _placedAssetInstance = Instantiate(asset.Prefab, transform);
            _placedAssetInstance.name = asset.DisplayName;

            // Apply transform settings
            _placedAssetInstance.transform.localPosition = new Vector3(0, asset.HeightOffset, 0);
            _placedAssetInstance.transform.localRotation = Quaternion.Euler(0, rotation + asset.DefaultRotation, 0);
            _placedAssetInstance.transform.localScale = Vector3.one * asset.Scale;
        }

        /// <summary>
        /// Remove the placed asset from this tile.
        /// </summary>
        public void ClearAsset()
        {
            if (_placedAssetInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_placedAssetInstance);
                }
                else
                {
                    DestroyImmediate(_placedAssetInstance);
                }

                _placedAssetInstance = null;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR GIZMOS
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw a small marker at tile center
            Gizmos.color = GetGizmoColor();
            Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 0.1f, 0.4f));
        }

        private void OnDrawGizmosSelected()
        {
            // Highlight when selected
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 0.2f, 0.45f));
        }

        private Color GetGizmoColor()
        {
            return _tileType switch
            {
                TileType.Empty => Color.gray,
                TileType.Road => new Color(0.3f, 0.3f, 0.3f),
                TileType.Park => Color.green,
                TileType.Building => Color.blue,
                TileType.Lot => new Color(1f, 0.8f, 0f),
                TileType.Water => Color.cyan,
                TileType.Special => Color.magenta,
                _ => Color.white
            };
        }
#endif
    }
}
