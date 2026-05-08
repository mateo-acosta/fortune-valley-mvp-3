using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.City
{
    /// <summary>
    /// Output of NeighborBuildingPicker.Pick. Buildings[i] and Rotations[i] are paired:
    /// the building at slot i should be instantiated with the matching rotation.
    /// A null entry in Buildings means the catalog had no candidate for that size bucket.
    /// </summary>
    public readonly struct NeighborBuildingPickResult
    {
        public NeighborBuildingSO[] Buildings { get; }
        public Quaternion[] Rotations { get; }

        public NeighborBuildingPickResult(NeighborBuildingSO[] buildings, Quaternion[] rotations)
        {
            Buildings = buildings;
            Rotations = rotations;
        }
    }
}
