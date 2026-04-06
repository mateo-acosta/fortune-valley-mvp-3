using UnityEngine;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the player's restaurant income generation.
    ///
    /// LEARNING DESIGN: The restaurant is the "safe" baseline income.
    /// Students should understand: "I can always rely on my restaurant,
    /// but it won't make me rich fast. Is there a better use for my money?"
    ///
    /// This creates the foundation for understanding opportunity cost.
    /// </summary>
    public class RestaurantSystem : MonoBehaviour, IRestaurantService
    {
        // ═══════════════════════════════════════════════════════════════
        // DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [Tooltip("Restaurant configuration (income rates, upgrade costs)")]
        [SerializeField] private RestaurantConfig _config;

        [Tooltip("Reference to currency manager for income deposits")]
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

        /// <summary>
        /// Current restaurant upgrade level.
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// Income generated per tick at current level.
        /// </summary>
        public float IncomePerTick => _config.GetIncomeForLevel(_currentLevel);

        /// <summary>
        /// Total money earned from restaurant this game.
        /// </summary>
        public float TotalEarned => _totalEarned;

        /// <summary>
        /// Whether the restaurant can be upgraded.
        /// </summary>
        public bool CanUpgrade => _config.CanUpgrade(_currentLevel);

        /// <summary>
        /// Cost to upgrade to the next level, or -1 if max level.
        /// </summary>
        public float UpgradeCost => _config.GetUpgradeCost(_currentLevel);

        /// <summary>
        /// Display name for the current tier (e.g., "Bistro").
        /// </summary>
        public string TierDisplayName => _config.GetTierName(_currentLevel);

        /// <summary>
        /// True when the restaurant cannot be upgraded further.
        /// </summary>
        public bool IsMaxTier => !CanUpgrade;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Cache renderer for rooftop height calculation
            if (_restaurantBuilding != null)
            {
                _buildingRenderer = _restaurantBuilding.GetComponent<Renderer>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnUpgradeRestaurantRequested += HandleUpgradeRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnUpgradeRestaurantRequested -= HandleUpgradeRequested;
        }

        private void HandleGameStart()
        {
            _currentLevel = 1;
            _totalEarned = 0f;
        }

        private void HandleTick(int tickNumber)
        {
            GenerateIncome();
        }

        /// <summary>
        /// Intent event handler: UI requested a restaurant upgrade.
        /// </summary>
        private void HandleUpgradeRequested()
        {
            TryUpgrade();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Attempt to upgrade the restaurant.
        /// Returns true if upgrade was successful.
        /// </summary>
        public bool TryUpgrade()
        {
            if (!CanUpgrade)
            {
                Debug.Log("[RestaurantSystem] Already at max level.");
                return false;
            }

            float cost = _config.GetUpgradeCost(_currentLevel);

            // Route upgrade cost through credit card charge event
            // Phase 0: placeholder in CurrencyManager deducts from checking
            // Phase 1: CreditCardSystem will handle this
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
                Debug.Log($"[RestaurantSystem] Upgraded to level {_currentLevel}. " +
                         $"New income: ${IncomePerTick:F2}/tick");
            }

            return true;
        }

        /// <summary>
        /// Get student-friendly explanation of upgrade value.
        /// </summary>
        public string GetUpgradeExplanation()
        {
            return _config.GetUpgradeExplanation(_currentLevel);
        }

        /// <summary>
        /// Get summary of restaurant performance for UI.
        /// </summary>
        public string GetPerformanceSummary()
        {
            return $"Restaurant Level {_currentLevel}\n" +
                   $"Income: ${IncomePerTick:F0} per day\n" +
                   $"Total earned: ${_totalEarned:F0}";
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void GenerateIncome()
        {
            // Combine base restaurant income with lot ownership bonuses
            float baseIncome = IncomePerTick;
            float lotBonus = _cityManager != null ? _cityManager.PlayerLotIncomeBonus : 0f;
            float income = baseIncome + lotBonus;

            _totalEarned += income;
            _currencyManager.AddToChecking(income, "Restaurant");

            // Compute spawn position above the restaurant rooftop
            Vector3 spawnPos = transform.position; // fallback to GameManager origin
            if (_restaurantBuilding != null)
            {
                float rooftopY = _buildingRenderer != null
                    ? _buildingRenderer.bounds.max.y
                    : _restaurantBuilding.position.y;
                spawnPos = new Vector3(
                    _restaurantBuilding.position.x,
                    rooftopY + _spawnHeightOffset,
                    _restaurantBuilding.position.z);
            }

            GameEvents.RaiseIncomeGeneratedWithPosition(income, spawnPos);

            if (_logIncome)
            {
                Debug.Log($"[RestaurantSystem] Generated ${income:F2} (base: ${baseIncome:F2} + lots: ${lotBonus:F2}). Total: ${_totalEarned:F2}");
            }
        }
    }
}
