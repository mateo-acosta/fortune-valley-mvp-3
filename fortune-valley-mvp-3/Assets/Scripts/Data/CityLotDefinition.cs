using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines a single lot in the city that can be purchased.
    /// Create 5 of these for the POC.
    ///
    /// LEARNING DESIGN: Lots represent the goal. Students must balance
    /// "buy this lot now" vs "invest money to afford a better lot later."
    /// </summary>
    [CreateAssetMenu(fileName = "NewLot", menuName = "Fortune Valley/City Lot")]
    public class CityLotDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // IDENTITY
        // ═══════════════════════════════════════════════════════════════

        [Header("Identity")]
        [Tooltip("Unique identifier for this lot")]
        [SerializeField] private string _lotId;

        [Tooltip("Display name (e.g., 'Downtown Corner')")]
        [SerializeField] private string _displayName;

        [Tooltip("Description for flavor")]
        [TextArea(1, 2)]
        [SerializeField] private string _description;

        // ═══════════════════════════════════════════════════════════════
        // ECONOMICS
        // ═══════════════════════════════════════════════════════════════

        [Header("Economics")]
        [Tooltip("Base cost to purchase this lot")]
        [SerializeField] private float _baseCost = 1000f;

        [Tooltip("Bonus income per tick if player owns this lot (passive benefit)")]
        [SerializeField] private float _incomeBonus = 5f;

        [Tooltip("Cost to upgrade from Tier 1 to Tier 2")]
        [SerializeField] private float _tier2UpgradeCost = 500f;

        [Tooltip("Cost to upgrade from Tier 2 to Tier 3")]
        [SerializeField] private float _tier3UpgradeCost = 1500f;

        [Tooltip("Multiplier applied to BaseCost when the player buys this lot out from the rival")]
        [SerializeField] private float _rivalBuyoutMultiplier = 3f;

        // ═══════════════════════════════════════════════════════════════
        // VISUAL PLACEMENT (for Editor wiring)
        // ═══════════════════════════════════════════════════════════════

        [Header("Visual Placement")]
        [Tooltip("Grid position for 2.5D layout")]
        [SerializeField] private Vector2Int _gridPosition;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string LotId => _lotId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float BaseCost => _baseCost;
        public float IncomeBonus => _incomeBonus;

        private const float TierOneIncomeMultiplier = 0.5f;
        private const float TierTwoIncomeMultiplier = 1f;
        private const float TierThreeIncomeMultiplier = 2f;

        // Per-tick income scaled by tier. _incomeBonus is treated as the T2 baseline.
        public float GetIncomeAtTier(int tier)
        {
            if (tier == 1) return _incomeBonus * TierOneIncomeMultiplier;
            if (tier == 2) return _incomeBonus * TierTwoIncomeMultiplier;
            if (tier == 3) return _incomeBonus * TierThreeIncomeMultiplier;
            return 0f;
        }
        public float Tier2UpgradeCost => _tier2UpgradeCost;
        public float Tier3UpgradeCost => _tier3UpgradeCost;
        public float RivalBuyoutMultiplier => _rivalBuyoutMultiplier;
        public Vector2Int GridPosition => _gridPosition;

        // ═══════════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════════

        private void OnValidate()
        {
            // Auto-generate ID from name if empty
            if (string.IsNullOrEmpty(_lotId) && !string.IsNullOrEmpty(name))
            {
                _lotId = name.Replace(" ", "_").ToLower();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get explanation text for students about this lot.
        /// </summary>
        public string GetPurchaseExplanation(float playerBalance)
        {
            bool canAfford = playerBalance >= _baseCost;
            string affordText = canAfford
                ? "You can afford this!"
                : $"You need ${_baseCost - playerBalance:F0} more.";

            // Calculate ROI from income bonus
            int daysToPayback = _incomeBonus > 0
                ? Mathf.CeilToInt(_baseCost / _incomeBonus)
                : 0;

            string bonusText = _incomeBonus > 0
                ? $"Owning this gives you ${_incomeBonus:F0} extra per day.\n" +
                  $"It will pay for itself in ~{daysToPayback} days."
                : "This lot has no income bonus.";

            return $"{_displayName} - ${_baseCost:F0}\n{affordText}\n{bonusText}";
        }
    }
}
