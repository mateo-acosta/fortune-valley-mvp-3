using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Home tab: portfolio overview with summary stats and
    /// portfolio performance graph. Update-in-place (tick-driven).
    ///
    /// LEARNING DESIGN: Students see total portfolio value, lifetime gain,
    /// and risk profile at a glance. The dual-line graph shows total wealth
    /// vs net investment gain, making opportunity cost visible.
    /// </summary>
    public class InvestingHomeSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;
        [SerializeField] private CurrencyManager _currencyManager;
        [SerializeField] private PortfolioHistoryTracker _historyTracker;

        [Header("Graph")]
        [SerializeField] private TMP_FontAsset _labelFont;
        [SerializeField] private Transform _graphPlaceholder;

        [Header("Summary Text")]
        [SerializeField] private TextMeshProUGUI _balanceText;
        [SerializeField] private TextMeshProUGUI _investmentsValueText;
        [SerializeField] private TextMeshProUGUI _totalGainText;
        [SerializeField] private TextMeshProUGUI _portfolioLevelText;
        [SerializeField] private TextMeshProUGUI _currentHoldingsText;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        private LineGraphGraphic _overviewGraph;
        private bool _graphCreated;
        private int _currentDayTick;

        // Cached lists for graph window data (reused per tick, no allocation)
        private List<float> _wealthWindow = new List<float>();
        private List<float> _gainWindow = new List<float>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnInvestmentCreated += HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
            GameEvents.OnCheckingBalanceChanged += HandleBalanceChanged;

            EnsureGraph();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnInvestmentCreated -= HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnCheckingBalanceChanged -= HandleBalanceChanged;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleTick(int tickNumber)
        {
            _currentDayTick = tickNumber;
            Refresh();
        }

        private void HandleInvestmentEvent(ActiveInvestment inv) => Refresh();
        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout) => Refresh();
        private void HandleBalanceChanged(float balance, float delta) => Refresh();

        // ===============================================================
        // REFRESH (update-in-place, no destroy/rebuild)
        // ===============================================================

        protected override void Refresh()
        {
            if (_investmentSystem == null || _currencyManager == null) return;

            // Property reads only
            float balance = _currencyManager.InvestingBalance;
            float portfolioVal = _investmentSystem.TotalPortfolioValue;
            float totalGain = _investmentSystem.LifetimeTotalGain;
            var holdings = _investmentSystem.ActiveInvestments;

            UIBuilderUtils.SetTextIfChanged(_balanceText, $"Balance: ${balance:N0}");
            UIBuilderUtils.SetTextIfChanged(_investmentsValueText, $"Invested: ${portfolioVal:N0}");

            string gainStr = $"Total Gain: {(totalGain >= 0 ? "+" : "")}${totalGain:N0}";
            UIBuilderUtils.SetTextIfChanged(_totalGainText, gainStr);
            if (_totalGainText != null)
                _totalGainText.color = totalGain >= 0 ? _gainColor : _lossColor;

            UIBuilderUtils.SetTextIfChanged(_portfolioLevelText,
                $"Risk: {PortfolioPanelLogic.GetPortfolioRiskLabel(holdings)}");
            UIBuilderUtils.SetTextIfChanged(_currentHoldingsText,
                PortfolioPanelLogic.BuildHoldingsSummary(holdings));

            RefreshGraph();
        }

        private void RefreshGraph()
        {
            if (_historyTracker == null || _overviewGraph == null) return;

            // Single-line graph: portfolio market value over time.
            FillWindowCached(_historyTracker.PortfolioValueHistory, _wealthWindow);

            int startDay = _currentDayTick - (_wealthWindow.Count - 1);
            _overviewGraph.SetData(_wealthWindow, startDay);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void EnsureGraph()
        {
            if (_graphCreated || _graphPlaceholder == null) return;

            // Clear placeholder background
            var img = _graphPlaceholder.GetComponent<Image>();
            if (img != null) img.color = Color.clear;

            var go = new GameObject("Graph", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_graphPlaceholder, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(LineGraphGraphic.YLabelWidth - 8f, LineGraphGraphic.XLabelHeight + 2f);
            rt.offsetMax = Vector2.zero;

            _overviewGraph = go.AddComponent<LineGraphGraphic>();
            _overviewGraph.SetLabelFont(_labelFont);
            _overviewGraph.SetLineColor(_gainColor);
            _graphCreated = true;
        }

        /// <summary>
        /// Fill a cached list with the last 30 entries from source.
        /// Reuses the list to avoid per-tick allocation.
        /// </summary>
        private static void FillWindowCached(IReadOnlyList<float> source, List<float> target)
        {
            target.Clear();
            if (source == null || source.Count == 0) return;

            int windowSize = 30;
            int start = source.Count > windowSize ? source.Count - windowSize : 0;
            for (int i = start; i < source.Count; i++)
                target.Add(source[i]);
        }
    }
}
