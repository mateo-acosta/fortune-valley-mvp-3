using System.Collections.Generic;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Handles the single deposit path for income collection. Subscribes to
    /// OnIncomeCollectRequested (now fired by DailyIncomeAccumulator on day-end
    /// and on ownership loss), calls TryCollect on the accumulator, deposits
    /// to CurrencyManager, and raises feedback + save events.
    /// </summary>
    public class IncomeCollectionController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private DailyIncomeAccumulator _pendingIncome;

        private readonly Dictionary<string, Transform> _anchors = new Dictionary<string, Transform>();
        private readonly HashSet<string> _warnedMissingAnchor = new HashSet<string>();

        private void OnEnable()
        {
            GameEvents.OnIncomeCollectRequested += HandleCollectRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnIncomeCollectRequested -= HandleCollectRequested;
        }

        /// <summary>
        /// Registers (or overwrites) the world-space transform used to spawn
        /// the floating "+$X" feedback on collect. Call from the spawning
        /// component (LotWorldCanvas for lots, RestaurantCollectAnchor for
        /// the restaurant) during Awake.
        /// </summary>
        public void RegisterAnchor(string buildingId, Transform anchor)
        {
            if (string.IsNullOrEmpty(buildingId) || anchor == null) return;
            _anchors[buildingId] = anchor;
        }

        public void UnregisterAnchor(string buildingId)
        {
            _anchors.Remove(buildingId);
        }

        private void HandleCollectRequested(string buildingId, CollectReason reason)
        {
            if (_pendingIncome == null || _currencyManager == null) return;
            if (!_pendingIncome.TryCollect(buildingId, out float amount)) return;

            string tag = ResolveTag(buildingId, reason);
            _currencyManager.AddToChecking(amount, tag);

            Vector3 pos = ResolveAnchorPosition(buildingId);
            GameEvents.RaiseIncomeGeneratedWithPosition(amount, pos);
            GameEvents.RaiseIncomeCollected(buildingId, amount);
            GameEvents.RaiseSaveRequested();
        }

        private string ResolveTag(string buildingId, CollectReason reason)
        {
            if (reason == CollectReason.OwnershipLost)
            {
                return $"LostLot:{buildingId}";
            }
            if (buildingId == DailyIncomeAccumulator.RestaurantBuildingId)
            {
                return "Restaurant";
            }
            return $"Lot:{buildingId}";
        }

        private Vector3 ResolveAnchorPosition(string buildingId)
        {
            if (_anchors.TryGetValue(buildingId, out var t) && t != null)
            {
                return t.position;
            }
            if (_warnedMissingAnchor.Add(buildingId))
            {
                Debug.LogWarning($"[IncomeCollectionController] Missing anchor for '{buildingId}'; using Vector3.zero for floating text.");
            }
            return Vector3.zero;
        }
    }
}
