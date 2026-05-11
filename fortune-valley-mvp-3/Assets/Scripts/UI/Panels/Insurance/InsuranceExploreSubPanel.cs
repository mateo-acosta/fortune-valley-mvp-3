using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels.Insurance
{
    /// <summary>
    /// Insurance Explore tab: browse available insurance policy products.
    /// Cards show premium, deductible, coverage. Greyed-out when fully covered.
    /// Buy button opens LotSelectionPopup for eligible lots.
    ///
    /// LEARNING DESIGN: Students compare policy costs vs coverage amounts,
    /// learning to evaluate insurance as a risk management tool.
    /// </summary>
    public class InsuranceExploreSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private InsuranceSystem _insuranceSystem;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private UIManager _uiManager;

        [Header("Filter Row 1 - Policy Type")]
        [SerializeField] private FilterRowController _policyTypeFilter;
        [SerializeField] private InsurancePolicyType[] _policyTypeMapping;

        [Header("Filter Row 2 - Coverage Status")]
        [SerializeField] private FilterRowController _coverageStatusFilter;
        [SerializeField] private InsuranceCoverageStatus[] _coverageStatusMapping;

        [Header("Card Backgrounds")]
        [SerializeField] private Sprite _generalBackground;
        [SerializeField] private Sprite _nonGeneralBackground;

        [Header("Card Grid")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _cardPrefab;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateObject;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<InsuranceCardItemView> _cardViews = new List<InsuranceCardItemView>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnLotPurchased += HandleLotPurchased;

            if (_policyTypeFilter != null)
                _policyTypeFilter.OnSelectionChanged += HandleFilterChanged;
            if (_coverageStatusFilter != null)
                _coverageStatusFilter.OnSelectionChanged += HandleFilterChanged;

            base.OnEnable(); // calls Refresh()
        }

        protected override void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceChanged;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnLotPurchased -= HandleLotPurchased;

            if (_policyTypeFilter != null)
                _policyTypeFilter.OnSelectionChanged -= HandleFilterChanged;
            if (_coverageStatusFilter != null)
                _coverageStatusFilter.OnSelectionChanged -= HandleFilterChanged;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceChanged(string lotId, string policyId) => Refresh();
        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType type) => Refresh();
        private void HandleFilterChanged(int index) => Refresh();

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner == Owner.Player) Refresh();
        }

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            if (_insuranceSystem == null || _cityManager == null) return;

            // Read filter values
            InsurancePolicyType? policyTypeFilter = MapPolicyTypeIndex(
                _policyTypeFilter != null ? _policyTypeFilter.SelectedIndex : 0);
            InsuranceCoverageStatus? coverageFilter = MapCoverageStatusIndex(
                _coverageStatusFilter != null ? _coverageStatusFilter.SelectedIndex : 0);

            // Build owned lot list and coverage map (property reads only)
            var ownedLotIds = BuildOwnedLotIds();
            var allPolicies = _insuranceSystem.Portfolio != null
                ? _insuranceSystem.Portfolio.AllPolicies : null;
            var coverageMap = InsuranceFilterLogic.BuildCoverageMap(allPolicies);

            // Filter
            var filtered = InsuranceFilterLogic.FilterPolicyConfigs(
                _insuranceSystem.AvailablePolicies,
                policyTypeFilter, coverageFilter, coverageMap, ownedLotIds);

            // Rebuild card grid
            ClearCards();

            if (_cardPrefab != null && _cardContainer != null)
            {
                for (int i = 0; i < filtered.Count; i++)
                {
                    var result = filtered[i];
                    var config = result.Config;

                    var card = Instantiate(_cardPrefab, _cardContainer);
                    var view = card.GetComponent<InsuranceCardItemView>();

                    if (view != null)
                    {
                        view.SetName(config.DisplayName);
                        view.SetType(config.PolicyType.ToString());
                        view.SetPremium($"${config.MonthlyPremium:N2}/mo");
                        view.SetDetail($"Deductible: ${config.Deductible:N2}");
                        view.SetBackground(InsuranceFilterLogic.GetBackgroundSprite(
                            config.PolicyType, _generalBackground, _nonGeneralBackground));

                        if (result.IsFullyCovered)
                        {
                            view.SetStatus("Fully Covered");
                            view.SetActionLabel("Owned");
                            view.SetGreyedOut(true);
                        }
                        else
                        {
                            view.SetStatus($"Coverage: {config.CoveragePercent:P0}");
                            view.SetActionLabel("Buy");
                            view.SetGreyedOut(false);

                            var capturedConfig = config;
                            view.SetActionCallback(() => ShowLotSelection(capturedConfig, coverageMap));
                        }

                        _cardViews.Add(view);
                    }
                }
            }

            // Empty state
            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(filtered.Count == 0);
        }

        // ===============================================================
        // LOT SELECTION POPUP
        // ===============================================================

        private void ShowLotSelection(
            InsurancePolicyConfig config,
            Dictionary<string, HashSet<InsurancePolicyType>> coverageMap)
        {
            if (_uiManager == null || _cityManager == null) return;

            var popup = _uiManager.InsuranceLotSelectionPopup as LotSelectionPopup;
            if (popup == null) return;

            // Build eligible lots (property reads, filtering in local scope)
            var eligibleLots = InsuranceFilterLogic.BuildEligibleLots(
                _cityManager.AllLots, _cityManager.LotOwnership,
                config.PolicyType, coverageMap);

            popup.Configure(config.PolicyId, config.DisplayName, eligibleLots);
            _uiManager.ShowPopup(popup);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private InsurancePolicyType? MapPolicyTypeIndex(int index)
        {
            if (index <= 0 || _policyTypeMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _policyTypeMapping.Length) return null;
            return _policyTypeMapping[arrayIndex];
        }

        private InsuranceCoverageStatus? MapCoverageStatusIndex(int index)
        {
            if (index <= 0 || _coverageStatusMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _coverageStatusMapping.Length) return null;
            return _coverageStatusMapping[arrayIndex];
        }

        private List<string> BuildOwnedLotIds()
        {
            var result = new List<string>();
            var allLots = _cityManager.AllLots;
            var ownership = _cityManager.LotOwnership;

            if (allLots == null || ownership == null) return result;

            for (int i = 0; i < allLots.Count; i++)
            {
                var lot = allLots[i];
                if (ownership.TryGetValue(lot.LotId, out var owner) && owner == Owner.Player)
                    result.Add(lot.LotId);
            }

            return result;
        }

        private void ClearCards()
        {
            for (int i = 0; i < _cardViews.Count; i++)
            {
                if (_cardViews[i] != null)
                    Destroy(_cardViews[i].gameObject);
            }
            _cardViews.Clear();
        }
    }
}
