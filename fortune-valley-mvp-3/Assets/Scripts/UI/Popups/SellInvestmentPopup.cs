using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for selling (withdrawing) an investment.
    /// Shows current value, gain/loss, and confirms the sale.
    /// </summary>
    public class SellInvestmentPopup : UIPopup
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Investment Info")]
        [SerializeField] private TextMeshProUGUI _investmentNameText;
        [SerializeField] private TextMeshProUGUI _investmentTypeText;

        [Header("Value Display")]
        [SerializeField] private TextMeshProUGUI _principalText;
        [SerializeField] private TextMeshProUGUI _currentValueText;
        [SerializeField] private TextMeshProUGUI _gainLossText;
        [SerializeField] private TextMeshProUGUI _percentReturnText;

        [Header("Time Held")]
        [SerializeField] private TextMeshProUGUI _daysHeldText;
        [SerializeField] private TextMeshProUGUI _compoundsText;

        [Header("Educational")]
        [SerializeField] private TextMeshProUGUI _explanationText;

        [Header("Buttons")]
        [SerializeField] private Button _sellButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _sellButtonText;

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private UIManager _uiManager;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private ActiveInvestment _investment;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_investmentSystem == null) Debug.LogError("[SellInvestmentPopup] _investmentSystem not wired in Inspector.");
            if (_uiManager == null) Debug.LogError("[SellInvestmentPopup] _uiManager not wired in Inspector.");

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (_sellButton != null)
            {
                _sellButton.onClick.AddListener(OnSellClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnInvestmentCompounded += OnInvestmentCompounded;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCompounded -= OnInvestmentCompounded;
        }

        private void OnInvestmentCompounded(ActiveInvestment inv)
        {
            // Update display if our investment just compounded
            if (_investment != null && inv.Id == _investment.Id)
            {
                UpdateDisplay();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show popup for a specific investment.
        /// </summary>
        public void ShowForInvestment(ActiveInvestment investment)
        {
            _investment = investment;
            UpdateDisplay();
            Show();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_investment == null) return;

            // Investment name and type
            if (_investmentNameText != null)
            {
                _investmentNameText.text = _investment.Definition.DisplayName;
            }

            if (_investmentTypeText != null)
            {
                string riskText = _investment.Definition.RiskLevel switch
                {
                    RiskLevel.Low => "Low Risk",
                    RiskLevel.Medium => "Medium Risk",
                    RiskLevel.High => "High Risk",
                    _ => "Unknown"
                };
                _investmentTypeText.text = riskText;
            }

            // Principal
            if (_principalText != null)
            {
                _principalText.text = $"Original Investment: ${_investment.Principal:N2}";
            }

            // Current value
            if (_currentValueText != null)
            {
                _currentValueText.text = $"Current Value: ${_investment.CurrentValue:N2}";
            }

            // Gain/loss
            float gain = _investment.TotalGain;
            bool isGain = gain >= 0;

            if (_gainLossText != null)
            {
                string prefix = isGain ? "+" : "";
                _gainLossText.text = $"Total {(isGain ? "Gain" : "Loss")}: {prefix}${gain:N2}";
                _gainLossText.color = isGain ? _gainColor : _lossColor;
            }

            // Percent return
            if (_percentReturnText != null)
            {
                float percent = _investment.PercentageReturn;
                string prefix = isGain ? "+" : "";
                _percentReturnText.text = $"Return: {prefix}{percent:F1}%";
                _percentReturnText.color = isGain ? _gainColor : _lossColor;
            }

            // Time held
            if (_daysHeldText != null)
            {
                _daysHeldText.text = $"Days Held: {_investment.TicksHeld}";
            }

            if (_compoundsText != null)
            {
                _compoundsText.text = $"Compound Events: {_investment.CompoundCount}";
            }

            // Educational explanation
            if (_explanationText != null)
            {
                _explanationText.text = _investment.GetPerformanceExplanation();
            }

            // Sell button text
            if (_sellButtonText != null)
            {
                _sellButtonText.text = $"Sell for ${_investment.CurrentValue:N2}";
            }
        }

        private void OnSellClicked()
        {
            if (_investment == null || _investmentSystem == null) return;

            float payout = _investmentSystem.WithdrawInvestment(_investment);

            if (payout > 0)
            {
                UnityEngine.Debug.Log($"[SellInvestmentPopup] Sold investment for ${payout:F2}");
                _uiManager.HidePopup(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[SellInvestmentPopup] Failed to sell investment");
            }
        }

        protected override void OnHide()
        {
            base.OnHide();
            _investment = null;
        }
    }
}
