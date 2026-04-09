using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Panels.Insurance
{
    /// <summary>
    /// Insurance History tab: shows past insurance transactions.
    /// Purchases, cancellations, and accident resolutions.
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

        [Header("History List")]
        [SerializeField] private Transform _historyListContainer;
        [SerializeField] private GameObject _historyRowPrefab;
        [SerializeField] private GameObject _emptyStateObject;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<GameObject> _rowInstances = new List<GameObject>();

        private static readonly TransactionType[] InsuranceTypes = new[]
        {
            TransactionType.InsurancePurchased,
            TransactionType.InsuranceCanceled,
            TransactionType.AccidentResolved
        };

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnInsurancePurchased += HandleInsuranceEvent;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnInsurancePurchased -= HandleInsuranceEvent;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleInsuranceEvent(string lotId, string policyId) => Refresh();
        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType type) => Refresh();
        private void HandleAccidentResolved(string lotId, string name, float damage, bool covered, float cost) => Refresh();

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            ClearRows();

            if (_transactionLog == null || _transactionLog.History == null) return;
            if (_historyRowPrefab == null || _historyListContainer == null) return;

            var records = _transactionLog.History.GetByTypes(InsuranceTypes);

            for (int i = 0; i < records.Count; i++)
            {
                SpawnHistoryRow(records[i]);
            }

            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(records.Count == 0);
        }

        private void SpawnHistoryRow(TransactionRecord record)
        {
            var row = Instantiate(_historyRowPrefab, _historyListContainer);
            _rowInstances.Add(row);

            var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);

            if (texts.Length > 0) texts[0].text = record.Description;
            if (texts.Length > 1) texts[1].text = record.Amount > 0f ? $"${record.Amount:N2}" : "";
        }

        private void ClearRows()
        {
            for (int i = 0; i < _rowInstances.Count; i++)
            {
                if (_rowInstances[i] != null)
                    Destroy(_rowInstances[i]);
            }
            _rowInstances.Clear();
        }
    }
}
