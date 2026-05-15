using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Attached on RestaurantVisual_N. Activates exactly one of the six tier meshes
    /// (player T1/T2/T3 + rival T1/T2/T3) based on CityManager ownership + tier state.
    /// Subscribes to the same events CityManager raises.
    /// </summary>
    public class RestaurantVisualTierSwapper : MonoBehaviour
    {
        [Header("Lot Binding")]
        [SerializeField] private CityLotDefinition _lot;

        [Header("Player Meshes")]
        [SerializeField] private GameObject _playerT1;
        [SerializeField] private GameObject _playerT2;
        [SerializeField] private GameObject _playerT3;

        [Header("Rival Meshes")]
        [SerializeField] private GameObject _rivalT1;
        [SerializeField] private GameObject _rivalT2;
        [SerializeField] private GameObject _rivalT3;

        [Header("For Sale Sign")]
        [SerializeField] private GameObject _forSaleSign;

        [Header("Vacant Lot Mesh")]
        [Tooltip("Dirt/vacant visual shown when the lot has no owner. Hidden once the lot is bought.")]
        [SerializeField] private GameObject _vacantLotMesh;

        private Owner _owner = Owner.None;
        private int _tier;

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleOwnership;
            GameEvents.OnLotTierChanged += HandleTier;
            GameEvents.OnGameStart += HandleGameStart;
            ApplyVisual();
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleOwnership;
            GameEvents.OnLotTierChanged -= HandleTier;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            if (GameEvents.LastLoadedSaveDto != null) return;
            _owner = Owner.None;
            _tier = 0;
            ApplyVisual();
        }

        private void HandleOwnership(string lotId, Owner owner)
        {
            if (_lot == null || lotId != _lot.LotId) return;
            _owner = owner;
            ApplyVisual();
        }

        private void HandleTier(string lotId, int newTier)
        {
            if (_lot == null || lotId != _lot.LotId) return;
            _tier = newTier;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            // Hide all.
            SetActiveIfNotNull(_playerT1, false);
            SetActiveIfNotNull(_playerT2, false);
            SetActiveIfNotNull(_playerT3, false);
            SetActiveIfNotNull(_rivalT1, false);
            SetActiveIfNotNull(_rivalT2, false);
            SetActiveIfNotNull(_rivalT3, false);

            SetActiveIfNotNull(_forSaleSign, _owner == Owner.None);
            SetActiveIfNotNull(_vacantLotMesh, _owner == Owner.None);

            if (_owner == Owner.None || _tier <= 0) return;

            GameObject target = PickTarget();
            SetActiveIfNotNull(target, true);
        }

        private GameObject PickTarget()
        {
            if (_owner == Owner.Player)
            {
                if (_tier == 1) return _playerT1;
                if (_tier == 2) return _playerT2;
                return _playerT3;
            }
            if (_owner == Owner.Rival)
            {
                if (_tier == 1) return _rivalT1;
                if (_tier == 2) return _rivalT2;
                return _rivalT3;
            }
            return null;
        }

        private static void SetActiveIfNotNull(GameObject go, bool value)
        {
            if (go != null && go.activeSelf != value) go.SetActive(value);
        }
    }
}
