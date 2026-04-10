using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Components;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI.Panels.Insurance
{
    /// <summary>
    /// Insurance History tab: shows past insurance transactions.
    /// Purchases, cancellations, accident resolutions, and premium charges.
    ///
    /// LEARNING DESIGN: Students review how accidents impacted them
    /// differently based on coverage decisions, reinforcing the
    /// value of insurance as risk management.
    /// </summary>
    public class InsuranceHistorySubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private TransactionLog _transactionLog;
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private InsuranceSystem _insuranceSystem;
        [SerializeField] private UIManager _uiManager;

        [Header("Filter Row 1 - Transaction Type")]
        [SerializeField] private FilterRowController _transactionTypeFilter;
        [SerializeField] private TransactionType[] _transactionTypeMapping;

        [Header("Filter Row 2 - Lot Dropdown")]
        [SerializeField] private TMP_Dropdown _lotDropdown;

        [Header("Summary Stats")]
        [SerializeField] private TMP_Text _accidentCostText;
        [SerializeField] private TMP_Text _premiumsPaidText;
        [SerializeField] private TMP_Text _accidentCountText;

        [Header("Card Grid")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private GameObject _historyCardPrefab;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateObject;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<InsuranceHistoryCardView> _cardViews = new List<InsuranceHistoryCardView>();
        private List<string> _lotIdMapping;

        private static readonly TransactionType[] InsuranceTransactionTypes = new[]
        {
            TransactionType.InsurancePurchased,
            TransactionType.InsuranceCanceled,
            TransactionType.AccidentResolved,
            TransactionType.PremiumCharged
        };

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceEvent;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
            GameEvents.OnInsurancePremiumCharged += HandlePremiumCharged;

            if (_transactionTypeFilter != null)
                _transactionTypeFilter.OnSelectionChanged += HandleFilterChanged;
            if (_lotDropdown != null)
                _lotDropdown.onValueChanged.AddListener(HandleLotFilterChanged);

            base.OnEnable(); // calls Refresh()
        }

        protected override void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceEvent;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
            GameEvents.OnInsurancePremiumCharged -= HandlePremiumCharged;

            if (_transactionTypeFilter != null)
                _transactionTypeFilter.OnSelectionChanged -= HandleFilterChanged;
            if (_lotDropdown != null)
                _lotDropdown.onValueChanged.RemoveListener(HandleLotFilterChanged);

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceEvent(string lotId, string policyId) => Refresh();
        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType type) => Refresh();
        private void HandleAccidentResolved(string lotId, string name, float damage, bool covered, float cost) => Refresh();
        private void HandlePremiumCharged(string lotId, string policyId, float amount) => Refresh();
        private void HandleFilterChanged(int index) => Refresh();
        private void HandleLotFilterChanged(int index) => Refresh();

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            if (_transactionLog == null || _transactionLog.History == null) return;

            // Build lot dropdown (property reads)
            if (_cityManager != null)
            {
                InsuranceFilterLogic.PopulateLotDropdown(
                    _lotDropdown, _cityManager.AllLots, _cityManager.LotOwnership,
                    out _lotIdMapping);
            }

            // Get all insurance transactions (property read + method on returned object)
            var allRecords = _transactionLog.History.GetByTypes(InsuranceTransactionTypes);

            // Read filter values
            TransactionType? typeFilter = MapTransactionTypeIndex(
                _transactionTypeFilter != null ? _transactionTypeFilter.SelectedIndex : 0);
            string entityIdFilter = GetSelectedLotId();

            // Filter
            var filtered = InsuranceFilterLogic.FilterInsuranceTransactions(
                allRecords, typeFilter, entityIdFilter);

            // Summary stats (computed from ALL insurance records, not filtered)
            InsuranceSummaryCalculator.CalculateHistorySummary(
                allRecords,
                out float totalAccidentCosts,
                out float totalPremiumsPaid,
                out int accidentCount);

            if (_accidentCostText != null)
                _accidentCostText.text = $"Accident Costs: ${totalAccidentCosts:N2}";
            if (_premiumsPaidText != null)
                _premiumsPaidText.text = $"Premiums Paid: ${totalPremiumsPaid:N2}";
            if (_accidentCountText != null)
                _accidentCountText.text = $"Accidents: {accidentCount}";

            // Rebuild card grid
            ClearCards();

            if (_historyCardPrefab != null && _cardContainer != null)
            {
                var allLots = _cityManager != null ? _cityManager.AllLots : null;

                for (int i = 0; i < filtered.Count; i++)
                {
                    var record = filtered[i];
                    var card = Instantiate(_historyCardPrefab, _cardContainer);
                    var view = card.GetComponent<InsuranceHistoryCardView>();

                    if (view != null)
                    {
                        var details = InsuranceDetailFormatter.FormatTransaction(record);
                        string lotName = record.EntityId != null
                            ? LotDisplayHelper.GetDisplayName(allLots, record.EntityId)
                            : "N/A";

                        view.SetTypeLabel(details.TypeLabel);
                        view.SetDate($"Tick {record.Tick}");
                        view.SetLot(lotName);
                        view.SetAmount(details.Amount);
                        view.SetDescription(record.Description);

                        var capturedRecord = record;
                        view.SetDetailsCallback(() => ShowTransactionDetail(capturedRecord));

                        _cardViews.Add(view);
                    }
                }
            }

            // Empty state
            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(filtered.Count == 0);
        }

        // ===============================================================
        // DETAIL POPUP
        // ===============================================================

        private void ShowTransactionDetail(TransactionRecord record)
        {
            if (_uiManager == null) return;

            var popup = _uiManager.InsuranceDetailPopup as InsuranceDetailPopup;
            if (popup == null) return;

            var configs = _insuranceSystem != null ? _insuranceSystem.AvailablePolicies : null;
            popup.ConfigureTransaction(record, configs);
            _uiManager.ShowPopup(popup);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private TransactionType? MapTransactionTypeIndex(int index)
        {
            if (index <= 0 || _transactionTypeMapping == null) return null;
            int arrayIndex = index - 1;
            if (arrayIndex >= _transactionTypeMapping.Length) return null;
            return _transactionTypeMapping[arrayIndex];
        }

        private string GetSelectedLotId()
        {
            if (_lotDropdown == null || _lotIdMapping == null) return null;
            int idx = _lotDropdown.value;
            if (idx < 0 || idx >= _lotIdMapping.Count) return null;
            return _lotIdMapping[idx];
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
