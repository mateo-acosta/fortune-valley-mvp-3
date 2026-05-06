using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// AI competitor that tries to buy city lots.
    ///
    /// LEARNING DESIGN: The rival creates urgency that forces trade-offs.
    /// Without time pressure, students could wait forever and never need
    /// to make real financial decisions. The rival makes opportunity cost
    /// tangible: "If I invest now, I might lose that lot to the rival."
    ///
    /// The rival is intentionally simple and predictable so students can
    /// reason about outcomes.
    /// </summary>
    public class RivalAI : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [SerializeField] private RivalConfig _config;
        [SerializeField] private CityManager _cityManager;

        [Header("Building Reference")]
        [SerializeField] private Transform _rivalBuilding;
        [SerializeField] private float _spawnHeightOffset = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _logBehavior = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _money;
        private int _lastPurchaseTick = 0;
        private string _targetedLotId = null;
        private int _warningIssuedTick = -1;
        private Renderer _buildingRenderer;

        // Cached available lots -- refreshed on ownership change, not per tick
        private List<CityLotDefinition> _cachedAvailableLots = new List<CityLotDefinition>();

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Rival's current money (for debug/UI).
        /// </summary>
        public float Money => _money;

        /// <summary>
        /// Lot the rival is currently targeting (for warning UI).
        /// </summary>
        public string TargetedLotId => _targetedLotId;

        /// <summary>
        /// Ticks until rival attempts next purchase.
        /// </summary>
        public int TicksUntilPurchase { get; private set; }

        /// <summary>
        /// Rival's cumulative per-tick income: base config rate + tier-scaled lot bonuses.
        /// </summary>
        public float TotalIncomePerTick
        {
            get
            {
                float lotBonus = _cityManager != null ? _cityManager.RivalLotIncomeBonus : 0f;
                float baseRate = _config != null ? _config.IncomePerTick : 0f;
                return baseRate + lotBonus;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_rivalBuilding != null)
            {
                _buildingRenderer = _rivalBuilding.GetComponent<Renderer>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnLotPurchased += HandleLotPurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
        }

        private void HandleGameStart()
        {
            _money = _config.StartingMoney;
            GameEvents.RaiseRivalBalanceChanged(_money);
            _lastPurchaseTick = 0;
            _targetedLotId = null;
            _warningIssuedTick = -1;
            TicksUntilPurchase = _config.PurchaseInterval;
            RefreshAvailableLotsCache();
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            // Refresh cache whenever any lot changes ownership
            RefreshAvailableLotsCache();

            // If the player bought the lot we were targeting, pick a new target
            if (owner == Owner.Player && lotId == _targetedLotId)
            {
                if (_logBehavior)
                {
                    Debug.Log($"[RivalAI] Player bought targeted lot {lotId}, picking new target");
                }

                // Pick a new target immediately
                string newTarget = PickTargetLot();
                _targetedLotId = newTarget;

                if (newTarget != null)
                {
                    // Notify UI of new target with remaining time
                    GameEvents.RaiseRivalTargetChanged(newTarget, TicksUntilPurchase);

                    if (_logBehavior)
                    {
                        Debug.Log($"[RivalAI] New target: {newTarget} in {TicksUntilPurchase} ticks");
                    }
                }
                else
                {
                    // No more lots available - clear the target indicator
                    GameEvents.RaiseRivalTargetChanged(null, 0);

                    if (_logBehavior)
                    {
                        Debug.Log("[RivalAI] No available lots to target");
                    }
                }
            }
        }

        private void HandleTick(int tickNumber)
        {
            // Earn base income + bonus from owned lots
            float lotBonus = _cityManager != null ? _cityManager.RivalLotIncomeBonus : 0f;
            float totalIncome = _config.IncomePerTick + lotBonus;
            _money += totalIncome;
            GameEvents.RaiseRivalBalanceChanged(_money);

            // Show floating income text above rival restaurant
            Vector3 spawnPos = transform.position;
            if (_rivalBuilding != null)
            {
                float rooftopY = _buildingRenderer != null
                    ? _buildingRenderer.bounds.max.y
                    : _rivalBuilding.position.y;
                spawnPos = new Vector3(
                    _rivalBuilding.position.x,
                    rooftopY + _spawnHeightOffset,
                    _rivalBuilding.position.z);
            }
            GameEvents.RaiseRivalIncomeWithPosition(totalIncome, spawnPos);

            // Calculate ticks until next purchase attempt
            int purchaseInterval = GetCurrentPurchaseInterval();
            int ticksSinceLastPurchase = tickNumber - _lastPurchaseTick;
            TicksUntilPurchase = purchaseInterval - ticksSinceLastPurchase;

            // Check if we should issue a warning
            CheckAndIssueWarning(tickNumber, purchaseInterval, ticksSinceLastPurchase);

            // Check if it's time to attempt a purchase
            if (ticksSinceLastPurchase >= purchaseInterval)
            {
                AttemptPurchase(tickNumber);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private int GetCurrentPurchaseInterval()
        {
            float progress = _cityManager.GetGameProgress();
            return _config.GetEffectivePurchaseInterval(progress);
        }

        private void CheckAndIssueWarning(int currentTick, int purchaseInterval, int ticksSinceLastPurchase)
        {
            int ticksRemaining = purchaseInterval - ticksSinceLastPurchase;

            // Issue warning when we're within warning threshold
            if (ticksRemaining <= _config.WarningTicks && ticksRemaining > 0)
            {
                // Only issue warning once per purchase cycle
                if (_warningIssuedTick < _lastPurchaseTick)
                {
                    // Pick the target lot
                    _targetedLotId = PickTargetLot();

                    if (_targetedLotId != null)
                    {
                        _warningIssuedTick = currentTick;
                        GameEvents.RaiseRivalTargetingLot(_targetedLotId);
                        // Raise enhanced event with days until purchase
                        GameEvents.RaiseRivalTargetChanged(_targetedLotId, ticksRemaining);

                        if (_logBehavior)
                        {
                            Debug.Log($"[RivalAI] Warning: Targeting {_targetedLotId} in {ticksRemaining} ticks");
                        }
                    }
                }
            }
        }

        private void AttemptPurchase(int tickNumber)
        {
            _lastPurchaseTick = tickNumber;
            _targetedLotId = null;

            // Soft cap (Life Goals revision): once the rival owns
            // CityManager.MAX_RIVAL_LOTS, skip purchase attempts so the
            // rival does not waste money trying to buy lots CityManager
            // would now refuse.
            if (_cityManager != null && _cityManager.RivalAtSoftCap)
            {
                if (_logBehavior)
                {
                    Debug.Log("[RivalAI] At MAX_RIVAL_LOTS soft cap; skipping purchase.");
                }
                return;
            }

            // Find a lot we can afford
            string lotToBuy = PickAffordableLot();

            if (lotToBuy == null)
            {
                if (_logBehavior)
                {
                    Debug.Log("[RivalAI] No affordable lot found, skipping purchase");
                }
                return;
            }

            var lot = _cityManager.GetLot(lotToBuy);
            float cost = lot.BaseCost;

            // Spend money and purchase
            _money -= cost;
            GameEvents.RaiseRivalBalanceChanged(_money);
            _cityManager.RivalPurchaseLot(lotToBuy, tickNumber);

            // Raise event for UI feedback (overlay, etc.)
            GameEvents.RaiseRivalPurchasedLot(lotToBuy);

            if (_logBehavior)
            {
                Debug.Log($"[RivalAI] Purchased {lot.DisplayName} for ${cost:F0}. Remaining: ${_money:F0}");
            }
        }

        /// <summary>
        /// Refresh the cached available lots list from CityManager.
        /// Called on game start and whenever lot ownership changes.
        /// Pre-sorted by cost so PickTargetLot/PickAffordableLot avoid per-tick allocations.
        /// </summary>
        private void RefreshAvailableLotsCache()
        {
            _cachedAvailableLots = _cityManager.GetAvailableLots();
            _cachedAvailableLots.Sort((a, b) => a.BaseCost.CompareTo(b.BaseCost));
        }

        /// <summary>
        /// Pick which lot to target (for warnings).
        /// Strategy: Target cheapest lot we might be able to afford.
        /// </summary>
        private string PickTargetLot()
        {
            if (_cachedAvailableLots.Count == 0)
                return null;

            // Return cheapest that we might afford by purchase time
            // (rough estimate: current money + income * warning ticks)
            float estimatedMoney = _money + (_config.IncomePerTick * _config.WarningTicks);

            foreach (var lot in _cachedAvailableLots)
            {
                if (lot.BaseCost <= estimatedMoney + _config.PurchaseBuffer)
                {
                    return lot.LotId;
                }
            }

            // If we can't afford any, target the cheapest anyway
            return _cachedAvailableLots[0].LotId;
        }

        /// <summary>
        /// Pick a lot we can actually afford right now.
        /// Strategy: Buy cheapest affordable lot.
        /// </summary>
        private string PickAffordableLot()
        {
            if (_cachedAvailableLots.Count == 0)
                return null;

            // Find cheapest we can afford with buffer
            foreach (var lot in _cachedAvailableLots)
            {
                if (_money >= lot.BaseCost + _config.PurchaseBuffer)
                {
                    return lot.LotId;
                }
            }

            return null;
        }

        /// <summary>
        /// Get rival status for UI.
        /// </summary>
        public string GetRivalStatus()
        {
            string targetInfo = "";
            if (!string.IsNullOrEmpty(_targetedLotId))
            {
                var lot = _cityManager.GetLot(_targetedLotId);
                targetInfo = $"\nTargeting: {lot?.DisplayName ?? _targetedLotId}";
            }

            return $"Rival Status:\n" +
                   $"• Money: ${_money:F0}\n" +
                   $"• Next purchase in: {TicksUntilPurchase} days" +
                   targetInfo;
        }
    }
}
