using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Trade tab: buy/sell interface for a selected stock.
    /// Shows price graph, stock details, and buy/sell buttons.
    /// Update-in-place (tick-driven).
    ///
    /// Receives selection via:
    /// - OnTradeRequested event (from Portfolio detail view Trade button)
    /// - _exploreSubPanel.SelectedDefinition (read on enable, for Explore tab flow)
    ///
    /// LEARNING DESIGN: Students make buy/sell decisions seeing real-time
    /// price movement, learning about market timing and risk tolerance.
    /// </summary>
    public class InvestingTradeSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private StockPriceHistoryStore _stockHistory;

        [Header("Selection Source")]
        [Tooltip("The Explore sub-panel. Selection is read on enable (for Explore->Trade flow).")]
        [SerializeField] private InvestingExploreSubPanel _exploreSubPanel;

        [Header("Graph")]
        [SerializeField] private TMP_FontAsset _labelFont;
        [SerializeField] private Image _graphPlaceholder;

        [Header("Stock Details")]
        [SerializeField] private TextMeshProUGUI _selectedAssetText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private TextMeshProUGUI _priceChangeText;
        [SerializeField] private TextMeshProUGUI _sharesOwnedText;
        [SerializeField] private TextMeshProUGUI _riskLevelText;
        [SerializeField] private TextMeshProUGUI _averagePriceText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Actions")]
        [SerializeField] private Button _buyButton;
        [SerializeField] private Button _sellButton;

        [Header("Trading")]
        [SerializeField] private int _sharesPerTrade = 1;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        private LineGraphGraphic _stockGraph;
        private int _currentDayTick;
        private InvestmentDefinition _selectedDefinition;

        // Cached for price change display
        private Dictionary<InvestmentDefinition, float> _previousPrices
            = new Dictionary<InvestmentDefinition, float>();

        // Reusable buffer for graph data (avoids per-tick allocation)
        private List<float> _graphBuffer = new List<float>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
            GameEvents.OnTradeRequested += HandleTradeRequested;

            if (_buyButton != null)
                _buyButton.onClick.AddListener(OnBuyClicked);
            if (_sellButton != null)
                _sellButton.onClick.AddListener(OnSellClicked);

            EnsureGraph();

            // Read selection from Explore tab (for Explore->Trade flow)
            if (_exploreSubPanel != null && _exploreSubPanel.SelectedDefinition != null)
                _selectedDefinition = _exploreSubPanel.SelectedDefinition;

            SnapshotPrices();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
            GameEvents.OnTradeRequested -= HandleTradeRequested;

            if (_buyButton != null)
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            if (_sellButton != null)
                _sellButton.onClick.RemoveListener(OnSellClicked);

            // Do NOT clear _selectedDefinition -- persist across enable/disable

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleTick(int tickNumber)
        {
            _currentDayTick = tickNumber;
            SnapshotPrices();

            // Check if Explore tab changed selection (for live browsing)
            if (_exploreSubPanel != null && _exploreSubPanel.SelectedDefinition != null
                && _exploreSubPanel.SelectedDefinition != _selectedDefinition)
            {
                _selectedDefinition = _exploreSubPanel.SelectedDefinition;
            }

            Refresh();
        }

        private void HandleBalanceChanged(float balance, float delta) => Refresh();

        private void HandleTradeRequested(InvestmentDefinition def)
        {
            if (def == null) return;
            _selectedDefinition = def;
            Refresh();
        }

        // ===============================================================
        // BUY / SELL (intent events)
        // ===============================================================

        private void OnBuyClicked()
        {
            if (_selectedDefinition == null) return;

            GameEvents.RaiseBuySharesRequested(_selectedDefinition, _sharesPerTrade);
            Refresh();
        }

        private void OnSellClicked()
        {
            if (_selectedDefinition == null || _investmentSystem == null) return;

            var inv = FindActiveInvestment(_selectedDefinition);
            if (inv != null)
                GameEvents.RaiseSellSharesRequested(inv, _sharesPerTrade);

            Refresh();
        }

        // ===============================================================
        // REFRESH (update-in-place, single stock view)
        // ===============================================================

        protected override void Refresh()
        {
            if (_selectedDefinition == null)
            {
                ShowPlaceholder();
                return;
            }

            UIBuilderUtils.SetTextIfChanged(_selectedAssetText, _selectedDefinition.DisplayName);

            // Price and change
            UIBuilderUtils.SetTextIfChanged(_priceText, $"Price: ${_selectedDefinition.CurrentPrice:F2}");

            float change = GetPriceChangePercent(_selectedDefinition);
            string changeStr = $"Change: {(change >= 0 ? "+" : "")}{change:F2}%";
            UIBuilderUtils.SetTextIfChanged(_priceChangeText, changeStr);
            if (_priceChangeText != null)
                _priceChangeText.color = change >= 0 ? _gainColor : _lossColor;

            // Risk level
            if (_riskLevelText != null)
            {
                UIBuilderUtils.SetTextIfChanged(_riskLevelText, $"Risk: {_selectedDefinition.RiskLevel}");
                _riskLevelText.color = _selectedDefinition.RiskLevel switch
                {
                    RiskLevel.Low    => _gainColor,
                    RiskLevel.Medium => new Color(1f, 0.8f, 0.2f),
                    RiskLevel.High   => _lossColor,
                    _                => Color.white
                };
            }

            // Shares owned
            var activeInv = FindActiveInvestment(_selectedDefinition);
            int sharesOwned = activeInv != null ? activeInv.NumberOfShares : 0;

            UIBuilderUtils.SetTextIfChanged(_sharesOwnedText, $"Owned: {sharesOwned}");

            string avgPriceStr = sharesOwned > 0
                ? $"Avg Cost: ${activeInv.AveragePurchasePrice:F2}"
                : "Avg Cost: $---";
            UIBuilderUtils.SetTextIfChanged(_averagePriceText, avgPriceStr);

            UIBuilderUtils.SetTextIfChanged(_descriptionText, sharesOwned > 0
                ? "Tap Sell to remove 1 share."
                : "Tap Buy to purchase 1 share.");

            // Button states
            if (_buyButton != null && _currencyManager != null)
                _buyButton.interactable = _currencyManager.CanAffordInvesting(_selectedDefinition.CurrentPrice);
            if (_sellButton != null)
                _sellButton.gameObject.SetActive(sharesOwned > 0);

            // Stock price graph
            RefreshGraph();
        }

        private void ShowPlaceholder()
        {
            UIBuilderUtils.SetTextIfChanged(_selectedAssetText, "Select an investment");
            UIBuilderUtils.SetTextIfChanged(_priceText, "Price: $---");
            UIBuilderUtils.SetTextIfChanged(_priceChangeText, "Change: ---%");
            UIBuilderUtils.SetTextIfChanged(_sharesOwnedText, "Owned: 0");
            UIBuilderUtils.SetTextIfChanged(_riskLevelText, "Risk: ---");
            UIBuilderUtils.SetTextIfChanged(_averagePriceText, "Avg Cost: $---");
            UIBuilderUtils.SetTextIfChanged(_descriptionText, "Select a stock to see details.");
            if (_priceChangeText != null) _priceChangeText.color = Color.white;
            if (_riskLevelText != null) _riskLevelText.color = Color.white;
            if (_buyButton != null) _buyButton.interactable = false;
            if (_sellButton != null) _sellButton.gameObject.SetActive(false);
        }

        private void RefreshGraph()
        {
            StockGraphHelper.RefreshGraph(
                _stockGraph, _stockHistory, _selectedDefinition,
                30, _currentDayTick, _graphBuffer);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void EnsureGraph()
        {
            if (_stockGraph != null || _graphPlaceholder == null) return;
            _stockGraph = StockGraphHelper.EnsureGraphCreated(_graphPlaceholder, _labelFont);
        }

        private ActiveInvestment FindActiveInvestment(InvestmentDefinition def)
        {
            if (_investmentSystem == null) return null;
            var investments = _investmentSystem.ActiveInvestments;
            for (int i = 0; i < investments.Count; i++)
            {
                if (investments[i].Definition == def)
                    return investments[i];
            }
            return null;
        }

        private void SnapshotPrices()
        {
            if (_investmentSystem == null) return;
            var investments = _investmentSystem.AvailableInvestments;
            for (int i = 0; i < investments.Count; i++)
                _previousPrices[investments[i]] = investments[i].CurrentPrice;
        }

        private float GetPriceChangePercent(InvestmentDefinition def)
        {
            if (_previousPrices.TryGetValue(def, out float prev) && prev > 0)
                return (def.CurrentPrice - prev) / prev * 100f;

            if (def.BasePricePerShare > 0)
                return (def.CurrentPrice - def.BasePricePerShare) / def.BasePricePerShare * 100f;

            return 0f;
        }
    }
}
