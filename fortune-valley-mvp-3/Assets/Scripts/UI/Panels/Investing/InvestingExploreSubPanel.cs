using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Investing Explore tab: browse all available investments as cards.
    /// Update-in-place (tick-driven, prices change per tick).
    ///
    /// LEARNING DESIGN: Students compare investments by risk level,
    /// current price, and price change, learning to evaluate opportunities.
    /// </summary>
    public class InvestingExploreSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("Card Grid")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardItemPrefab;

        [Header("Colors")]
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ===============================================================
        // STATE
        // ===============================================================

        private List<GameObject> _cardItems = new List<GameObject>();
        private int _lastInvestmentCount = -1;

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
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;

            base.OnDisable();
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
        // REFRESH (full rebuild)
        // ===============================================================

        protected override void Refresh()
        {
            ClearCards();

            if (_investmentSystem == null) return;
            if (_cardItemPrefab == null || _cardContainer == null) return;

            var investments = _investmentSystem.AvailableInvestments;
            _lastInvestmentCount = investments.Count;

            for (int i = 0; i < investments.Count; i++)
            {
                var go = Instantiate(_cardItemPrefab, _cardContainer);
                _cardItems.Add(go);
                PopulateCard(go, investments[i]);
                WireCardButton(go, investments[i]);
            }
        }

        // ===============================================================
        // UPDATE IN PLACE (per-tick price updates)
        // ===============================================================

        private void UpdateInPlace()
        {
            if (_investmentSystem == null) return;

            var investments = _investmentSystem.AvailableInvestments;

            if (investments.Count != _lastInvestmentCount)
            {
                Refresh();
                return;
            }

            for (int i = 0; i < _cardItems.Count && i < investments.Count; i++)
            {
                PopulateCard(_cardItems[i], investments[i]);
            }
        }

        private void PopulateCard(GameObject go, InvestmentDefinition def)
        {
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>(true);

            // Expected layout: Name, Price, Change%, RiskLevel
            if (texts.Length > 0) texts[0].text = def.DisplayName;
            if (texts.Length > 1) texts[1].text = $"${def.CurrentPrice:F2}";

            if (texts.Length > 2)
            {
                float change = GetPriceChangePercent(def);
                texts[2].text = $"{(change >= 0 ? "+" : "")}{change:F1}%";
                texts[2].color = change >= 0 ? _gainColor : _lossColor;
            }

            if (texts.Length > 3) texts[3].text = $"{def.RiskLevel} Risk";
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
            for (int i = 0; i < _cardItems.Count; i++)
            {
                if (_cardItems[i] != null)
                    Destroy(_cardItems[i]);
            }
            _cardItems.Clear();
            _lastInvestmentCount = -1;
        }
    }
}
