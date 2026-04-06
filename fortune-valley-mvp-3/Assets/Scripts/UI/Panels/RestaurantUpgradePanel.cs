using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Panel that opens when the player clicks the restaurant building.
    /// Shows current tier info and lets the player upgrade.
    ///
    /// LEARNING DESIGN: The upgrade cost creates an explicit opportunity
    /// cost choice. Cash spent here cannot be reinvested. Students should
    /// ask: "Is $500 now worth more as an upgrade or as investment capital?"
    /// </summary>
    public class RestaurantUpgradePanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR FIELDS
        // ═══════════════════════════════════════════════════════════════

        [Header("Systems (read-only access)")]
        [Tooltip("Restaurant system -- provides tier info (read-only)")]
        [SerializeField] private RestaurantSystem _restaurantSystem;

        [Tooltip("Currency manager -- used to check affordability (read-only)")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Current Tier Display")]
        [Tooltip("Shows the current tier name, e.g. 'Bistro'")]
        [SerializeField] private TextMeshProUGUI _tierNameText;

        [Tooltip("Shows income per tick, e.g. 'Income: $25/tick'")]
        [SerializeField] private TextMeshProUGUI _incomeText;

        [Tooltip("Tier image -- swap sprite per tier to give visual feedback")]
        [SerializeField] private Image _tierImage;

        [Tooltip("One sprite per tier (index 0 = Tier 1). Placeholder colored squares work fine.")]
        [SerializeField] private Sprite[] _tierSprites;

        [Header("Upgrade Section (hidden at max tier)")]
        [Tooltip("Container for all upgrade UI -- hidden when fully upgraded")]
        [SerializeField] private GameObject _upgradeGroup;

        [Tooltip("Cost and destination tier text, e.g. 'Upgrade to Fortune Grill: $1,500'")]
        [SerializeField] private TextMeshProUGUI _upgradeCostText;

        [Tooltip("Payback explanation from RestaurantConfig")]
        [SerializeField] private TextMeshProUGUI _upgradeDetailText;

        [Tooltip("Shows 'Need $X more' when the player cannot afford the upgrade")]
        [SerializeField] private TextMeshProUGUI _affordabilityText;

        [Tooltip("Triggers the upgrade -- grayed out when player cannot afford")]
        [SerializeField] private Button _upgradeButton;

        [Header("Max Tier Section (shown when fully upgraded)")]
        [Tooltip("Container shown when restaurant is at max tier")]
        [SerializeField] private GameObject _maxTierGroup;

        [Tooltip("Message shown when fully upgraded")]
        [SerializeField] private TextMeshProUGUI _maxTierText;

        [Header("Navigation")]
        [Tooltip("Closes the panel")]
        [SerializeField] private Button _closeButton;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Wire buttons
            if (_upgradeButton != null)
                _upgradeButton.onClick.AddListener(OnUpgradeClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnRestaurantSelected += HandleRestaurantSelected;
            GameEvents.OnRestaurantUpgraded += OnUpgraded;
            GameEvents.OnCurrencyChanged += OnCurrencyChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnRestaurantSelected -= HandleRestaurantSelected;
            GameEvents.OnRestaurantUpgraded -= OnUpgraded;
            GameEvents.OnCurrencyChanged -= OnCurrencyChanged;
        }

        // ═══════════════════════════════════════════════════════════════
        // UIPANEL OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            Refresh();
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Player clicked the restaurant building in the world.
        /// </summary>
        private void HandleRestaurantSelected()
        {
            Show(); // inherited from UIPanel
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Rebuilds all text and button states from current RestaurantSystem state.
        /// </summary>
        private void Refresh()
        {
            if (_restaurantSystem == null) return;

            // Current tier
            if (_tierNameText != null)
                _tierNameText.text = _restaurantSystem.TierDisplayName;

            if (_incomeText != null)
                _incomeText.text = $"Income: ${_restaurantSystem.IncomePerTick:F0}/tick";

            // Tier image (tier index is level - 1)
            if (_tierImage != null && _tierSprites != null && _tierSprites.Length > 0)
            {
                int spriteIndex = Mathf.Clamp(_restaurantSystem.CurrentLevel - 1, 0, _tierSprites.Length - 1);
                if (_tierSprites[spriteIndex] != null)
                    _tierImage.sprite = _tierSprites[spriteIndex];
            }

            bool isMax = _restaurantSystem.IsMaxTier;

            // Toggle between upgrade section and max tier message
            if (_upgradeGroup != null)
                _upgradeGroup.SetActive(!isMax);

            if (_maxTierGroup != null)
                _maxTierGroup.SetActive(isMax);

            if (isMax)
            {
                // Show a clear summary at max tier
                if (_maxTierText != null)
                {
                    _maxTierText.text = $"{_restaurantSystem.TierDisplayName} is fully upgraded!\n" +
                                        $"You're earning ${_restaurantSystem.IncomePerTick:F0} every tick.";
                }
                return;
            }

            // Populate upgrade section
            if (_upgradeCostText != null)
                _upgradeCostText.text = _restaurantSystem.GetUpgradeExplanation()
                    .Split('\n')[0]; // First line: "Upgrade to X: $Y"

            if (_upgradeDetailText != null)
                _upgradeDetailText.text = _restaurantSystem.GetUpgradeExplanation();

            RefreshAffordability();
        }

        /// <summary>
        /// Updates the upgrade button and affordability hint based on current balance.
        /// Called by Refresh and whenever currency changes while the panel is open.
        /// </summary>
        private void RefreshAffordability()
        {
            if (_restaurantSystem == null || _currencyManager == null) return;
            if (_restaurantSystem.IsMaxTier) return;

            float cost = _restaurantSystem.UpgradeCost;
            bool canAfford = _currencyManager.CanAfford(cost);

            if (_upgradeButton != null)
                _upgradeButton.interactable = canAfford;

            if (_affordabilityText != null)
            {
                if (canAfford)
                {
                    _affordabilityText.gameObject.SetActive(false);
                }
                else
                {
                    float shortfall = cost - _currencyManager.Balance;
                    _affordabilityText.text = $"Need ${shortfall:F0} more";
                    _affordabilityText.gameObject.SetActive(true);
                }
            }
        }

        private void OnUpgradeClicked()
        {
            // Fire intent event -- RestaurantSystem handles affordability and upgrade
            GameEvents.RaiseUpgradeRestaurantRequested();
        }

        private void OnUpgraded(int newLevel)
        {
            // Event fired by RestaurantSystem after a successful upgrade
            Refresh();
        }

        private void OnCurrencyChanged(float newBalance, float delta)
        {
            // Only bother updating if the panel is visible
            if (IsVisible)
                RefreshAffordability();
        }
    }
}
