using UnityEngine;
using FortuneValley.Grid;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines an asset that can be placed on the grid.
    /// Specifies which tile types are valid for placement.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlaceableAsset", menuName = "Fortune Valley/Grid/Placeable Asset")]
    public class PlaceableAsset : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // IDENTITY
        // ═══════════════════════════════════════════════════════════════

        [Header("Identity")]
        [Tooltip("Display name shown in editor")]
        [SerializeField] private string _displayName;

        [Tooltip("Category for organizing in editor palette")]
        [SerializeField] private AssetCategory _category;

        // ═══════════════════════════════════════════════════════════════
        // PREFAB REFERENCE
        // ═══════════════════════════════════════════════════════════════

        [Header("Prefab")]
        [Tooltip("The prefab or model to instantiate")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Preview icon for editor (optional)")]
        [SerializeField] private Sprite _previewIcon;

        // ═══════════════════════════════════════════════════════════════
        // PLACEMENT RULES
        // ═══════════════════════════════════════════════════════════════

        [Header("Placement Rules")]
        [Tooltip("Which tile types this asset can be placed on")]
        [SerializeField] private TileType[] _allowedTileTypes;

        [Tooltip("Can multiple of this asset be placed on the same tile?")]
        [SerializeField] private bool _allowStacking = false;

        [Tooltip("Does this asset block other assets from being placed?")]
        [SerializeField] private bool _blocksPlacement = true;

        // ═══════════════════════════════════════════════════════════════
        // TRANSFORM DEFAULTS
        // ═══════════════════════════════════════════════════════════════

        [Header("Transform Defaults")]
        [Tooltip("Y offset from ground when placed")]
        [SerializeField] private float _heightOffset = 0f;

        [Tooltip("Scale multiplier when placed")]
        [SerializeField] private float _scale = 1f;

        [Tooltip("Default rotation (Y axis, degrees)")]
        [SerializeField] private float _defaultRotation = 0f;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public AssetCategory Category => _category;
        public GameObject Prefab => _prefab;
        public Sprite PreviewIcon => _previewIcon;
        public TileType[] AllowedTileTypes => _allowedTileTypes;
        public bool AllowStacking => _allowStacking;
        public bool BlocksPlacement => _blocksPlacement;
        public float HeightOffset => _heightOffset;
        public float Scale => _scale;
        public float DefaultRotation => _defaultRotation;

        // ═══════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if this asset can be placed on the given tile type.
        /// </summary>
        public bool CanPlaceOn(TileType tileType)
        {
            if (_allowedTileTypes == null || _allowedTileTypes.Length == 0)
            {
                // If no restrictions specified, allow anywhere except Empty
                return tileType != TileType.Empty;
            }

            foreach (var allowed in _allowedTileTypes)
            {
                if (allowed == tileType)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            // Auto-generate display name from asset name if empty
            if (string.IsNullOrEmpty(_displayName) && !string.IsNullOrEmpty(name))
            {
                _displayName = name;
            }
        }
    }

    /// <summary>
    /// Categories for organizing placeable assets.
    /// </summary>
    public enum AssetCategory
    {
        Road,           // Road segments, intersections
        Park,           // Trees, benches, flowers
        Building,       // Generic buildings, houses
        Vehicle,        // Cars, trucks, buses
        Decoration,     // Signs, lights, misc props
        Special         // Player restaurant, landmarks
    }
}
