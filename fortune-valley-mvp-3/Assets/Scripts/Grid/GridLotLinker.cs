using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Grid
{
    /// <summary>
    /// Links CityLotDefinitions to grid tiles.
    /// Validates that lots are placed on LOT-type tiles.
    /// </summary>
    public static class GridLotLinker
    {
        /// <summary>
        /// Get all lots that match their grid positions on the map.
        /// </summary>
        public static List<CityLotDefinition> GetValidLots(GridMapData mapData, List<CityLotDefinition> allLots)
        {
            var validLots = new List<CityLotDefinition>();

            foreach (var lot in allLots)
            {
                if (IsLotValidOnGrid(mapData, lot))
                {
                    validLots.Add(lot);
                }
            }

            return validLots;
        }

        /// <summary>
        /// Check if a lot is placed on a valid LOT tile.
        /// </summary>
        public static bool IsLotValidOnGrid(GridMapData mapData, CityLotDefinition lot)
        {
            if (mapData == null || lot == null)
            {
                return false;
            }

            Vector2Int pos = lot.GridPosition;

            if (!mapData.IsValidPosition(pos.x, pos.y))
            {
                return false;
            }

            TileType tileType = mapData.GetTileType(pos);
            return tileType == TileType.Lot;
        }

        /// <summary>
        /// Get lots at positions that don't match LOT tiles.
        /// Useful for editor validation.
        /// </summary>
        public static List<CityLotDefinition> GetMismatchedLots(GridMapData mapData, List<CityLotDefinition> allLots)
        {
            var mismatched = new List<CityLotDefinition>();

            foreach (var lot in allLots)
            {
                if (!IsLotValidOnGrid(mapData, lot))
                {
                    mismatched.Add(lot);
                }
            }

            return mismatched;
        }

        /// <summary>
        /// Get LOT tiles that have no CityLotDefinition assigned.
        /// </summary>
        public static List<Vector2Int> GetUnassignedLotTiles(GridMapData mapData, List<CityLotDefinition> allLots)
        {
            var unassigned = new List<Vector2Int>();

            // Build set of used positions
            var usedPositions = new HashSet<Vector2Int>();
            foreach (var lot in allLots)
            {
                usedPositions.Add(lot.GridPosition);
            }

            // Find LOT tiles without definitions
            for (int y = 0; y < mapData.Height; y++)
            {
                for (int x = 0; x < mapData.Width; x++)
                {
                    if (mapData.GetTileType(x, y) == TileType.Lot)
                    {
                        var pos = new Vector2Int(x, y);
                        if (!usedPositions.Contains(pos))
                        {
                            unassigned.Add(pos);
                        }
                    }
                }
            }

            return unassigned;
        }

        /// <summary>
        /// Get a summary of lot/grid alignment for debugging.
        /// </summary>
        public static string GetAlignmentSummary(GridMapData mapData, List<CityLotDefinition> allLots)
        {
            int totalLotTiles = mapData.CountTilesOfType(TileType.Lot);
            int definedLots = allLots.Count;
            int validLots = GetValidLots(mapData, allLots).Count;
            int mismatchedLots = definedLots - validLots;
            int unassignedTiles = GetUnassignedLotTiles(mapData, allLots).Count;

            return $"Grid Lot Summary:\n" +
                   $"• LOT tiles on grid: {totalLotTiles}\n" +
                   $"• CityLotDefinitions: {definedLots}\n" +
                   $"• Valid placements: {validLots}\n" +
                   $"• Mismatched lots: {mismatchedLots}\n" +
                   $"• Unassigned tiles: {unassignedTiles}";
        }
    }
}
