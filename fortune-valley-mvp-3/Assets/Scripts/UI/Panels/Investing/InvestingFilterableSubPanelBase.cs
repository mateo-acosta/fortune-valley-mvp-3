using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Investing
{
    /// <summary>
    /// Intermediate base for investing sub-panels that support
    /// category + industry filtering and per-category card backgrounds.
    /// One inheritance level above SubPanelBase (permitted by architecture).
    ///
    /// Subclasses implement Refresh() and call the protected helpers
    /// for mapping, sprite lookup, and current filter state.
    /// </summary>
    public abstract class InvestingFilterableSubPanelBase : SubPanelBase
    {
        // ===============================================================
        // FILTER REFERENCES
        // ===============================================================

        [Header("Filters")]
        [SerializeField] private FilterRowController _categoryFilter;
        [SerializeField] private FilterRowController _industryFilter;
        [SerializeField] private GameObject _industryFilterRow;

        [Header("Filter Mappings (button index 0 = All, then one entry per remaining button)")]
        [Tooltip("Order must match category filter buttons. E.g. [Stock, ETF, Bond, TBill]")]
        [SerializeField] private InvestmentCategory[] _categoryMapping;

        [Tooltip("Order must match industry filter buttons. E.g. [Technology, Financials, Energy, ...]")]
        [SerializeField] private Industry[] _industryMapping;

        [Header("Card Backgrounds (one per category)")]
        [SerializeField] private Sprite _stockBackground;
        [SerializeField] private Sprite _etfBackground;
        [SerializeField] private Sprite _bondBackground;
        [SerializeField] private Sprite _tbillBackground;

        // ===============================================================
        // STATE
        // ===============================================================

        // Prevents double-fire when auto-narrowing category on industry select
        private bool _suppressFilterEvents;

        // Cached filter values updated on each filter change
        private InvestmentCategory? _currentCategoryFilter;
        private Industry? _currentIndustryFilter;

        // ===============================================================
        // PROTECTED ACCESSORS (for subclasses)
        // ===============================================================

        protected InvestmentCategory? CurrentCategoryFilter => _currentCategoryFilter;
        protected Industry? CurrentIndustryFilter => _currentIndustryFilter;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            if (_categoryFilter != null)
                _categoryFilter.OnSelectionChanged += HandleFilterChanged;
            if (_industryFilter != null)
                _industryFilter.OnSelectionChanged += HandleFilterChanged;

            // Initialize filter state from current controller positions
            _currentCategoryFilter = MapCategoryIndex(
                _categoryFilter != null ? _categoryFilter.SelectedIndex : 0);
            _currentIndustryFilter = MapIndustryIndex(
                _industryFilter != null ? _industryFilter.SelectedIndex : 0);
            UpdateIndustryRowVisibility();

            base.OnEnable(); // calls Refresh()
        }

        protected override void OnDisable()
        {
            if (_categoryFilter != null)
                _categoryFilter.OnSelectionChanged -= HandleFilterChanged;
            if (_industryFilter != null)
                _industryFilter.OnSelectionChanged -= HandleFilterChanged;

            base.OnDisable();
        }

        // ===============================================================
        // FILTER HANDLING
        // ===============================================================

        private void HandleFilterChanged(int index)
        {
            if (_suppressFilterEvents) return;

            _currentCategoryFilter = MapCategoryIndex(
                _categoryFilter != null ? _categoryFilter.SelectedIndex : 0);
            _currentIndustryFilter = MapIndustryIndex(
                _industryFilter != null ? _industryFilter.SelectedIndex : 0);

            // Auto-narrow: if industry is set and category is not Stock,
            // force category to Stock for intuitive UX
            if (_currentIndustryFilter.HasValue
                && _currentCategoryFilter != InvestmentCategory.Stock)
            {
                int stockButtonIndex = FindCategoryButtonIndex(InvestmentCategory.Stock);
                if (stockButtonIndex >= 0)
                {
                    _suppressFilterEvents = true;
                    if (_categoryFilter != null)
                        _categoryFilter.Select(stockButtonIndex);
                    _suppressFilterEvents = false;

                    _currentCategoryFilter = InvestmentCategory.Stock;
                }
            }

            UpdateIndustryRowVisibility();
            Refresh();
        }

        private void UpdateIndustryRowVisibility()
        {
            if (_industryFilterRow == null) return;

            // Show industry row only when category is All or Stock
            bool showIndustry = !_currentCategoryFilter.HasValue
                || _currentCategoryFilter.Value == InvestmentCategory.Stock;

            _industryFilterRow.SetActive(showIndustry);

            // Reset industry filter when hiding the row
            if (!showIndustry && _industryFilter != null)
            {
                _industryFilter.ResetToAll();
                _currentIndustryFilter = null;
            }
        }

        // ===============================================================
        // INDEX-TO-DOMAIN MAPPING (serialized, Inspector-visible)
        // ===============================================================

        /// <summary>
        /// Map category button index to domain enum using the serialized array.
        /// Index 0 = "All" (returns null). Index 1+ looks up _categoryMapping[index - 1].
        /// </summary>
        protected InvestmentCategory? MapCategoryIndex(int index)
        {
            if (index <= 0 || _categoryMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _categoryMapping.Length) return null;
            return _categoryMapping[arrayIndex];
        }

        /// <summary>
        /// Map industry button index to domain enum using the serialized array.
        /// Index 0 = "All" (returns null). Index 1+ looks up _industryMapping[index - 1].
        /// </summary>
        protected Industry? MapIndustryIndex(int index)
        {
            if (index <= 0 || _industryMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _industryMapping.Length) return null;
            return _industryMapping[arrayIndex];
        }

        /// <summary>
        /// Find the button index for a given category in the serialized mapping.
        /// Returns the button index (1-based, since 0 = All), or -1 if not found.
        /// </summary>
        private int FindCategoryButtonIndex(InvestmentCategory category)
        {
            if (_categoryMapping == null) return -1;
            for (int i = 0; i < _categoryMapping.Length; i++)
            {
                if (_categoryMapping[i] == category)
                    return i + 1; // +1 because button index 0 is "All"
            }
            return -1;
        }

        // ===============================================================
        // BACKGROUND SPRITE LOOKUP
        // ===============================================================

        /// <summary>
        /// Get the background sprite for a given investment category.
        /// Returns null if no sprite is assigned for the category.
        /// </summary>
        protected Sprite GetBackgroundSprite(InvestmentCategory category)
        {
            return category switch
            {
                InvestmentCategory.Stock => _stockBackground,
                InvestmentCategory.ETF => _etfBackground,
                InvestmentCategory.Bond => _bondBackground,
                InvestmentCategory.TBill => _tbillBackground,
                _ => null
            };
        }
    }
}
