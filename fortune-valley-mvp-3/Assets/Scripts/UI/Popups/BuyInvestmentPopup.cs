using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Popup for buying a new investment.
    /// Shows available investment types and allows amount selection.
    /// </summary>
    public class BuyInvestmentPopup : UIPopup
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Investment Selection")]
        [SerializeField] private TMP_Dropdown _investmentDropdown;
        [SerializeField] private TextMeshProUGUI _investmentNameText;
        [SerializeField] private TextMeshProUGUI _investmentDescriptionText;

        [Header("Risk & Return Info")]
        [SerializeField] private TextMeshProUGUI _riskText;
        [SerializeField] private TextMeshProUGUI _returnText;
        [SerializeField] private TextMeshProUGUI _minimumText;
        [SerializeField] private Image _riskIndicator;

        [Header("Amount Input")]
        [SerializeField] private TMP_InputField _amountInput;
        [SerializeField] private Slider _amountSlider;
        [SerializeField] private Button _maxButton;

        [Header("Balance Display")]
        [SerializeField] private TextMeshProUGUI _investingBalanceText;
        [SerializeField] private TextMeshProUGUI _projectionText;

        [Header("Buttons")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _buyButtonText;

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private UIManager _uiManager;

        [Header("Colors")]
        [SerializeField] private Color _lowRiskColor = new Color(0.2f, 0.7f, 0.2f);
        [SerializeField] private Color _mediumRiskColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _highRiskColor = new Color(0.9f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private InvestmentDefinition _selectedInvestment;
        private float _investAmount;
        private List<InvestmentDefinition> _availableInvestments;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            FindDependencies();
            SetupControls();
        }

        private void FindDependencies()
        {
            if (_investmentSystem == null) Debug.LogError("[BuyInvestmentPopup] _investmentSystem not wired in Inspector.");
            if (_currencyManager == null) Debug.LogError("[BuyInvestmentPopup] _currencyManager not wired in Inspector.");
            if (_uiManager == null) Debug.LogError("[BuyInvestmentPopup] _uiManager not wired in Inspector.");
        }

        private void SetupControls()
        {
            if (_investmentDropdown != null)
            {
                _investmentDropdown.onValueChanged.AddListener(OnInvestmentSelected);
            }

            if (_amountInput != null)
            {
                _amountInput.onValueChanged.AddListener(OnAmountInputChanged);
                _amountInput.contentType = TMP_InputField.ContentType.DecimalNumber;
            }

            if (_amountSlider != null)
            {
                _amountSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (_maxButton != null)
            {
                _maxButton.onClick.AddListener(OnMaxClicked);
            }

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
            GameEvents.OnInvestingBalanceChanged += OnBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestingBalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(float balance, float delta)
        {
            UpdateBalanceDisplay();
            UpdateBuyButton();
        }

        // ═══════════════════════════════════════════════════════════════
        // POPUP OVERRIDES
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            PopulateInvestmentDropdown();
            ResetInputs();
            UpdateDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show popup pre-selected to a specific investment type.
        /// </summary>
        public void ShowForInvestment(InvestmentDefinition investment)
        {
            Show();

            // Find and select the investment in dropdown
            if (_availableInvestments != null && investment != null)
            {
                int index = _availableInvestments.IndexOf(investment);
                if (index >= 0 && _investmentDropdown != null)
                {
                    _investmentDropdown.value = index;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void PopulateInvestmentDropdown()
        {
            if (_investmentDropdown == null || _investmentSystem == null) return;

            _investmentDropdown.ClearOptions();
            _availableInvestments = new List<InvestmentDefinition>(_investmentSystem.AvailableInvestments);

            var options = new List<string>();
            foreach (var inv in _availableInvestments)
            {
                options.Add(inv.DisplayName);
            }

            _investmentDropdown.AddOptions(options);

            // Select first investment
            if (_availableInvestments.Count > 0)
            {
                _selectedInvestment = _availableInvestments[0];
            }
        }

        private void ResetInputs()
        {
            _investAmount = 0;

            if (_amountInput != null)
            {
                _amountInput.text = "";
            }

            if (_amountSlider != null)
            {
                _amountSlider.value = 0;
            }
        }

        private void UpdateDisplay()
        {
            UpdateInvestmentInfo();
            UpdateBalanceDisplay();
            UpdateBuyButton();
        }

        private void UpdateInvestmentInfo()
        {
            if (_selectedInvestment == null) return;

            // Name and description
            if (_investmentNameText != null)
            {
                _investmentNameText.text = _selectedInvestment.DisplayName;
            }

            if (_investmentDescriptionText != null)
            {
                _investmentDescriptionText.text = _selectedInvestment.Description;
            }

            // Risk level
            string riskText;
            Color riskColor;

            switch (_selectedInvestment.RiskLevel)
            {
                case RiskLevel.Low:
                    riskText = "Low Risk - Stable returns";
                    riskColor = _lowRiskColor;
                    break;
                case RiskLevel.Medium:
                    riskText = "Medium Risk - Moderate volatility";
                    riskColor = _mediumRiskColor;
                    break;
                case RiskLevel.High:
                    riskText = "High Risk - Can gain or lose significantly";
                    riskColor = _highRiskColor;
                    break;
                default:
                    riskText = "Unknown Risk";
                    riskColor = Color.gray;
                    break;
            }

            if (_riskText != null)
            {
                _riskText.text = riskText;
                _riskText.color = riskColor;
            }

            if (_riskIndicator != null)
            {
                _riskIndicator.color = riskColor;
            }

            // Expected return
            if (_returnText != null)
            {
                float annualReturn = _selectedInvestment.AnnualReturnRate * 100f;
                _returnText.text = $"Expected Return: ~{annualReturn:F1}% per year";
            }

            // Minimum deposit
            if (_minimumText != null)
            {
                _minimumText.text = $"Minimum: ${_selectedInvestment.MinimumDeposit:N0}";
            }

            UpdateProjection();
        }

        private void UpdateBalanceDisplay()
        {
            if (_investingBalanceText != null && _currencyManager != null)
            {
                _investingBalanceText.text = $"Available: ${_currencyManager.InvestingBalance:N2}";
            }
        }

        private void UpdateProjection()
        {
            if (_projectionText == null || _selectedInvestment == null) return;

            if (_investAmount <= 0)
            {
                _projectionText.text = "Enter an amount to see projection";
                return;
            }

            // Project 30 days (rough month)
            int projectionTicks = 30;
            float projectedValue = _selectedInvestment.ProjectValue(_investAmount, projectionTicks);
            float projectedGain = projectedValue - _investAmount;

            _projectionText.text = $"After ~30 days: ${projectedValue:N2} (+${projectedGain:N2})";
        }

        private void UpdateBuyButton()
        {
            if (_buyButton == null || _selectedInvestment == null || _currencyManager == null) return;

            float balance = _currencyManager.InvestingBalance;
            bool canAfford = balance >= _investAmount;
            bool meetsMinimum = _investAmount >= _selectedInvestment.MinimumDeposit;
            bool isValid = _investAmount > 0 && canAfford && meetsMinimum;

            _buyButton.interactable = isValid;

            if (_buyButtonText != null)
            {
                if (_investAmount <= 0)
                {
                    _buyButtonText.text = "Enter Amount";
                }
                else if (!meetsMinimum)
                {
                    _buyButtonText.text = $"Min ${_selectedInvestment.MinimumDeposit:N0}";
                }
                else if (!canAfford)
                {
                    _buyButtonText.text = "Insufficient Funds";
                }
                else
                {
                    _buyButtonText.text = "Buy";
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // CONTROL CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnInvestmentSelected(int index)
        {
            if (_availableInvestments != null && index >= 0 && index < _availableInvestments.Count)
            {
                _selectedInvestment = _availableInvestments[index];
                UpdateDisplay();
            }
        }

        private void OnAmountInputChanged(string value)
        {
            if (float.TryParse(value, out float amount))
            {
                _investAmount = Mathf.Max(0, amount);
            }
            else
            {
                _investAmount = 0;
            }

            // Update slider
            if (_amountSlider != null && _currencyManager != null)
            {
                float maxAmount = _currencyManager.InvestingBalance;
                _amountSlider.SetValueWithoutNotify(maxAmount > 0 ? _investAmount / maxAmount : 0);
            }

            UpdateProjection();
            UpdateBuyButton();
        }

        private void OnSliderChanged(float value)
        {
            if (_currencyManager == null) return;

            float maxAmount = _currencyManager.InvestingBalance;
            _investAmount = maxAmount * value;

            if (_amountInput != null)
            {
                _amountInput.SetTextWithoutNotify($"{_investAmount:F2}");
            }

            UpdateProjection();
            UpdateBuyButton();
        }

        private void OnMaxClicked()
        {
            if (_currencyManager == null) return;

            _investAmount = _currencyManager.InvestingBalance;

            if (_amountInput != null)
            {
                _amountInput.text = $"{_investAmount:F2}";
            }

            if (_amountSlider != null)
            {
                _amountSlider.value = 1f;
            }

            UpdateProjection();
            UpdateBuyButton();
        }

        private void OnBuyClicked()
        {
            if (_investmentSystem == null || _selectedInvestment == null || _investAmount <= 0) return;

            var investment = _investmentSystem.CreateInvestment(_selectedInvestment, _investAmount);

            if (investment != null)
            {
                UnityEngine.Debug.Log($"[BuyInvestmentPopup] Successfully invested ${_investAmount:F2} in {_selectedInvestment.DisplayName}");
                _uiManager.HidePopup(this);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[BuyInvestmentPopup] Failed to create investment");
                UpdateBuyButton();
            }
        }
    }
}
