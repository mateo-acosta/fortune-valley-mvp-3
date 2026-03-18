using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for the restaurant income system.
    /// Tweak these values to balance the "safe income" baseline.
    ///
    /// LEARNING DESIGN: The restaurant represents opportunity cost -
    /// it's reliable but limited. Students should feel "I could just wait
    /// for restaurant income... but is that the best use of my time?"
    /// </summary>
    [CreateAssetMenu(fileName = "RestaurantConfig", menuName = "Fortune Valley/Restaurant Config")]
    public class RestaurantConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // BASE INCOME
        // ═══════════════════════════════════════════════════════════════

        [Header("Base Income")]
        [Tooltip("Base income generated per tick at level 1")]
        [SerializeField] private float _baseIncomePerTick = 10f;

        // ═══════════════════════════════════════════════════════════════
        // UPGRADE SYSTEM
        // ═══════════════════════════════════════════════════════════════

        [Header("Upgrades")]
        [Tooltip("Maximum upgrade level (3 tiers: Food Cart, Bistro, Fortune Grill)")]
        [SerializeField] private int _maxLevel = 3;

        [Tooltip("Display name for each tier (index 0 = level 1)")]
        [SerializeField] private string[] _tierNames = { "Level 1", "Level 2", "Level 3" };

        [Tooltip("Cost to upgrade to each level (index 0 = cost to reach level 2)")]
        [SerializeField] private float[] _upgradeCosts = { 500f, 1500f };

        [Tooltip("Income multiplier at each level (index 0 = level 1)")]
        [SerializeField] private float[] _incomeMultipliers = { 1f, 2.5f, 5f };

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public float BaseIncomePerTick => _baseIncomePerTick;
        public int MaxLevel => _maxLevel;

        /// <summary>
        /// Get the display name for a given level (e.g., "Bistro" for level 2).
        /// </summary>
        public string GetTierName(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, _tierNames.Length - 1);
            return _tierNames != null && _tierNames.Length > 0 ? _tierNames[index] : $"Level {level}";
        }

        /// <summary>
        /// Get the income per tick for a given level.
        /// </summary>
        public float GetIncomeForLevel(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, _incomeMultipliers.Length - 1);
            return _baseIncomePerTick * _incomeMultipliers[index];
        }

        /// <summary>
        /// Get the cost to upgrade TO a given level.
        /// Returns -1 if already at max level.
        /// </summary>
        public float GetUpgradeCost(int currentLevel)
        {
            if (currentLevel >= _maxLevel)
                return -1f; // Can't upgrade further

            int index = currentLevel - 1; // currentLevel 1 → index 0 → cost to reach level 2
            if (index < 0 || index >= _upgradeCosts.Length)
                return -1f;

            return _upgradeCosts[index];
        }

        /// <summary>
        /// Check if a level can be upgraded.
        /// </summary>
        public bool CanUpgrade(int currentLevel)
        {
            return currentLevel < _maxLevel;
        }

        /// <summary>
        /// Get explanation text for students about upgrading.
        /// </summary>
        public string GetUpgradeExplanation(int currentLevel)
        {
            if (!CanUpgrade(currentLevel))
                return "Your restaurant is at maximum level!";

            float currentIncome = GetIncomeForLevel(currentLevel);
            float nextIncome = GetIncomeForLevel(currentLevel + 1);
            float upgradeCost = GetUpgradeCost(currentLevel);
            float incomeGain = nextIncome - currentIncome;

            // Calculate payback period
            int ticksToPayback = Mathf.CeilToInt(upgradeCost / incomeGain);

            string nextTierName = GetTierName(currentLevel + 1);
            return $"Upgrade to {nextTierName}: ${upgradeCost:F0}\n" +
                   $"Income increase: ${currentIncome:F0} to ${nextIncome:F0} per day\n" +
                   $"Payback period: ~{ticksToPayback} days\n" +
                   $"After payback, you'll earn ${incomeGain:F0} extra every day!";
        }
    }
}
