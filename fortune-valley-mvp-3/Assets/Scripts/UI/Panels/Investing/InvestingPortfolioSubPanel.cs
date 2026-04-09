using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Portfolio tab: detailed per-holding stats.
    /// Update-in-place (tick-driven, prices change per tick).
    ///
    /// LEARNING DESIGN: Students see per-stock performance broken down:
    /// shares owned, avg cost, current value, gain/loss, and % return.
    /// This teaches portfolio diversification and position tracking.
    /// </summary>
    public class InvestingPortfolioSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("Holdings List")]
        [SerializeField] private Transform _holdingsContainer;
        [SerializeField] private GameObject _holdingItemPrefab;
        [SerializeField] private GameObject _emptyStateObject;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        // Cached items for update-in-place
        private List<GameObject> _holdingItems = new List<GameObject>();
        private int _lastHoldingCount = -1;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnInvestmentCreated += HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnInvestmentCreated -= HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleTick(int tickNumber) => UpdateInPlace();

        private void HandleInvestmentEvent(ActiveInvestment inv) => Refresh();
        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout) => Refresh();

        // ===============================================================
        // REFRESH (full rebuild when holdings count changes)
        // ===============================================================

        protected override void Refresh()
        {
            ClearItems();

            if (_investmentSystem == null) return;

            var holdings = _investmentSystem.ActiveInvestments;
            _lastHoldingCount = holdings.Count;

            if (_holdingItemPrefab == null || _holdingsContainer == null) return;

            for (int i = 0; i < holdings.Count; i++)
            {
                var go = Instantiate(_holdingItemPrefab, _holdingsContainer);
                _holdingItems.Add(go);
                PopulateHoldingItem(go, holdings[i]);
            }

            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(holdings.Count == 0);
        }

        // ===============================================================
        // UPDATE IN PLACE (per-tick price changes)
        // ===============================================================

        private void UpdateInPlace()
        {
            if (_investmentSystem == null) return;

            var holdings = _investmentSystem.ActiveInvestments;

            // If holdings count changed, do a full rebuild
            if (holdings.Count != _lastHoldingCount)
            {
                Refresh();
                return;
            }

            // Update existing items with new price data
            for (int i = 0; i < _holdingItems.Count && i < holdings.Count; i++)
            {
                PopulateHoldingItem(_holdingItems[i], holdings[i]);
            }
        }

        private void PopulateHoldingItem(GameObject go, ActiveInvestment inv)
        {
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);

            // Expected layout: Name, Shares, AvgCost, CurrentValue, Gain, Return%
            if (texts.Length > 0) texts[0].text = inv.Definition.DisplayName;
            if (texts.Length > 1) texts[1].text = $"{inv.NumberOfShares} shares";
            if (texts.Length > 2) texts[2].text = $"Avg: ${inv.AveragePurchasePrice:F2}";
            if (texts.Length > 3) texts[3].text = $"Value: ${inv.CurrentValue:N0}";

            if (texts.Length > 4)
            {
                float gain = inv.TotalGain;
                texts[4].text = $"{(gain >= 0 ? "+" : "")}${gain:N0}";
                texts[4].color = gain >= 0 ? _gainColor : _lossColor;
            }

            if (texts.Length > 5)
            {
                float returnPct = inv.TotalCostBasis > 0
                    ? (inv.TotalGain / inv.TotalCostBasis) * 100f
                    : 0f;
                texts[5].text = $"{(returnPct >= 0 ? "+" : "")}{returnPct:F1}%";
                texts[5].color = returnPct >= 0 ? _gainColor : _lossColor;
            }
        }

        private void ClearItems()
        {
            for (int i = 0; i < _holdingItems.Count; i++)
            {
                if (_holdingItems[i] != null)
                    Destroy(_holdingItems[i]);
            }
            _holdingItems.Clear();
            _lastHoldingCount = -1;
        }
    }
}
