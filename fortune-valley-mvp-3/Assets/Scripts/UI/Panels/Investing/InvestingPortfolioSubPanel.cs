using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Portfolio tab: holdings list with click-to-select.
    /// This script manages the list ONLY. The detail view is handled
    /// by PortfolioDetailView via GameEvents (decoupled).
    ///
    /// Supports category and industry filtering via base class.
    /// Clicking a holding fires OnPortfolioHoldingSelected.
    /// Auto-selects the first holding on load.
    ///
    /// LEARNING DESIGN: Students see their portfolio at a glance and
    /// can drill into any holding for detailed performance metrics.
    /// </summary>
    public class InvestingPortfolioSubPanel : InvestingFilterableSubPanelBase
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
        [SerializeField] private GameObject _noFilterResultsObject;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        // Cached views for update-in-place (zero per-tick GetComponent)
        private List<HoldingListItemView> _holdingViews = new List<HoldingListItemView>();

        // Cached filtered list to avoid re-filtering on each tick
        private List<ActiveInvestment> _filteredHoldings = new List<ActiveInvestment>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnInvestmentCreated += HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;

            base.OnEnable(); // subscribes filters, calls Refresh()
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnInvestmentCreated -= HandleInvestmentEvent;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;

            base.OnDisable(); // unsubscribes filters
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleTick(int tickNumber) => UpdateInPlace();

        private void HandleInvestmentEvent(ActiveInvestment inv) => Refresh();
        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout) => Refresh();

        // ===============================================================
        // REFRESH (full rebuild with current filters)
        // ===============================================================

        protected override void Refresh()
        {
            ClearItems();

            if (_investmentSystem == null) return;

            var allHoldings = _investmentSystem.ActiveInvestments;

            _filteredHoldings = InvestmentFilterLogic.FilterActiveInvestments(
                allHoldings,
                CurrentCategoryFilter,
                CurrentIndustryFilter);

            if (_holdingItemPrefab == null || _holdingsContainer == null) return;

            for (int i = 0; i < _filteredHoldings.Count; i++)
            {
                var go = Instantiate(_holdingItemPrefab, _holdingsContainer);
                var view = go.GetComponent<HoldingListItemView>();
                _holdingViews.Add(view);

                PopulateHoldingItem(view, _filteredHoldings[i]);
                WireHoldingClick(go, _filteredHoldings[i]);
            }

            // Empty state: no holdings at all
            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(allHoldings.Count == 0);

            // Filter-specific empty state: has holdings but filter excluded all
            if (_noFilterResultsObject != null)
                _noFilterResultsObject.SetActive(
                    allHoldings.Count > 0 && _filteredHoldings.Count == 0);

            // Auto-select first holding so detail view populates immediately
            if (_filteredHoldings.Count > 0)
                GameEvents.RaisePortfolioHoldingSelected(_filteredHoldings[0]);
        }

        // ===============================================================
        // UPDATE IN PLACE (per-tick price changes)
        // ===============================================================

        private void UpdateInPlace()
        {
            if (_investmentSystem == null) return;

            var freshFiltered = InvestmentFilterLogic.FilterActiveInvestments(
                _investmentSystem.ActiveInvestments,
                CurrentCategoryFilter,
                CurrentIndustryFilter);

            // If filtered count changed, do a full rebuild
            if (freshFiltered.Count != _holdingViews.Count)
            {
                Refresh();
                return;
            }

            // Update existing items with new price data
            for (int i = 0; i < _holdingViews.Count && i < freshFiltered.Count; i++)
            {
                PopulateHoldingItem(_holdingViews[i], freshFiltered[i]);
            }

            _filteredHoldings = freshFiltered;
        }

        private void PopulateHoldingItem(HoldingListItemView view, ActiveInvestment inv)
        {
            if (view == null || inv == null || inv.Definition == null) return;

            view.SetName($"{inv.Definition.DisplayName}\n{inv.NumberOfShares} shares");

            float gain = inv.TotalGain;
            view.SetValue(
                $"${inv.CurrentValue:N0}",
                gain >= 0 ? _gainColor : _lossColor);
        }

        private void WireHoldingClick(GameObject go, ActiveInvestment holding)
        {
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.GetComponentInChildren<Button>(true);

            // Add Button + Image if the prefab doesn't have one (needed for click raycasting)
            if (btn == null)
            {
                var img = go.GetComponent<UnityEngine.UI.Image>();
                if (img == null)
                {
                    img = go.AddComponent<UnityEngine.UI.Image>();
                    img.color = new Color(1f, 1f, 1f, 0f); // transparent
                }
                btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
            }

            var capturedHolding = holding;
            btn.onClick.AddListener(() =>
                GameEvents.RaisePortfolioHoldingSelected(capturedHolding));
        }

        private void ClearItems()
        {
            for (int i = 0; i < _holdingViews.Count; i++)
            {
                if (_holdingViews[i] != null)
                    Destroy(_holdingViews[i].gameObject);
            }
            _holdingViews.Clear();
            _filteredHoldings.Clear();
        }
    }
}
