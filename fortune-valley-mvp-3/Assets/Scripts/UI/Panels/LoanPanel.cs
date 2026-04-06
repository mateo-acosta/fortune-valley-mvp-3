using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// Read-only panel displaying all active loans.
    /// Shows remaining balance, monthly payment, and payment progress per loan.
    ///
    /// LEARNING DESIGN: Seeing all debt in one place teaches students
    /// about total debt obligations and the path to paying each off.
    /// </summary>
    public class LoanPanel : UIPanel
    {
        // ===============================================================
        // REFERENCES
        // ===============================================================

        [Header("List")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private LoanListItem _loanItemPrefab;

        [Header("Summary")]
        [SerializeField] private TextMeshProUGUI _totalDebtText;
        [SerializeField] private TextMeshProUGUI _totalMonthlyText;
        [SerializeField] private TextMeshProUGUI _emptyStateText;
        [SerializeField] private Button _closeButton;

        [Header("Dependencies")]
        [SerializeField] private LoanSystem _loanSystem;
        [SerializeField] private CityManager _cityManager;

        // ===============================================================
        // STATE
        // ===============================================================

        private List<LoanListItem> _listItems = new List<LoanListItem>();

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void Start()
        {
            if (_loanSystem == null) Debug.LogError("[LoanPanel] _loanSystem not wired.");
            if (_cityManager == null) Debug.LogError("[LoanPanel] _cityManager not wired.");

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnEnable()
        {
            GameEvents.OnLoanOriginated += HandleLoanChanged;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
        }

        private void OnDisable()
        {
            GameEvents.OnLoanOriginated -= HandleLoanChanged;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
        }

        // ===============================================================
        // PANEL OVERRIDE
        // ===============================================================

        protected override void OnShow()
        {
            RefreshList();
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleLoanChanged(ActiveLoan loan)
        {
            if (IsVisible) RefreshList();
        }

        private void HandleLoanPaymentMade(ActiveLoan loan, float amount)
        {
            if (IsVisible) RefreshList();
        }

        private void HandleLoanPaidOff(ActiveLoan loan)
        {
            if (IsVisible) RefreshList();
        }

        // ===============================================================
        // LIST MANAGEMENT
        // ===============================================================

        private void RefreshList()
        {
            ClearList();

            if (_loanSystem == null) return;

            var loans = _loanSystem.Portfolio.AllLoans;
            bool hasActiveLoans = false;

            for (int i = 0; i < loans.Count; i++)
            {
                var loan = loans[i];
                if (!loan.IsActive) continue;

                hasActiveLoans = true;
                string displayName = GetLotDisplayName(loan.LotId);
                CreateListItem(loan, displayName);
            }

            if (_emptyStateText != null)
                _emptyStateText.gameObject.SetActive(!hasActiveLoans);

            UpdateSummary();
        }

        private void CreateListItem(ActiveLoan loan, string lotDisplayName)
        {
            if (_loanItemPrefab == null || _listContainer == null) return;

            var item = Instantiate(_loanItemPrefab, _listContainer);
            item.Setup(loan, lotDisplayName);
            _listItems.Add(item);
        }

        private void ClearList()
        {
            for (int i = 0; i < _listItems.Count; i++)
            {
                if (_listItems[i] != null)
                    Destroy(_listItems[i].gameObject);
            }
            _listItems.Clear();
        }

        private void UpdateSummary()
        {
            if (_loanSystem == null) return;

            if (_totalDebtText != null)
                _totalDebtText.text = $"Total Debt: ${_loanSystem.TotalOutstandingPrincipal:N2}";

            if (_totalMonthlyText != null)
                _totalMonthlyText.text = $"Monthly Payments: ${_loanSystem.TotalMonthlyDebt:N2}";
        }

        private string GetLotDisplayName(string lotId)
        {
            if (_cityManager == null) return lotId;

            var allLots = _cityManager.AllLots;
            for (int i = 0; i < allLots.Count; i++)
            {
                if (allLots[i].LotId == lotId)
                    return allLots[i].DisplayName;
            }

            return lotId;
        }
    }
}
