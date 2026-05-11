using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Catalog of cosmetic neighbor buildings, bucketed by size. The block scene seeder
    /// pulls one of each size per block to produce a stratified trio. Authored once per project.
    /// </summary>
    [CreateAssetMenu(fileName = "NeighborBuildingCatalog", menuName = "Fortune Valley/Neighbor Building Catalog")]
    public class NeighborBuildingCatalogSO : ScriptableObject
    {
        [SerializeField] private NeighborBuildingSO[] _smallBuildings;
        [SerializeField] private NeighborBuildingSO[] _mediumBuildings;
        [SerializeField] private NeighborBuildingSO[] _largeBuildings;

        public NeighborBuildingSO GetRandomBySize(NeighborBuildingSize size, System.Random rng)
        {
            var bucket = GetBucket(size);
            if (bucket == null || bucket.Length == 0) return null;
            return bucket[rng.Next(0, bucket.Length)];
        }

        public NeighborBuildingSO[] GetBucket(NeighborBuildingSize size)
        {
            if (size == NeighborBuildingSize.Small) return _smallBuildings;
            if (size == NeighborBuildingSize.Medium) return _mediumBuildings;
            if (size == NeighborBuildingSize.Large) return _largeBuildings;
            return null;
        }

        // Editor-only validation. Logs a clear error if any bucket is empty or contains nulls,
        // so misconfigured catalogs are caught at author time rather than at scene-seed time.
        private void OnValidate()
        {
            ValidateBucket(_smallBuildings, NeighborBuildingSize.Small);
            ValidateBucket(_mediumBuildings, NeighborBuildingSize.Medium);
            ValidateBucket(_largeBuildings, NeighborBuildingSize.Large);
        }

        private void ValidateBucket(NeighborBuildingSO[] bucket, NeighborBuildingSize size)
        {
            if (bucket == null || bucket.Length == 0)
            {
                Debug.LogError($"[NeighborBuildingCatalog] '{name}': {size} bucket is empty. Add at least one NeighborBuildingSO.", this);
                return;
            }
            for (int i = 0; i < bucket.Length; i++)
            {
                if (bucket[i] == null)
                {
                    Debug.LogError($"[NeighborBuildingCatalog] '{name}': {size} bucket has a null entry at index {i}.", this);
                }
            }
        }
    }
}
