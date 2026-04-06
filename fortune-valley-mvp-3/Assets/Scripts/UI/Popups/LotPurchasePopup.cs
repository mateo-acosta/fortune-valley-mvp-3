using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for purchasing a city lot.
    /// Shows lot details, cost, income bonus, and ROI information.
    /// </summary>
    public class LotPurchasePopup : UIPopup
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Lot Info")]
        [SerializeField] private TextMeshProUGUI _lotNameText;
        [SerializeField] private TextMeshProUGUI _lotDescriptionText;

        [Header("Economics")]
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _incomeBonusText;
        [SerializeField] private TextMeshProUGUI _roiText;
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _affordabilityText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _buyButtonText;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Colors")]
        [SerializeField] private Color _canAffordColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _cannotAffordColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private CityLotDefinition _currentLot;
        private int _currentTick;
        private bool _initialized = false;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Lazy init: finds dependencies and wires buttons once.
        /// Safe to call before Start() (e.g. from ConfigureForLot on an inactive GameObject).
        /// </summary>
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            if (_currencyManager == null) Debug.LogError("[LotPurchasePopup] _currencyManager not wired in Inspector.");

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnEnable()
        {
            // Update when balance changes while popup is open
            GameEvents.OnCheckingBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckingBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(float balance, float delta)
        {
            if (_currentLot != null)
            {
                UpdateAffordability();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Set lot data on the popup without showing it.
        /// Call this before UIManager.ShowPopup() to configure what the popup displays.
        /// </summary>
        public void ConfigureForLot(CityLotDefinition lot, int currentTick = 0)
        {
            EnsureInitialized();
            _currentLot = lot;
            _currentTick = currentTick;
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdateDisplay();

            // Center on screen (anchors are already 0.5, 0.5)
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_currentLot == null) return;

            // Lot info
            if (_lotNameText != null)
            {
                _lotNameText.text = _currentLot.DisplayName;
            }

            if (_lotDescriptionText != null)
            {
                _lotDescriptionText.text = _currentLot.Description;
            }

            // Cost
            if (_costText != null)
            {
                _costText.text = $"Cost: ${_currentLot.BaseCost:N0}";
            }

            // Income bonus
            if (_incomeBonusText != null)
            {
                if (_currentLot.IncomeBonus > 0)
                {
                    _incomeBonusText.text = $"Income: +${_currentLot.IncomeBonus:N0}/day";
                }
                else
                {
                    _incomeBonusText.text = "No income bonus";
                }
            }

            // ROI calculation
            if (_roiText != null)
            {
                if (_currentLot.IncomeBonus > 0)
                {
                    int daysToPayback = Mathf.CeilToInt(_currentLot.BaseCost / _currentLot.IncomeBonus);
                    _roiText.text = $"Payback: ~{daysToPayback} days";
                }
                else
                {
                    _roiText.text = "";
                }
            }

            UpdateAffordability();
        }

        private void UpdateAffordability()
        {
            if (_currentLot == null || _currencyManager == null) return;

            float checkingBalance = _currencyManager.CheckingBalance;
            bool canAfford = checkingBalance >= _currentLot.BaseCost;

            // Balance display
            if (_balanceText != null)
            {
                _balanceText.text = $"Your Checking: ${checkingBalance:N0}";
            }

            // Affordability message
            if (_affordabilityText != null)
            {
                if (canAfford)
                {
                    _affordabilityText.text = "You can afford this!";
                    _affordabilityText.color = _canAffordColor;
                }
                else
                {
                    float needed = _currentLot.BaseCost - checkingBalance;
                    _affordabilityText.text = $"Need ${needed:N0} more";
                    _affordabilityText.color = _cannotAffordColor;
                }
            }

            // Buy button state
            if (_buyButton != null)
            {
                _buyButton.interactable = canAfford;
            }

            if (_buyButtonText != null)
            {
                _buyButtonText.text = canAfford ? "Buy" : "Can't Afford";
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnBuyClicked()
        {
            if (_currentLot == null || _currencyManager == null) return;

            // Pre-validate affordability before firing intent event (property reads only)
            if (_currencyManager.Balance < _currentLot.BaseCost)
            {
                UpdateAffordability();
                return;
            }

            GameEvents.RaisePurchaseLotRequested(_currentLot.LotId, _currentTick);
            OnCancelClicked(); // inherited from UIPopup -- fires OnCloseRequested
        }

        protected override void OnHide()
        {
            base.OnHide();
            _currentLot = null;
        }
    }
}
