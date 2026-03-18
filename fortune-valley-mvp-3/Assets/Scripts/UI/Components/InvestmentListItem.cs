using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// A single row in the portfolio showing an active investment.
    /// </summary>
    public class InvestmentListItem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _gainText;
        [SerializeField] private TextMeshProUGUI _principalText;
        [SerializeField] private TextMeshProUGUI _daysHeldText;

        [Header("Risk Indicator")]
        [SerializeField] private Image _riskIcon;
        [SerializeField] private TextMeshProUGUI _riskText;

        [Header("Buttons")]
        [SerializeField] private Button _sellButton;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        [SerializeField] private Color _lowRiskColor = new Color(0.2f, 0.7f, 0.2f);
        [SerializeField] private Color _mediumRiskColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _highRiskColor = new Color(0.9f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private ActiveInvestment _investment;
        private Action<ActiveInvestment> _onSellClick;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Setup the list item with investment data.
        /// </summary>
        public void Setup(ActiveInvestment investment, Action<ActiveInvestment> onSellClick)
        {
            _investment = investment;
            _onSellClick = onSellClick;

            UpdateDisplay();
            SetupButton();
        }

        /// <summary>
        /// Refresh the display with current values.
        /// </summary>
        public void Refresh()
        {
            UpdateDisplay();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateDisplay()
        {
            if (_investment == null) return;

            // Name
            if (_nameText != null)
            {
                _nameText.text = _investment.Definition.DisplayName;
            }

            // Current value
            if (_valueText != null)
            {
                _valueText.text = $"${_investment.CurrentValue:N2}";
            }

            // Gain/loss
            if (_gainText != null)
            {
                float gain = _investment.TotalGain;
                float percent = _investment.PercentageReturn;
                string prefix = gain >= 0 ? "+" : "";

                _gainText.text = $"{prefix}${gain:N2} ({prefix}{percent:F1}%)";
                _gainText.color = gain >= 0 ? _gainColor : _lossColor;
            }

            // Principal (original investment)
            if (_principalText != null)
            {
                _principalText.text = $"Principal: ${_investment.Principal:N2}";
            }

            // Days held
            if (_daysHeldText != null)
            {
                _daysHeldText.text = $"{_investment.TicksHeld} days";
            }

            // Risk indicator
            UpdateRiskIndicator();
        }

        private void UpdateRiskIndicator()
        {
            if (_investment?.Definition == null) return;

            RiskLevel risk = _investment.Definition.RiskLevel;
            Color riskColor;
            string riskText;

            switch (risk)
            {
                case RiskLevel.Low:
                    riskColor = _lowRiskColor;
                    riskText = "Low Risk";
                    break;
                case RiskLevel.Medium:
                    riskColor = _mediumRiskColor;
                    riskText = "Med Risk";
                    break;
                case RiskLevel.High:
                    riskColor = _highRiskColor;
                    riskText = "High Risk";
                    break;
                default:
                    riskColor = Color.gray;
                    riskText = "Unknown";
                    break;
            }

            if (_riskIcon != null)
            {
                _riskIcon.color = riskColor;
            }

            if (_riskText != null)
            {
                _riskText.text = riskText;
                _riskText.color = riskColor;
            }
        }

        private void SetupButton()
        {
            if (_sellButton != null)
            {
                _sellButton.onClick.RemoveAllListeners();
                _sellButton.onClick.AddListener(OnSellClicked);
            }
        }

        private void OnSellClicked()
        {
            _onSellClick?.Invoke(_investment);
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public ActiveInvestment Investment => _investment;
    }
}
