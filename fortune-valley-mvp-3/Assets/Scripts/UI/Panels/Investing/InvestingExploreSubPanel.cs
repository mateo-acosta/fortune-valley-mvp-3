using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Explore tab: browse available investments as cards.
    /// Supports category and industry filtering via base class.
    /// Update-in-place (tick-driven, prices change per tick).
    ///
    /// LEARNING DESIGN: Students compare investments by risk level,
    /// current price, and price change, learning to evaluate opportunities.
    /// </summary>
    public class InvestingExploreSubPanel : InvestingFilterableSubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("Card Grid")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardItemPrefab;

        [Header("Navigation")]
        [Tooltip("The sidebar that controls tab switching for the investing panel")]
        [SerializeField] private SidebarController _sidebarController;

        [Tooltip("Index of the Trade tab in the sidebar (0-based)")]
        [SerializeField] private int _tradeTabIndex;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        // Cached card views for update-in-place (zero per-tick GetComponent)
        private List<CardItemView> _cardViews = new List<CardItemView>();

        // Cached filtered list to avoid re-filtering on each tick
        private List<InvestmentDefinition> _filteredInvestments
            = new List<InvestmentDefinition>();

        // Track previous prices for daily change display
        private Dictionary<InvestmentDefinition, float> _previousPrices
            = new Dictionary<InvestmentDefinition, float>();

        /// <summary>
        /// Currently selected investment definition.
        /// InvestingTradeSubPanel reads this to know what to display.
        /// </summary>
        public InvestmentDefinition SelectedDefinition { get; private set; }

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnTick += HandleTick;

            SnapshotPrices();
            base.OnEnable(); // subscribes filters, calls Refresh()
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;

            base.OnDisable(); // unsubscribes filters
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleTick(int tickNumber)
        {
            SnapshotPrices();
            UpdateInPlace();
        }

        // ===============================================================
        // REFRESH (full rebuild with current filters)
        // ===============================================================

        protected override void Refresh()
        {
            ClearCards();

            if (_investmentSystem == null) return;
            if (_cardItemPrefab == null || _cardContainer == null) return;

            _filteredInvestments = InvestmentFilterLogic.FilterDefinitions(
                _investmentSystem.AvailableInvestments,
                CurrentCategoryFilter,
                CurrentIndustryFilter);

            for (int i = 0; i < _filteredInvestments.Count; i++)
            {
                var go = Instantiate(_cardItemPrefab, _cardContainer);
                var view = go.GetComponent<CardItemView>();
                _cardViews.Add(view);

                PopulateCard(view, _filteredInvestments[i]);
                WireCardButton(go, _filteredInvestments[i]);
            }
        }

        // ===============================================================
        // UPDATE IN PLACE (per-tick price updates)
        // ===============================================================

        private void UpdateInPlace()
        {
            if (_investmentSystem == null) return;

            // Re-filter to check if the count changed (e.g., new investment added)
            var freshFiltered = InvestmentFilterLogic.FilterDefinitions(
                _investmentSystem.AvailableInvestments,
                CurrentCategoryFilter,
                CurrentIndustryFilter);

            if (freshFiltered.Count != _cardViews.Count)
            {
                Refresh();
                return;
            }

            for (int i = 0; i < _cardViews.Count && i < freshFiltered.Count; i++)
            {
                PopulateCard(_cardViews[i], freshFiltered[i]);
            }

            _filteredInvestments = freshFiltered;
        }

        private void PopulateCard(CardItemView view, InvestmentDefinition def)
        {
            if (view == null) return;

            view.SetName(def.DisplayName);
            view.SetPrice($"${def.CurrentPrice:F2}");

            float change = GetPriceChangePercent(def);
            view.SetChange(
                $"{(change >= 0 ? "+" : "")}{change:F1}%",
                change >= 0 ? _gainColor : _lossColor);

            view.SetRisk($"{def.RiskLevel} Risk");
            view.SetBackground(GetBackgroundSprite(def.Category));
        }

        private void WireCardButton(GameObject go, InvestmentDefinition def)
        {
            var btn = go.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn == null) return;

            var capturedDef = def;
            btn.onClick.AddListener(() => OnCardSelected(capturedDef));
        }

        private void OnCardSelected(InvestmentDefinition def)
        {
            SelectedDefinition = def;
            GameEvents.RaiseTradeRequested(def);

            // Navigate to the Trade tab so the user can buy/sell immediately
            if (_sidebarController != null)
                _sidebarController.SwitchTo(_tradeTabIndex);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

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

        private void ClearCards()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null)
                    Destroy(_cardViews[i].gameObject);
            }
            _cardViews.Clear();
            _filteredInvestments.Clear();
        }
    }
}
