using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// One cosmetic neighbor building option that can fill a CosmeticSlot on a block.
    /// Purely visual. Sized into Small/Medium/Large so the picker can stratify a block's trio.
    /// </summary>
    [CreateAssetMenu(fileName = "NeighborBuilding", menuName = "Fortune Valley/Neighbor Building")]
    public class NeighborBuildingSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private NeighborBuildingSize _size;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;
        public NeighborBuildingSize Size => _size;
    }
}
