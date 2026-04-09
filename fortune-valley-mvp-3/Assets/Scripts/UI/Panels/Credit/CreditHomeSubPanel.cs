using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Credit Home tab: active loans list, credit card balance,
    /// credit score, and summary stats.
    ///
    /// LEARNING DESIGN: Seeing all debt obligations in one place
    /// helps students understand total exposure and monthly commitments.
    /// </summary>
    public class CreditHomeSubPanel : SubPanelBase
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CreditCardSystem _creditCardSystem;
        [SerializeField] private CityManager _cityManager;

        [Header("Loan List")]
        [SerializeField] private Transform _loanListContainer;
        [SerializeField] private LoanListItem _loanItemPrefab;
        [SerializeField] private GameObject _emptyStateObject;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _totalDebtText;
        [SerializeField] private TextMeshProUGUI _totalMonthlyText;
        [SerializeField] private TextMeshProUGUI _creditScoreText;
        [SerializeField] private TextMeshProUGUI _creditBalanceText;
        [SerializeField] private TextMeshProUGUI _availableCreditText;
        [SerializeField] private TextMeshProUGUI _utilizationText;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<LoanListItem> _listItems = new List<LoanListItem>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        protected override void OnEnable()
        {
            GameEvents.OnLoanOriginated += HandleLoanEvent;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentEvent;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOffEvent;
            GameEvents.OnCreditScoreChanged += HandleCreditScoreChanged;
            GameEvents.OnCreditCardBalanceChanged += HandleCreditCardBalanceChanged;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            GameEvents.OnLoanOriginated -= HandleLoanEvent;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentEvent;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOffEvent;
            GameEvents.OnCreditScoreChanged -= HandleCreditScoreChanged;
            GameEvents.OnCreditCardBalanceChanged -= HandleCreditCardBalanceChanged;

            base.OnDisable();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleLoanEvent(ActiveLoan loan) => Refresh();
        private void HandleLoanPaymentEvent(ActiveLoan loan, float amount) => Refresh();
        private void HandleLoanPaidOffEvent(ActiveLoan loan) => Refresh();
        private void HandleCreditScoreChanged(int newScore) => Refresh();
        private void HandleCreditCardBalanceChanged(float balance, float delta) => Refresh();

        // ===============================================================
        // REFRESH
        // ===============================================================

        protected override void Refresh()
        {
            RefreshLoanList();
            RefreshSummary();
        }

        private void RefreshLoanList()
        {
            // Clear existing items
            for (int i = 0; i < _listItems.Count; i++)
            {
                if (_listItems[i] != null)
                    Destroy(_listItems[i].gameObject);
            }
            _listItems.Clear();

            if (_loanSystem == null || _loanSystem.Portfolio == null) return;

            var loans = _loanSystem.Portfolio.AllLoans;
            var lots = _cityManager != null ? _cityManager.AllLots : null;
            bool hasActiveLoans = false;

            for (int i = 0; i < loans.Count; i++)
            {
                var loan = loans[i];
                if (!loan.IsActive) continue;

                hasActiveLoans = true;
                string displayName = LotDisplayHelper.GetDisplayName(lots, loan.LotId);

                if (_loanItemPrefab != null && _loanListContainer != null)
                {
                    var item = Instantiate(_loanItemPrefab, _loanListContainer);
                    item.Setup(loan, displayName);
                    _listItems.Add(item);
                }
            }

            if (_emptyStateObject != null)
                _emptyStateObject.SetActive(!hasActiveLoans);
        }

        private void RefreshSummary()
        {
            // Loan summary (property reads only)
            if (_loanSystem != null)
            {
                if (_totalDebtText != null)
                    _totalDebtText.text = $"Total Debt: ${_loanSystem.TotalOutstandingPrincipal:N2}";
                if (_totalMonthlyText != null)
                    _totalMonthlyText.text = $"Monthly Payments: ${_loanSystem.TotalMonthlyDebt:N2}";
            }

            // Credit card summary (property reads only)
            if (_creditCardSystem != null)
            {
                if (_creditScoreText != null)
                    _creditScoreText.text = $"Credit Score: {_creditCardSystem.CreditScore}";
                if (_creditBalanceText != null)
                    _creditBalanceText.text = $"CC Balance: ${_creditCardSystem.CurrentBalance:N2}";
                if (_availableCreditText != null)
                    _availableCreditText.text = $"Available Credit: ${_creditCardSystem.AvailableCredit:N2}";
                if (_utilizationText != null)
                    _utilizationText.text = $"Utilization: {_creditCardSystem.Utilization:P0}";
            }
        }
    }
}
