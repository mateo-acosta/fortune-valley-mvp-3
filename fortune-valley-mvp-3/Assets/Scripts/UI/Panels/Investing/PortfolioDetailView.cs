using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Detail view for a selected portfolio holding.
    /// Shows stock title, price, graph, stats, and trade button.
    /// Communicates via GameEvents only -- no direct reference to the holdings list.
    ///
    /// LEARNING DESIGN: Students see detailed performance metrics for each
    /// holding, reinforcing concepts like average cost basis, unrealized P/L,
    /// and portfolio diversification through position ratio.
    /// </summary>
    public class PortfolioDetailView : MonoBehaviour
    {
        // ===============================================================
        // DEPENDENCIES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private StockPriceHistoryStore _stockHistory;

        [Header("Time Filter")]
        [SerializeField] private FilterRowController _timeFilter;

        [Tooltip("Day count for each button index. E.g. [7, 30, 60, 200]")]
        [SerializeField] private int[] _timeWindowMapping;

        [Header("Graph")]
        [SerializeField] private Image _graphPlaceholder;
        [SerializeField] private TMP_FontAsset _labelFont;

        [Header("Stock Info")]
        [SerializeField] private TMP_Text _stockTitleText;
        [SerializeField] private TMP_Text _stockPriceText;
        [SerializeField] private TMP_Text _priceChangeText;

        [Header("Stats")]
        [SerializeField] private TMP_Text _avgPriceText;
        [SerializeField] private TMP_Text _openPLText;
        [SerializeField] private TMP_Text _sharesText;
        [SerializeField] private TMP_Text _positionRatioText;

        [Header("Actions")]
        [SerializeField] private Button _tradeButton;
        [SerializeField] private SidebarController _sidebarController;
        [SerializeField] private int _tradeTabIndex;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateText;
        [SerializeField] private GameObject _detailContent;

        [Header("Graph Settings")]
        [Tooltip("Default graph window if time filter is unavailable")]
        [SerializeField] private int _defaultGraphWindow = 30;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        private ActiveInvestment _selectedHolding;
        private LineGraphGraphic _stockGraph;
        private float _previousPrice;
        private int _graphTickCounter;
        private int _currentDayTick;
        private List<float> _graphBuffer = new List<float>();

        [SerializeField] private int _graphRefreshInterval = 5;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnPortfolioHoldingSelected += HandleHoldingSelected;
            GameEvents.OnTick += HandleTick;
            GameEvents.OnInvestmentWithdrawn += HandleWithdrawal;
            GameEvents.OnInvestmentCreated += HandleCreation;

            if (_timeFilter != null)
                _timeFilter.OnSelectionChanged += HandleTimeFilterChanged;
            if (_tradeButton != null)
                _tradeButton.onClick.AddListener(HandleTradeClicked);

            // If we already had a selection from before disable, refresh it
            if (_selectedHolding != null)
                RefreshDetail();
        }

        private void OnDisable()
        {
            GameEvents.OnPortfolioHoldingSelected -= HandleHoldingSelected;
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnInvestmentWithdrawn -= HandleWithdrawal;
            GameEvents.OnInvestmentCreated -= HandleCreation;

            if (_timeFilter != null)
                _timeFilter.OnSelectionChanged -= HandleTimeFilterChanged;
            if (_tradeButton != null)
                _tradeButton.onClick.RemoveListener(HandleTradeClicked);

            // Do NOT clear _selectedHolding -- persist across enable/disable
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleHoldingSelected(ActiveInvestment holding)
        {
            _selectedHolding = holding;

            if (holding != null && holding.Definition != null)
                _previousPrice = holding.Definition.CurrentPrice;

            _graphTickCounter = 0;
            EnsureGraph();
            RefreshDetail();
            RefreshGraph();
        }

        private void HandleTick(int tickNumber)
        {
            _currentDayTick = tickNumber;

            if (_selectedHolding == null) return;

            UpdatePriceFields();

            _graphTickCounter++;
            if (_graphTickCounter >= _graphRefreshInterval)
            {
                RefreshGraph();
                _graphTickCounter = 0;
            }
        }

        private void HandleTimeFilterChanged(int index)
        {
            RefreshGraph();
        }

        private void HandleWithdrawal(ActiveInvestment withdrawn, float payout)
        {
            if (withdrawn != _selectedHolding) return;

            // The selected holding was sold -- try to select the next available
            if (_investmentSystem == null || _investmentSystem.ActiveInvestments.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            // Select the first remaining holding
            var next = _investmentSystem.ActiveInvestments[0];
            GameEvents.RaisePortfolioHoldingSelected(next);
        }

        private void HandleCreation(ActiveInvestment created)
        {
            // If we're showing empty state, select the new holding
            if (_selectedHolding == null)
                GameEvents.RaisePortfolioHoldingSelected(created);
        }

        private void HandleTradeClicked()
        {
            if (_selectedHolding == null || _selectedHolding.Definition == null) return;
            GameEvents.RaiseTradeRequested(_selectedHolding.Definition);

            // Navigate to the Trade tab
            if (_sidebarController != null)
                _sidebarController.SwitchTo(_tradeTabIndex);
        }

        // ===============================================================
        // DETAIL POPULATION
        // ===============================================================

        private void RefreshDetail()
        {
            if (_selectedHolding == null || _selectedHolding.Definition == null)
            {
                ShowEmptyState();
                return;
            }

            ShowDetailContent();

            var def = _selectedHolding.Definition;

            if (_stockTitleText != null)
                _stockTitleText.text = def.DisplayName;

            if (_stockPriceText != null)
                _stockPriceText.text = $"${def.CurrentPrice:F2}";

            UpdatePriceChangeText();
            UpdateStatsFields();
        }

        /// <summary>
        /// Lightweight per-tick update for price-sensitive fields only.
        /// Does not rebuild the graph.
        /// </summary>
        private void UpdatePriceFields()
        {
            if (_selectedHolding == null || _selectedHolding.Definition == null) return;

            var def = _selectedHolding.Definition;

            if (_stockPriceText != null)
                _stockPriceText.text = $"${def.CurrentPrice:F2}";

            UpdatePriceChangeText();
            UpdateStatsFields();
        }

        private void UpdatePriceChangeText()
        {
            if (_priceChangeText == null || _selectedHolding == null) return;

            var def = _selectedHolding.Definition;
            float change = CalculatePriceChangePercent(def.CurrentPrice, _previousPrice);

            _priceChangeText.text = $"{(change >= 0 ? "+" : "")}{change:F1}%";
            _priceChangeText.color = change >= 0 ? _gainColor : _lossColor;
        }

        private void UpdateStatsFields()
        {
            if (_selectedHolding == null) return;

            if (_avgPriceText != null)
                _avgPriceText.text = $"${_selectedHolding.AveragePurchasePrice:F2}";

            if (_openPLText != null)
            {
                float gain = _selectedHolding.TotalGain;
                _openPLText.text = $"{(gain >= 0 ? "+" : "")}${gain:N0}";
                _openPLText.color = gain >= 0 ? _gainColor : _lossColor;
            }

            if (_sharesText != null)
                _sharesText.text = $"{_selectedHolding.NumberOfShares}";

            if (_positionRatioText != null)
                _positionRatioText.text = CalculatePositionRatio(
                    _selectedHolding.CurrentValue,
                    _investmentSystem != null ? _investmentSystem.TotalPortfolioValue : 0f);
        }

        // ===============================================================
        // GRAPH
        // ===============================================================

        private void EnsureGraph()
        {
            if (_stockGraph != null || _graphPlaceholder == null) return;
            _stockGraph = StockGraphHelper.EnsureGraphCreated(_graphPlaceholder, _labelFont);
        }

        private void RefreshGraph()
        {
            if (_selectedHolding == null || _selectedHolding.Definition == null) return;

            EnsureGraph();
            int windowSize = GetCurrentWindowSize();

            StockGraphHelper.RefreshGraph(
                _stockGraph, _stockHistory, _selectedHolding.Definition,
                windowSize, _currentDayTick, _graphBuffer);
        }

        private int GetCurrentWindowSize()
        {
            if (_timeFilter == null || _timeWindowMapping == null)
                return _defaultGraphWindow; // default 30-day window

            int index = _timeFilter.SelectedIndex;
            if (index >= 0 && index < _timeWindowMapping.Length)
                return _timeWindowMapping[index];

            return _defaultGraphWindow;
        }

        // ===============================================================
        // EMPTY STATE
        // ===============================================================

        private void ShowEmptyState()
        {
            _selectedHolding = null;
            if (_emptyStateText != null) _emptyStateText.SetActive(true);
            if (_detailContent != null) _detailContent.SetActive(false);
        }

        private void ShowDetailContent()
        {
            if (_emptyStateText != null) _emptyStateText.SetActive(false);
            if (_detailContent != null) _detailContent.SetActive(true);
        }

        // ===============================================================
        // PURE CALCULATIONS (testable)
        // ===============================================================

        private const float PercentMultiplier = 100f;

        /// <summary>
        /// Calculate position ratio as a formatted string.
        /// Returns "--" if total portfolio value is zero to avoid division by zero.
        /// </summary>
        public static string CalculatePositionRatio(float holdingValue, float totalPortfolioValue)
        {
            if (totalPortfolioValue <= 0f) return "--";
            float ratio = (holdingValue / totalPortfolioValue) * PercentMultiplier;
            return $"{ratio:F1}%";
        }

        /// <summary>
        /// Calculate price change percentage from previous to current.
        /// Returns 0 if previous price is zero or negative.
        /// </summary>
        public static float CalculatePriceChangePercent(float current, float previous)
        {
            if (previous <= 0f) return 0f;
            return (current - previous) / previous * PercentMultiplier;
        }
    }
}
