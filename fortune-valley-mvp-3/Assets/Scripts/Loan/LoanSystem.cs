using System;
using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages loan origination and monthly payments.
    /// Subscribes to intent events from UI and delegates
    /// all logic to LoanPortfolio (pure C#).
    ///
    /// LEARNING DESIGN: Loans let students finance lot purchases
    /// they cannot yet afford outright. The trade-off between
    /// paying interest vs waiting to save is a core lesson.
    ///
    /// Implements IBankruptcyResettable: on soft bankruptcy, all active
    /// loans are wiped (student starts fresh on the borrow side).
    /// </summary>
    public class LoanSystem : MonoBehaviour, IBankruptcyResettable
    {
        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        [Header("Available Loans")]
        [Tooltip("All loan options the player can choose from")]
        [SerializeField] private List<LoanConfig> _availableLoans;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions;

        // ===============================================================
        // CONSTANTS
        // ===============================================================

        private const int InitialStartDay = 0;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        private LoanPortfolio _portfolio;

        // ===============================================================
        // PUBLIC ACCESSORS
        // ===============================================================

        public LoanPortfolio Portfolio => _portfolio;

        public float TotalMonthlyDebt => _portfolio != null
            ? _portfolio.GetTotalMonthlyDebt()
            : 0f;

        // Stage 0a alias: the "monthly" payment fires once per billing cycle
        // (= 1 in-game year), so this sum IS the yearly debt total.
        public float TotalYearlyDebt => TotalMonthlyDebt;

        public float TotalOutstandingPrincipal => _portfolio != null
            ? _portfolio.GetTotalOutstandingPrincipal()
            : 0f;

        public IReadOnlyList<LoanConfig> AvailableLoans => _availableLoans;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnLoanPurchaseRequested += HandleLoanPurchaseRequested;
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;

            if (GameEvents.LastLoadedSaveDto != null)
            {
                HandleSaveStateLoaded(GameEvents.LastLoadedSaveDto);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnLoanPurchaseRequested -= HandleLoanPurchaseRequested;
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;
        }

        private void HandleSaveStateLoaded(GamePlayerStateDTO dto)
        {
            try { Hydrate(dto); }
            catch (Exception e) { Debug.LogError($"[{nameof(LoanSystem)}] hydrate failed: {e}"); }
        }

        private void HandleGameStart()
        {
            _portfolio = new LoanPortfolio();
        }

        /// IBankruptcyResettable. Soft reset: clear the loan portfolio so the
        /// player has no outstanding debt after bankruptcy.
        /// </summary>
        public void OnBankruptcyReset()
        {
            if (_portfolio != null)
            {
                _portfolio.Clear();
            }
            else
            {
                _portfolio = new LoanPortfolio();
            }
        }

        /// <summary>
        /// Rebuild the loan portfolio from a saved DTO.
        /// ADVISORY: contains a loop, but runs once at restore.
        /// Public so EditMode tests can call directly without raising the event.
        /// </summary>
        public void Hydrate(GamePlayerStateDTO state)
        {
            if (state == null) return;
            if (_portfolio == null) _portfolio = new LoanPortfolio();
            _portfolio.Clear();
            if (state.active_loans == null) return;

            for (int i = 0; i < state.active_loans.Length; i++)
            {
                var dto = state.active_loans[i];
                if (dto == null) continue;

                var loan = ActiveLoan.FromSave(
                    dto.loan_id, dto.lot_id, dto.principal, dto.apr,
                    dto.term_months, dto.monthly_payment, dto.down_payment,
                    dto.start_day, dto.remaining_balance, dto.payments_made);
                _portfolio.AddRestored(loan);
            }
        }

        // ===============================================================
        // LOAN ORIGINATION (via intent event from LoanSelectionPopup)
        // ===============================================================

        private void HandleLoanPurchaseRequested(string loanConfigId, string lotId, float price)
        {
            if (_portfolio == null || _currencyManager == null) return;

            // Look up config
            LoanConfig config = LoanPortfolio.FindLoanConfig(_availableLoans, loanConfigId);
            if (config == null)
            {
                if (_logTransactions)
                    Debug.Log($"[LoanSystem] Loan config '{loanConfigId}' not found.");
                return;
            }

            float prevBalance = TotalOutstandingPrincipal;

            // POC flow: no down payment. Full principal = lot price. Loan proceeds deposit
            // into checking; the lot itself is not auto-purchased (player must click Buy again).
            ActiveLoan loan = _portfolio.Originate(
                config.LoanId, lotId, price,
                config.APR, config.TermMonths,
                0f, InitialStartDay);

            if (loan == null)
            {
                if (_logTransactions)
                    Debug.Log($"[LoanSystem] Loan rejected for lot {lotId} (duplicate or zero principal).");
                return;
            }

            _currencyManager.AddToChecking(price, $"Loan proceeds: {config.DisplayName} for lot {lotId}");

            if (_logTransactions)
            {
                Debug.Log($"[LoanSystem] Loan originated: {config.DisplayName} for lot {lotId}. " +
                          $"Principal: ${loan.Principal:F2}, Monthly: ${loan.MonthlyPayment:F2}");
            }

            GameEvents.RaiseLoanOriginated(loan);

            float newBalance = TotalOutstandingPrincipal;
            GameEvents.RaiseLoanBalanceChanged(newBalance, newBalance - prevBalance);
        }

        // ===============================================================
        // MONTHLY PAYMENTS (called by MonthlyPaymentDayController)
        // ===============================================================

        /// <summary>
        /// Process monthly loan payments. Deducts from checking for each active loan.
        /// Called by MonthlyPaymentDayController on payment day (step 1).
        /// </summary>
        public void ProcessMonthlyPayments()
        {
            if (_portfolio == null || _currencyManager == null) return;

            float prevBalance = TotalOutstandingPrincipal;

            _portfolio.ProcessMonthlyPayments(
                _currencyManager.TrySpendChecking,
                HandlePaymentMade,
                HandlePaymentMissed);

            float newBalance = TotalOutstandingPrincipal;
            if (newBalance != prevBalance)
                GameEvents.RaiseLoanBalanceChanged(newBalance, newBalance - prevBalance);
        }

        /// <summary>
        /// Stage 0a alias for ProcessMonthlyPayments. Same behavior; the
        /// payment cycle currently fires once per in-game year so "yearly"
        /// is the accurate name.
        /// </summary>
        public void ProcessYearlyPayments() => ProcessMonthlyPayments();

        private void HandlePaymentMade(ActiveLoan loan, float amountPaid)
        {
            if (_logTransactions)
            {
                Debug.Log($"[LoanSystem] Payment ${amountPaid:F2} on {loan.LoanId}. " +
                          $"Remaining: ${loan.RemainingBalance:F2}");
            }

            GameEvents.RaiseLoanPaymentMade(loan, amountPaid);

            if (loan.IsPaidOff)
            {
                if (_logTransactions)
                    Debug.Log($"[LoanSystem] Loan {loan.LoanId} paid off!");

                GameEvents.RaiseLoanPaidOff(loan);
            }
        }

        private void HandlePaymentMissed(ActiveLoan loan)
        {
            if (_logTransactions)
            {
                Debug.Log($"[LoanSystem] MISSED payment on {loan.LoanId}. " +
                          $"Total missed: {loan.MissedPayments}");
            }

            GameEvents.RaiseLoanPaymentMissed(loan);
        }

        // ===============================================================
        // QUERY HELPERS (for DTI computation by external callers)
        // ===============================================================

        /// <summary>
        /// Get filtered loan configs based on credit score and DTI.
        /// Called when configuring the LoanSelectionPopup.
        /// </summary>
        public List<LoanConfig> GetQualifiedLoans(int creditScore, float dti)
        {
            return LoanPortfolio.GetAvailableLoans(_availableLoans, creditScore, dti);
        }
    }
}
