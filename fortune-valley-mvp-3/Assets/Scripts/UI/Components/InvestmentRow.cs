using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Shows one investment type (owned or not) with inline buy/sell buttons.
    /// Used in the portfolio panel's investment list.
    /// </summary>
    public class InvestmentRow : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _sharesText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _gainText;
        [SerializeField] private TextMeshProUGUI _fixedReturnText;
        [SerializeField] private Image _riskDot;

        [Header("Buy Buttons")]
        [SerializeField] private Button _buy1Button;
        [SerializeField] private Button _buy5Button;
        [SerializeField] private Button _buy50Button;
        [SerializeField] private Button _buyMaxButton;

        [Header("Sell Buttons")]
        [SerializeField] private Button _sell1Button;
        [SerializeField] private Button _sell5Button;
        [SerializeField] private Button _sell50Button;
        [SerializeField] private Button _sellAllButton;

        [Header("Sell Button Container")]
        [SerializeField] private GameObject _sellButtonsContainer;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _neutralColor = new Color(0.7f, 0.7f, 0.7f);

        // Dependencies set at runtime
        private InvestmentDefinition _definition;
        private InvestmentSystem _investmentSystem;
        private CurrencyManager _currencyManager;

        private static readonly Color LOW_RISK_COLOR = new Color(0.2f, 0.7f, 0.3f);
        private static readonly Color MEDIUM_RISK_COLOR = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color HIGH_RISK_COLOR = new Color(0.9f, 0.3f, 0.2f);

        /// <summary>
        /// Wire up this row for a specific investment type.
        /// </summary>
        public void Setup(InvestmentDefinition definition, InvestmentSystem investmentSystem, CurrencyManager currencyManager)
        {
            _definition = definition;
            _investmentSystem = investmentSystem;
            _currencyManager = currencyManager;

            // Set static info
            if (_nameText != null)
                _nameText.text = definition.DisplayName;

            // Risk dot color
            if (_riskDot != null)
            {
                _riskDot.color = definition.RiskLevel switch
                {
                    RiskLevel.Low => LOW_RISK_COLOR,
                    RiskLevel.Medium => MEDIUM_RISK_COLOR,
                    RiskLevel.High => HIGH_RISK_COLOR,
                    _ => _neutralColor
                };
            }

            // Fixed return label (only for bonds/t-bills)
            if (_fixedReturnText != null)
            {
                if (definition.HasFixedReturn)
                {
                    _fixedReturnText.gameObject.SetActive(true);
                    _fixedReturnText.text = $"{definition.AnnualReturnRate * 100:F1}% APY";
                }
                else
                {
                    _fixedReturnText.gameObject.SetActive(false);
                }
            }

            // Wire buy buttons
            WireButton(_buy1Button, () => Buy(1));
            WireButton(_buy5Button, () => Buy(5));
            WireButton(_buy50Button, () => Buy(50));
            WireButton(_buyMaxButton, () => BuyMax());

            // Wire sell buttons
            WireButton(_sell1Button, () => Sell(1));
            WireButton(_sell5Button, () => Sell(5));
            WireButton(_sell50Button, () => Sell(50));
            WireButton(_sellAllButton, () => SellAll());

            Refresh();
        }

        /// <summary>
        /// Update dynamic display values (price, shares, gain/loss, button states).
        /// </summary>
        public void Refresh()
        {
            if (_definition == null || _investmentSystem == null) return;

            float currentPrice = _definition.CurrentPrice;
            ActiveInvestment holding = FindHolding();
            int sharesOwned = holding?.NumberOfShares ?? 0;
            float positionValue = holding?.CurrentValue ?? 0f;
            float gain = holding?.TotalGain ?? 0f;
            float percentReturn = holding?.PercentageReturn ?? 0f;

            // Price per share
            if (_priceText != null)
                _priceText.text = $"${currentPrice:F2}";

            // Shares owned
            if (_sharesText != null)
                _sharesText.text = $"{sharesOwned}";

            // Position value
            if (_valueText != null)
            {
                if (sharesOwned > 0)
                    _valueText.text = $"${positionValue:F2}";
                else
                    _valueText.text = "-";
            }

            // Gain/loss
            if (_gainText != null)
            {
                if (sharesOwned > 0)
                {
                    string prefix = gain >= 0 ? "+" : "";
                    _gainText.text = $"{prefix}${gain:F2} ({prefix}{percentReturn:F1}%)";
                    _gainText.color = gain >= 0 ? _gainColor : _lossColor;
                }
                else
                {
                    _gainText.text = "-";
                    _gainText.color = _neutralColor;
                }
            }

            // Update button states
            bool canAfford1 = _currencyManager != null && _currencyManager.CanAfford(currentPrice);
            SetButtonInteractable(_buy1Button, canAfford1);
            SetButtonInteractable(_buy5Button, _currencyManager != null && _currencyManager.CanAfford(currentPrice * 5));
            SetButtonInteractable(_buy50Button, _currencyManager != null && _currencyManager.CanAfford(currentPrice * 50));
            SetButtonInteractable(_buyMaxButton, canAfford1);

            // Sell buttons visible only when holding shares
            if (_sellButtonsContainer != null)
                _sellButtonsContainer.SetActive(sharesOwned > 0);

            SetButtonInteractable(_sell1Button, sharesOwned >= 1);
            SetButtonInteractable(_sell5Button, sharesOwned >= 5);
            SetButtonInteractable(_sell50Button, sharesOwned >= 50);
            SetButtonInteractable(_sellAllButton, sharesOwned > 0);
        }

        private ActiveInvestment FindHolding()
        {
            if (_investmentSystem == null || _definition == null) return null;

            foreach (var inv in _investmentSystem.ActiveInvestments)
            {
                if (inv.Definition == _definition)
                    return inv;
            }
            return null;
        }

        private void Buy(int count)
        {
            if (_investmentSystem != null && _definition != null)
            {
                _investmentSystem.BuyShares(_definition, count);
                Refresh();
            }
        }

        private void BuyMax()
        {
            if (_investmentSystem == null || _definition == null || _currencyManager == null) return;

            float price = _definition.CurrentPrice;
            int maxShares = Mathf.FloorToInt(_currencyManager.Balance / price);
            if (maxShares > 0)
            {
                _investmentSystem.BuyShares(_definition, maxShares);
                Refresh();
            }
        }

        private void Sell(int count)
        {
            var holding = FindHolding();
            if (holding != null && _investmentSystem != null)
            {
                _investmentSystem.SellShares(holding, count);
                Refresh();
            }
        }

        private void SellAll()
        {
            var holding = FindHolding();
            if (holding != null && _investmentSystem != null)
            {
                _investmentSystem.SellAllShares(holding);
                Refresh();
            }
        }

        private void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
