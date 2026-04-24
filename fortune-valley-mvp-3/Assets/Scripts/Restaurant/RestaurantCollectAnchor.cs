using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Registers the rooftop transform used to spawn the restaurant's
    /// floating "+$X" feedback when the player taps the collect coin.
    /// The BuildingCollectButton above the restaurant is configured with
    /// its _buildingId set to PendingIncomeService.RestaurantBuildingId.
    /// </summary>
    public class RestaurantCollectAnchor : MonoBehaviour
    {
        [SerializeField] private IncomeCollectionController _collectionController;
        [SerializeField] private Transform _spawnAnchor;

        private void Start()
        {
            if (_collectionController == null) return;
            Transform anchor = _spawnAnchor != null ? _spawnAnchor : transform;
            _collectionController.RegisterAnchor(PendingIncomeService.RestaurantBuildingId, anchor);
        }

        private void OnDestroy()
        {
            if (_collectionController == null) return;
            _collectionController.UnregisterAnchor(PendingIncomeService.RestaurantBuildingId);
        }
    }
}
