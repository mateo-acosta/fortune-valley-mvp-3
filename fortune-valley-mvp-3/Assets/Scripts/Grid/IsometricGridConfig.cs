using UnityEngine;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Configuration for the isometric grid system.
    /// Defines grid dimensions, tile sizes, and editor visualization colors.
    /// </summary>
    [CreateAssetMenu(fileName = "IsometricGridConfig", menuName = "Fortune Valley/Grid/Grid Config")]
    public class IsometricGridConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // GRID DIMENSIONS
        // ═══════════════════════════════════════════════════════════════

        [Header("Grid Dimensions")]
        [Tooltip("Width of the grid in tiles")]
        [SerializeField] private int _gridWidth = 30;

        [Tooltip("Height of the grid in tiles")]
        [SerializeField] private int _gridHeight = 30;

        // ═══════════════════════════════════════════════════════════════
        // TILE SIZE
        // ═══════════════════════════════════════════════════════════════

        [Header("Tile Size (World Units)")]
        [Tooltip("Width of a tile in world units (X axis)")]
        [SerializeField] private float _tileWidth = 1.0f;

        [Tooltip("Height of a tile in world units (Z axis for isometric)")]
        [SerializeField] private float _tileHeight = 0.5f;

        // ═══════════════════════════════════════════════════════════════
        // EDITOR COLORS
        // ═══════════════════════════════════════════════════════════════

        [Header("Editor Colors")]
        [SerializeField] private Color _emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private Color _roadColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color _parkColor = new Color(0.2f, 0.7f, 0.2f, 0.5f);
        [SerializeField] private Color _buildingColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);
        [SerializeField] private Color _lotColor = new Color(0.9f, 0.7f, 0.1f, 0.5f);
        [SerializeField] private Color _waterColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        [SerializeField] private Color _specialColor = new Color(0.7f, 0.2f, 0.8f, 0.5f);
        [SerializeField] private Color _borderColor = new Color(0.4f, 0.3f, 0.2f, 0.7f);

        [Header("Grid Lines")]
        [SerializeField] private Color _gridLineColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color _selectedTileColor = new Color(1f, 1f, 0f, 0.8f);

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float TileWidth => _tileWidth;
        public float TileHeight => _tileHeight;
        public Color GridLineColor => _gridLineColor;
        public Color SelectedTileColor => _selectedTileColor;

        /// <summary>
        /// Get the editor color for a tile type.
        /// </summary>
        public Color GetTileColor(TileType type)
        {
            return type switch
            {
                TileType.Empty => _emptyColor,
                TileType.Road => _roadColor,
                TileType.Park => _parkColor,
                TileType.Building => _buildingColor,
                TileType.Lot => _lotColor,
                TileType.Water => _waterColor,
                TileType.Special => _specialColor,
                TileType.Border => _borderColor,
                _ => _emptyColor
            };
        }

        /// <summary>
        /// Get the label for a tile type (shown in editor).
        /// </summary>
        public string GetTileLabel(TileType type)
        {
            return type switch
            {
                TileType.Empty => "---",
                TileType.Road => "ROAD",
                TileType.Park => "PARK",
                TileType.Building => "BLDG",
                TileType.Lot => "LOT",
                TileType.Water => "WATER",
                TileType.Special => "SPEC",
                TileType.Border => "BORDER",
                _ => "???"
            };
        }
    }
}
