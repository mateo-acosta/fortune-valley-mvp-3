using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Portfolio panel with two-tab layout (Overview / Invest).
    /// Overview tab: summary stats + current holdings.
    /// Invest tab:   stock rows wired by label → InvestmentDefinition, with buy/sell detail panel.
    /// All UI paths are relative to this GameObject — no manual inspector wiring needed
    /// beyond the serialized dependencies at the top.
    /// </summary>
    public class PortfolioPanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // INSPECTOR DEPENDENCIES
        // ═══════════════════════════════════════════════════════════════

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private PortfolioHistoryTracker _historyTracker;
        [SerializeField] private StockPriceHistoryStore _stockHistory;

        [Header("Trading")]
        [Tooltip("Number of shares per buy/sell click")]
        [SerializeField] private int _sharesPerTrade = 1;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset _labelFont; // assign Rubik-Medium SDF in Inspector

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME REFERENCES (found by path)
        // ═══════════════════════════════════════════════════════════════

        // Tab panel roots
        private GameObject _overviewPanel;
        private GameObject _investPanel;

        // Tab buttons + cached Image refs (avoid GetComponent on every switch)
        private Button _tab1Button;
        private Button _tab2Button;
        private Image  _tab1Image;
        private Image  _tab2Image;
        private Color  _tabNormalColor;
        private Color  _tabActiveColor = new Color(0.35f, 0.55f, 0.75f);

        // Overview stat texts
        private TextMeshProUGUI _balanceText;
        private TextMeshProUGUI _investmentsValueText;
        private TextMeshProUGUI _totalGainText;
        private TextMeshProUGUI _portfolioLevelText;
        private TextMeshProUGUI _currentHoldingsText;

        // Invest panel containers (existing scene row children)
        private Transform _highRiskContainer;   // HighRiskStocks/Companies
        private Transform _lowRiskContainer;    // LowRiskStocks/Companies

        // Detail panel texts
        private TextMeshProUGUI _selectedAssetText;
        private TextMeshProUGUI _priceText;
        private TextMeshProUGUI _priceChangeText;
        private TextMeshProUGUI _sharesOwnedText;
        private TextMeshProUGUI _riskLevelText;
        private TextMeshProUGUI _averagePriceText;
        private TextMeshProUGUI _descriptionText;

        // Action buttons
        private Button _closeButton;
        private Button _buyButton;
        private Button _sellButton;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        // Graph components (created at runtime inside the placeholder Image GOs)
        private LineGraphGraphic _overviewGraph;
        private LineGraphGraphic _stockGraph;

        private bool _initialized;
        private int _activeTabIndex; // 0 = Overview, 1 = Invest
        private int _currentDayTick;
        private InvestmentDefinition _selectedDefinition;

        // Track previous prices per tick for daily change display
        private Dictionary<InvestmentDefinition, float> _previousPrices
            = new Dictionary<InvestmentDefinition, float>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;

            FindDependencies();
            if (_investmentSystem == null || _currencyManager == null)
            {
                UnityEngine.Debug.LogError("[PortfolioPanel] Missing dependencies — will retry on next OnShow()");
                return; // Don't set _initialized — allow retry
            }

            FindUIElements();
            WireTabButtons();
            WireActionButtons();
            WireInvestmentButtons(); // Wire existing scene rows once at startup

            _initialized = true;
        }

        private void FindDependencies()
        {
            if (_investmentSystem == null) UnityEngine.Debug.LogError("[PortfolioPanel] _investmentSystem not wired in Inspector.");
            if (_currencyManager == null) UnityEngine.Debug.LogError("[PortfolioPanel] _currencyManager not wired in Inspector.");
            if (_historyTracker == null) UnityEngine.Debug.LogError("[PortfolioPanel] _historyTracker not wired in Inspector.");
            if (_stockHistory == null) UnityEngine.Debug.LogError("[PortfolioPanel] _stockHistory not wired in Inspector.");
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;
        }

        // ═══════════════════════════════════════════════════════════════
        // PANEL OVERRIDE
        // ═══════════════════════════════════════════════════════════════

        protected override void OnShow()
        {
            Initialize();
            SnapshotPrices();
            SwitchToTab(0); // always open to Overview; internally calls RefreshOverviewPanel()
        }

        // ═══════════════════════════════════════════════════════════════
        // UI ELEMENT DISCOVERY (by path from this transform)
        // ═══════════════════════════════════════════════════════════════

        private void FindUIElements()
        {
            // Tab buttons
            _tab1Button = FindButton("TopFrame/Tab1");
            _tab2Button = FindButton("TopFrame/Tab2");
            _tab1Image  = _tab1Button?.GetComponent<Image>();
            _tab2Image  = _tab2Button?.GetComponent<Image>();
            _tabNormalColor = _tab1Image != null ? _tab1Image.color : new Color(0.2f, 0.2f, 0.2f);

            // Close button
            _closeButton = FindButton("TopFrame/Button_Close");

            // Panel roots
            var overviewT = transform.Find("OverviewPanel");
            _overviewPanel = overviewT != null ? overviewT.gameObject : null;
            var investT = transform.Find("InvestPanel");
            _investPanel = investT != null ? investT.gameObject : null;

            // Overview stats
            var statsBase = "OverviewPanel/Stats_Group/SummaryStatistics";
            _balanceText          = FindText($"{statsBase}/BalanceText");
            _investmentsValueText = FindText($"{statsBase}/InvestmentsValue");
            _totalGainText        = FindText($"{statsBase}/TotalGainText");
            _portfolioLevelText   = FindText($"{statsBase}/PortfolioLevelText");
            _currentHoldingsText  = FindText($"{statsBase}/CurrentHoldings");

            // Invest panel containers
            var optBase = "InvestPanel/InvestmentInfoPanel/InvestmentOptions";
            var highRiskT = transform.Find($"{optBase}/HighRiskStocks/Companies");
            _highRiskContainer = highRiskT;
            var lowRiskT = transform.Find($"{optBase}/LowRiskStocks/Companies");
            _lowRiskContainer = lowRiskT;

            // Detail panel texts
            var bsp = "InvestPanel/InvestmentInfoPanel/BuySellPanel";
            _selectedAssetText = FindText($"{bsp}/SelectedAssetText");
            _priceText         = FindText($"{bsp}/SelectedAssetInfo/PriceText");
            _priceChangeText   = FindText($"{bsp}/SelectedAssetInfo/PriceChangeText");
            _sharesOwnedText   = FindText($"{bsp}/SelectedAssetInfo/SharesOwnedText");
            _riskLevelText     = FindText($"{bsp}/SelectedAssetInfo/RiskLevelText");
            _averagePriceText  = FindText($"{bsp}/SelectedAssetInfo/AveragePriceText");
            _descriptionText   = FindText($"{bsp}/Buy or Sell/DescriptionText");

            // Action buttons
            _buyButton  = FindButton($"{bsp}/Buy or Sell/ButtonGrid/BuyButton");
            _sellButton = FindButton($"{bsp}/Buy or Sell/ButtonGrid/SellButton");

            // Graphs — created as children of the existing placeholder Image GOs
            _overviewGraph = CreateGraphInPlaceholder("OverviewPanel/PortfolioPerformance/Image");
            _stockGraph    = CreateGraphInPlaceholder("InvestPanel/InvestmentInfoPanel/BuySellPanel/Image");
        }

        /// <summary>
        /// Finds the placeholder Image at <paramref name="imagePath"/> relative to this transform,
        /// creates a "Graph" child with LineGraphGraphic, and returns the component.
        /// Returns null (with a warning) if the path is not found.
        /// </summary>
        private LineGraphGraphic CreateGraphInPlaceholder(string imagePath)
        {
            Transform placeholder = transform.Find(imagePath);
            if (placeholder == null)
            {
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Graph placeholder not found at: {imagePath}");
                return null;
            }

            // Clear the placeholder background so the graph draws on the card's white surface
            var img = placeholder.GetComponent<Image>();
            if (img != null) img.color = Color.clear;

            // CanvasRenderer must be added explicitly at runtime — [RequireComponent] only
            // auto-adds in the Editor. Without it, GraphicRaycaster throws
            // MissingComponentException and breaks ALL button clicks in the canvas.
            var go = new GameObject("Graph", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(placeholder, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Inset from the placeholder edges so Y-label (left) and X-label (bottom) aren't clipped
            rt.offsetMin = new Vector2(LineGraphGraphic.YLabelWidth - 8f, LineGraphGraphic.XLabelHeight + 2f);
            rt.offsetMax = Vector2.zero;

            var graph = go.AddComponent<LineGraphGraphic>();
            graph.SetLabelFont(_labelFont);
            return graph;
        }

        // ═══════════════════════════════════════════════════════════════
        // TAB SWITCHING
        // ═══════════════════════════════════════════════════════════════

        private void WireTabButtons()
        {
            _tab1Button?.onClick.AddListener(() => SwitchToTab(0));
            _tab2Button?.onClick.AddListener(() => SwitchToTab(1));
        }

        private void SwitchToTab(int index)
        {
            _activeTabIndex = index;
            _overviewPanel?.SetActive(index == 0);
            _investPanel?.SetActive(index == 1);

            if (_tab1Image != null) _tab1Image.color = index == 0 ? _tabActiveColor : _tabNormalColor;
            if (_tab2Image != null) _tab2Image.color = index == 1 ? _tabActiveColor : _tabNormalColor;

            if (index == 0) RefreshOverviewPanel();
            else            RefreshDetailPanel();
        }

        // ═══════════════════════════════════════════════════════════════
        // BUY / SELL WIRING
        // ═══════════════════════════════════════════════════════════════

        private void WireActionButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
                if (_closeButton.targetGraphic == null)
                    _closeButton.targetGraphic = _closeButton.GetComponent<Image>();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PortfolioPanel] Close button not found");
            }

            _buyButton?.onClick.AddListener(OnBuyButtonClicked);
            _sellButton?.onClick.AddListener(OnSellButtonClicked);
        }

        private void OnBuyButtonClicked()
        {
            if (_selectedDefinition == null) return;

            GameEvents.RaiseBuySharesRequested(_selectedDefinition, _sharesPerTrade);

            // Safe: Unity events invoke handlers synchronously before returning.
            // Investment state is already updated by the time Refresh runs.
            RefreshDetailPanel();
            RefreshOverviewPanel();
        }

        private void OnSellButtonClicked()
        {
            if (_selectedDefinition == null) return;

            var inv = GetActiveInvestment(_selectedDefinition);
            if (inv != null)
                GameEvents.RaiseSellSharesRequested(inv, _sharesPerTrade);

            RefreshDetailPanel();
            RefreshOverviewPanel();
        }

        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT BUTTON WIRING (scene rows → definitions, once at init)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Traverse existing stock row GameObjects in each Companies container.
        /// Reads each row's TitleText to find the matching InvestmentDefinition by DisplayName.
        /// Wires the row's Button child to OnInvestmentSelected with that definition.
        /// Called once inside Initialize() — scene rows never change at runtime.
        /// </summary>
        private void WireInvestmentButtons()
        {
            WireContainerButtons(_highRiskContainer);
            WireContainerButtons(_lowRiskContainer);
        }

        private void WireContainerButtons(Transform container)
        {
            if (container == null || _investmentSystem == null) return;

            for (int i = 0; i < container.childCount; i++)
            {
                Transform row = container.GetChild(i);

                // Read the label from the TitleText child — key for definition lookup
                var titleText = row.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                string rowLabel = titleText != null ? titleText.text.Trim() : row.name;

                var def = FindDefinitionByLabel(rowLabel);
                if (def == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[PortfolioPanel] No InvestmentDefinition found for row label '{rowLabel}'. " +
                        $"Ensure a Stock definition's DisplayName matches this label.");
                    continue;
                }

                // Find the Button child — expected to be named "Button"
                var btn = row.Find("Button")?.GetComponent<Button>();
                if (btn == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[PortfolioPanel] Row '{rowLabel}' has no Button child named 'Button'.");
                    continue;
                }

                var capturedDef = def; // closure-safe capture in loop
                btn.onClick.AddListener(() => OnInvestmentSelected(capturedDef));
            }
        }

        /// <summary>
        /// Find a Stock definition whose DisplayName matches the given label.
        /// Tries exact match first, then case-insensitive contains as fallback.
        /// </summary>
        private InvestmentDefinition FindDefinitionByLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return null;

            var stocks = _investmentSystem.AvailableInvestments
                .Where(d => d.Category == InvestmentCategory.Stock);

            // Exact match (case-insensitive)
            var match = stocks.FirstOrDefault(d =>
                string.Equals(d.DisplayName, label, System.StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Fallback: contains match (e.g. scene says "AMZN", def says "AMZN Stock")
            return stocks.FirstOrDefault(d =>
                d.DisplayName.IndexOf(label, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                label.IndexOf(d.DisplayName, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void OnInvestmentSelected(InvestmentDefinition def)
        {
            _selectedDefinition = def;
            RefreshDetailPanel();
            RefreshStockGraph();
        }

        // ═══════════════════════════════════════════════════════════════
        // OVERVIEW PANEL REFRESH
        // ═══════════════════════════════════════════════════════════════

        private void RefreshOverviewPanel()
        {
            if (_investmentSystem == null || _currencyManager == null) return;

            float balance      = _currencyManager.InvestingBalance;
            float portfolioVal = _investmentSystem.TotalPortfolioValue;
            // LifetimeTotalGain = realized gains from all sales + current unrealized gain on open positions.
            // This never drops after a profitable sale, matching the "cumulative investment gain" intent.
            float totalGain    = _investmentSystem.LifetimeTotalGain;
            var   holdings     = _investmentSystem.ActiveInvestments;

            UIBuilderUtils.SetTextIfChanged(_balanceText,          $"Balance: ${balance:N0}");
            UIBuilderUtils.SetTextIfChanged(_investmentsValueText, $"Invested: ${portfolioVal:N0}");

            string gainStr = $"Total Gain: {(totalGain >= 0 ? "+" : "")}${totalGain:N0}";
            UIBuilderUtils.SetTextIfChanged(_totalGainText, gainStr);
            if (_totalGainText != null)
                _totalGainText.color = totalGain >= 0 ? _gainColor : _lossColor;

            UIBuilderUtils.SetTextIfChanged(_portfolioLevelText,
                $"Risk: {PortfolioPanelLogic.GetPortfolioRiskLabel(holdings)}");
            UIBuilderUtils.SetTextIfChanged(_currentHoldingsText,
                PortfolioPanelLogic.BuildHoldingsSummary(holdings));

            // Update portfolio value graph on Overview tab — two lines so students can see
            // total wealth (red) and net investment gain (yellow) separately (opportunity cost signal)
            if (_historyTracker != null && _overviewGraph != null)
            {
                var window     = GetLastN(_historyTracker.TotalWealthHistory, 30);
                var gainWindow = GetLastN(_historyTracker.NetGainHistory, 30);
                int startDay   = _currentDayTick - (window.Count - 1); // may be negative early on
                _overviewGraph.SetData(window, gainWindow, startDay);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // STOCK GRAPH
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Update the per-stock price history graph on the Invest tab.</summary>
        private void RefreshStockGraph()
        {
            if (_selectedDefinition == null || _stockHistory == null || _stockGraph == null) return;

            var window   = _stockHistory.GetWindow(_selectedDefinition, 30);
            int startDay = _currentDayTick - (window.Count - 1); // negative for pre-game days
            _stockGraph.SetData(window, startDay);
        }

        /// <summary>Returns a new list of the last min(n, source.Count) entries.</summary>
        private static IReadOnlyList<float> GetLastN(IReadOnlyList<float> source, int n)
        {
            if (source == null || source.Count == 0) return new List<float>();
            int start = System.Math.Max(0, source.Count - n);
            var result = new List<float>(source.Count - start);
            for (int i = start; i < source.Count; i++)
                result.Add(source[i]);
            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        // DETAIL PANEL REFRESH
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Shared price/change/affordability update used by both RefreshDetailPanel and UpdateDetailPriceTick.</summary>
        private void UpdatePriceDisplay()
        {
            if (_selectedDefinition == null) return;

            UIBuilderUtils.SetTextIfChanged(_priceText, $"Price: ${_selectedDefinition.CurrentPrice:F2}");

            float change = GetPriceChangePercent(_selectedDefinition);
            string changeStr = $"Change: {(change >= 0 ? "+" : "")}{change:F2}%";
            UIBuilderUtils.SetTextIfChanged(_priceChangeText, changeStr);
            if (_priceChangeText != null)
                _priceChangeText.color = change >= 0 ? _gainColor : _lossColor;

            if (_buyButton != null && _currencyManager != null)
                _buyButton.interactable = _currencyManager.CanAffordInvesting(_selectedDefinition.CurrentPrice);
        }

        private void RefreshDetailPanel()
        {
            if (_selectedDefinition == null)
            {
                // Placeholder state — no investment selected
                UIBuilderUtils.SetTextIfChanged(_selectedAssetText, "Select an investment");
                UIBuilderUtils.SetTextIfChanged(_priceText,         "Price: $---");
                UIBuilderUtils.SetTextIfChanged(_priceChangeText,   "Change: ---%");
                UIBuilderUtils.SetTextIfChanged(_sharesOwnedText,   "Owned: 0");
                UIBuilderUtils.SetTextIfChanged(_riskLevelText,     "Risk: ---");
                UIBuilderUtils.SetTextIfChanged(_averagePriceText,  "Avg Cost: $---");
                UIBuilderUtils.SetTextIfChanged(_descriptionText,   "Select a stock to see details.");
                if (_priceChangeText != null) _priceChangeText.color = Color.white;
                if (_riskLevelText   != null) _riskLevelText.color   = Color.white;
                if (_buyButton       != null) _buyButton.interactable = false;
                if (_sellButton      != null) _sellButton.gameObject.SetActive(false);
                return;
            }

            UIBuilderUtils.SetTextIfChanged(_selectedAssetText, _selectedDefinition.DisplayName);

            UpdatePriceDisplay(); // handles price, daily change, buy interactable

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

            var activeInv   = GetActiveInvestment(_selectedDefinition);
            int sharesOwned = activeInv != null ? activeInv.NumberOfShares : 0;

            UIBuilderUtils.SetTextIfChanged(_sharesOwnedText, $"Owned: {sharesOwned}");

            string avgPriceStr = sharesOwned > 0
                ? $"Avg Cost: ${activeInv.AveragePurchasePrice:F2}"
                : "Avg Cost: $---";
            UIBuilderUtils.SetTextIfChanged(_averagePriceText, avgPriceStr);

            UIBuilderUtils.SetTextIfChanged(_descriptionText, sharesOwned > 0
                ? "Tap Sell to remove 1 share."
                : "Tap Buy to purchase 1 share.");

            if (_sellButton != null)
                _sellButton.gameObject.SetActive(sharesOwned > 0);
        }

        // ═══════════════════════════════════════════════════════════════
        // TICK + BALANCE EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleTick(int tickNumber)
        {
            if (!IsVisible) return;

            _currentDayTick = tickNumber;

            // Snapshot unconditionally — Invest tab needs accurate snapshot when switching
            SnapshotPrices();

            if (_activeTabIndex == 0)
            {
                RefreshOverviewPanel(); // also updates _overviewGraph
            }
            else
            {
                UpdateDetailPriceTick();
                // Update stock graph while player is on Invest tab with a selection
                if (_selectedDefinition != null)
                    RefreshStockGraph();
            }
        }

        /// <summary>Lightweight per-tick update for Invest tab — price fields only.</summary>
        private void UpdateDetailPriceTick()
        {
            UpdatePriceDisplay();
        }

        private void HandleBalanceChanged(float newBalance, float delta)
        {
            if (!IsVisible) return;

            if (_activeTabIndex == 0)
                RefreshOverviewPanel();
            else if (_buyButton != null && _selectedDefinition != null)
                _buyButton.interactable = _currencyManager.CanAffordInvesting(_selectedDefinition.CurrentPrice);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private ActiveInvestment GetActiveInvestment(InvestmentDefinition def)
        {
            if (_investmentSystem == null) return null;
            return _investmentSystem.ActiveInvestments
                .FirstOrDefault(a => a.Definition == def);
        }

        /// <summary>
        /// Store current prices so the next tick can show 1-day change.
        /// </summary>
        private void SnapshotPrices()
        {
            if (_investmentSystem == null) return;
            foreach (var def in _investmentSystem.AvailableInvestments)
                _previousPrices[def] = def.CurrentPrice;
        }

        /// <summary>
        /// Percentage price change since previous snapshot (1-day change).
        /// </summary>
        private float GetPriceChangePercent(InvestmentDefinition def)
        {
            if (_previousPrices.TryGetValue(def, out float prev) && prev > 0)
                return (def.CurrentPrice - prev) / prev * 100f;

            // Fallback: change from base price
            if (def.BasePricePerShare > 0)
                return (def.CurrentPrice - def.BasePricePerShare) / def.BasePricePerShare * 100f;

            return 0f;
        }

        // ═══════════════════════════════════════════════════════════════
        // PATH-BASED FINDERS
        // ═══════════════════════════════════════════════════════════════

        private TextMeshProUGUI FindText(string path)
        {
            Transform t = transform.Find(path);
            if (t == null)
            {
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Text not found at path: {path}");
                return null;
            }
            return t.GetComponent<TextMeshProUGUI>();
        }

        private Button FindButton(string path)
        {
            Transform t = transform.Find(path);
            if (t == null)
            {
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Button not found at path: {path}");
                return null;
            }
            var btn = t.GetComponent<Button>();
            if (btn == null)
                UnityEngine.Debug.LogWarning($"[PortfolioPanel] Found '{path}' but it has no Button component");
            return btn;
        }
    }
}
