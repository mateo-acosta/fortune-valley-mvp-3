using UnityEngine;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the player's restaurant income rate and upgrade flow.
    ///
    /// LEARNING DESIGN: The restaurant is the "safe" baseline income.
    /// Students should understand: "I can always rely on my restaurant,
    /// but it won't make me rich fast. Is there a better use for my money?"
    ///
    /// Income no longer accumulates tick-by-tick here. DailyIncomeAccumulator
    /// owns the daily-locked coin cycle; this module only exposes the
    /// per-tick rate, handles upgrades, and tracks TotalEarned by summing
    /// OnIncomeCollected deposits.
    /// </summary>
    public class RestaurantSystem : MonoBehaviour, IRestaurantService
    {
        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [Tooltip("Restaurant configuration (income rates, upgrade costs)")]
        [SerializeField] private RestaurantConfig _config;

        [Tooltip("Reference to currency manager for affordability checks")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Tooltip("Reference to city manager for lot income bonuses")]
        [SerializeField] private CityManager _cityManager;

        [Header("Building Reference")]
        [Tooltip("The restaurant building in the scene (for positioning feedback above the rooftop)")]
        [SerializeField] private Transform _restaurantBuilding;

        [Tooltip("Extra height above rooftop to spawn floating text")]
        [SerializeField] private float _spawnHeightOffset = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool _logIncome = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _currentLevel = 1;
        private float _totalEarned = 0f;
        private Renderer _buildingRenderer;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// Income generated per tick at the current level. Consumed by
        /// DailyIncomeAccumulator.ComputeDayRate to lock a day's payout.
        /// </summary>
        public float IncomePerTick => _config.GetIncomeForLevel(_currentLevel);

        /// <summary>Total money earned from restaurant deposits this game.</summary>
        public float TotalEarned => _totalEarned;

        public bool CanUpgrade => _config.CanUpgrade(_currentLevel);
        public float UpgradeCost => _config.GetUpgradeCost(_currentLevel);
        public string TierDisplayName => _config.GetTierName(_currentLevel);
        public bool IsMaxTier => !CanUpgrade;

        /// <summary>
        /// Total income per tick including lot bonuses. Used by
        /// MonthlyPaymentDayController to compute DTI.
        /// </summary>
        public float TotalIncomePerTick
        {
            get
            {
                float lotBonus = _cityManager != null ? _cityManager.PlayerLotIncomeBonus : 0f;
                return IncomePerTick + lotBonus;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_restaurantBuilding != null)
            {
                _buildingRenderer = _restaurantBuilding.GetComponent<Renderer>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnUpgradeRestaurantRequested += HandleUpgradeRequested;
            GameEvents.OnIncomeCollected += HandleIncomeCollected;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnUpgradeRestaurantRequested -= HandleUpgradeRequested;
            GameEvents.OnIncomeCollected -= HandleIncomeCollected;
        }

        private void HandleGameStart()
        {
            _currentLevel = 1;
            _totalEarned = 0f;
        }

        private void HandleUpgradeRequested()
        {
            TryUpgrade();
        }

        private void HandleIncomeCollected(string buildingId, float amount)
        {
            _totalEarned += amount;
            if (_logIncome)
            {
                Debug.Log($"[RestaurantSystem] Deposit from '{buildingId}': +${amount:F2}. Total earned: ${_totalEarned:F2}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                Debug.Log("[RestaurantSystem] Already at max level.");
                return false;
            }

            float cost = _config.GetUpgradeCost(_currentLevel);

            if (!_currencyManager.CanAffordChecking(cost))
            {
                Debug.Log($"[RestaurantSystem] Cannot afford upgrade. Need ${cost:F0}");
                return false;
            }

            GameEvents.RaiseCreditCardChargeRequested(cost, $"Restaurant upgrade to level {_currentLevel + 1}");

            _currentLevel++;
            GameEvents.RaiseRestaurantUpgraded(_currentLevel);

            if (_logIncome)
            {
                Debug.Log($"[RestaurantSystem] Upgraded to level {_currentLevel}. New income: ${IncomePerTick:F2}/tick");
            }

            return true;
        }

        public string GetUpgradeExplanation()
        {
            return _config.GetUpgradeExplanation(_currentLevel);
        }

        public string GetPerformanceSummary()
        {
            return $"Restaurant Level {_currentLevel}\n" +
                   $"Income: ${IncomePerTick:F0} per day\n" +
                   $"Total earned: ${_totalEarned:F0}";
        }
    }
}
