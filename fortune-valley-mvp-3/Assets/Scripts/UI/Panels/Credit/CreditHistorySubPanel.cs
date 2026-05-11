using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Credit History tab: shows past credit-related transactions.
    /// Reads from TransactionLog's passive event history.
    ///
    /// LEARNING DESIGN: Reviewing past payments, charges, and missed
    /// payments helps students see how their decisions affected their
    /// credit score and total cost of borrowing.
    /// </summary>
    public class CreditHistorySubPanel : SubPanelBase
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

        // Credit-related transaction types to display
        private static readonly TransactionType[] CreditTypes = new[]
        {
            TransactionType.LoanOriginated,
            TransactionType.LoanPayment,
            TransactionType.LoanPaidOff,
            TransactionType.LoanPaymentMissed,
            TransactionType.CreditCardCharge,
            TransactionType.CreditCardPayment
        };

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            // Re-render when new credit events occur
            GameEvents.OnLoanPaymentMade += HandleLoanPayment;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnCreditCardCharged += HandleCreditCardCharged;
            GameEvents.OnCreditCardPaymentCompleted += HandleCreditCardPayment;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnLoanPaymentMade -= HandleLoanPayment;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnCreditCardCharged -= HandleCreditCardCharged;
            GameEvents.OnCreditCardPaymentCompleted -= HandleCreditCardPayment;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleLoanPayment(ActiveLoan loan, float amount) => Refresh();
        private void HandleLoanOriginated(ActiveLoan loan) => Refresh();
        private void HandleLoanPaidOff(ActiveLoan loan) => Refresh();
        private void HandleCreditCardCharged(float amount) => Refresh();
        private void HandleCreditCardPayment(float amount) => Refresh();

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            ClearRows();

            if (_transactionLog == null || _transactionLog.History == null) return;
            if (_historyRowPrefab == null || _historyListContainer == null) return;

            var records = _transactionLog.History.GetByTypes(CreditTypes);

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

            // Populate row text fields
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);

            // Expected layout: Description, Amount
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
