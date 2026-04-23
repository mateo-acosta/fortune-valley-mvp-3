using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.City
{
    /// <summary>
    /// One block = 1 player-ownable lot plus 3 cosmetic neighbor slots.
    /// Owns the block-level edge glow and (Phase 2) the cosmetic variant picker flow.
    /// Attach to an empty Block_* parent in the scene; wire the owned lot's
    /// CityLotDefinition, the BlockEdgeGlow child, and 3 cosmetic slot Transforms.
    /// </summary>
    public class BlockController : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("The single player-ownable lot on this block. Purchasing or upgrading this lot drives block state.")]
        [SerializeField] private CityLotDefinition _ownedLot;

        /// <summary>Lot definition this block owns. Null on ambient (non-interactive) blocks.</summary>
        public CityLotDefinition OwnedLot => _ownedLot;

        [Header("Glow")]
        [SerializeField] private BlockEdgeGlow _edgeGlow;

        [Header("Cosmetic Slots (Phase 2 picker anchors)")]
        [Tooltip("3 anchor Transforms where Phase 2 variant prefabs will spawn. Not used in Phase 1.")]
        [SerializeField] private Transform[] _cosmeticSlots = new Transform[3];

        [Header("Neighbor Buildings (Phase 1 visibility swap)")]
        [Tooltip("3 pre-placed cosmetic neighbor buildings on this block. Revealed one per tier as the owned lot upgrades.")]
        [SerializeField] private GameObject[] _neighborBuildings = new GameObject[3];

        [Tooltip("3 vacant-lot dirt meshes, one per neighbor slot. Shown when that neighbor's building is still hidden.")]
        [SerializeField] private GameObject[] _neighborVacantMeshes = new GameObject[3];

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            ResetNeighborVisibility();
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_ownedLot == null) return;
            if (lotId != _ownedLot.LotId) return;
            if (_edgeGlow == null) return;

            _edgeGlow.SetOwnershipColor(owner);
        }

        private void HandleLotTierChanged(string lotId, int newTier)
        {
            if (_ownedLot == null) return;
            if (lotId != _ownedLot.LotId) return;

            // Reveal neighbor[0..newTier-1]. Their matching vacant meshes hide.
            for (int i = 0; i < _neighborBuildings.Length; i++)
            {
                bool revealed = i < newTier;
                if (_neighborBuildings[i] != null) _neighborBuildings[i].SetActive(revealed);
                if (_neighborVacantMeshes[i] != null) _neighborVacantMeshes[i].SetActive(!revealed);
            }
        }

        // Block starts fully vacant: no neighbor buildings visible, all 3 dirt meshes showing.
        private void ResetNeighborVisibility()
        {
            for (int i = 0; i < _neighborBuildings.Length; i++)
            {
                if (_neighborBuildings[i] != null) _neighborBuildings[i].SetActive(false);
                if (_neighborVacantMeshes[i] != null) _neighborVacantMeshes[i].SetActive(true);
            }
        }
    }
}
