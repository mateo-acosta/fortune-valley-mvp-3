using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the collection of active loans.
    /// Pure C# class extracted from LoanSystem to keep
    /// loops, math, and collection logic out of MonoBehaviours.
    ///
    /// LEARNING DESIGN: Students can see all their loans at a glance,
    /// comparing terms and tracking progress toward payoff.
    /// </summary>
    public class LoanPortfolio
    {
        private readonly List<ActiveLoan> _loans = new List<ActiveLoan>();

        public IReadOnlyList<ActiveLoan> AllLoans => _loans;

        /// <summary>
        /// Create and add a new loan to the portfolio.
        /// Returns null if a loan already exists for this lot.
        /// Caller is responsible for deducting the down payment.
        /// </summary>
        public ActiveLoan Originate(
            string loanId,
            string lotId,
            float purchasePrice,
            float apr,
            int termMonths,
            float downPaymentPercent,
            int startDay)
        {
            // Reject duplicate: one loan per lot
            for (int i = 0; i < _loans.Count; i++)
            {
                if (_loans[i].LotId == lotId && _loans[i].IsActive)
                    return null;
            }

            float downPayment = purchasePrice * downPaymentPercent;
            float principal = purchasePrice - downPayment;

            // No loan needed if down payment covers full price
            if (principal <= 0f) return null;

            float monthlyPayment = ActiveLoan.CalculateMonthlyPayment(principal, apr, termMonths);

            var loan = new ActiveLoan(
                loanId, lotId, principal, apr, termMonths,
                monthlyPayment, downPayment, startDay);

            _loans.Add(loan);
            return loan;
        }

        /// <summary>
        /// Process monthly payments for all active loans.
        /// Calls onPaymentDeducted for each payment that should be deducted from checking.
        /// Calls onPaymentMissed for each loan where checking had insufficient funds.
        /// Returns via callbacks so the MonoBehaviour can handle currency deduction.
        /// </summary>
        /// <summary>
        /// Stage 0a alias for ProcessMonthlyPayments. Same callback contract
        /// and behavior. Renamed because the payment cycle currently fires
        /// once per in-game year.
        /// </summary>
        public void ProcessYearlyPayments(
            System.Func<float, string, bool> tryDeductFromChecking,
            System.Action<ActiveLoan, float> onPaymentMade,
            System.Action<ActiveLoan> onPaymentMissed)
            => ProcessMonthlyPayments(tryDeductFromChecking, onPaymentMade, onPaymentMissed);

        public void ProcessMonthlyPayments(
            System.Func<float, string, bool> tryDeductFromChecking,
            System.Action<ActiveLoan, float> onPaymentMade,
            System.Action<ActiveLoan> onPaymentMissed)
        {
            for (int i = 0; i < _loans.Count; i++)
            {
                var loan = _loans[i];
                if (!loan.IsActive) continue;

                bool success = tryDeductFromChecking(
                    loan.MonthlyPayment,
                    $"Loan payment: {loan.LoanId} on lot {loan.LotId}");

                if (success)
                {
                    float paid = loan.ApplyPayment();
                    onPaymentMade?.Invoke(loan, paid);
                }
                else
                {
                    loan.RecordMissedPayment();
                    onPaymentMissed?.Invoke(loan);
                }
            }
        }

        /// <summary>
        /// Sum of all active loan monthly payments (for DTI calculation).
        /// </summary>
        public float GetTotalMonthlyDebt()
        {
            float total = 0f;
            for (int i = 0; i < _loans.Count; i++)
            {
                if (_loans[i].IsActive)
                    total += _loans[i].MonthlyPayment;
            }
            return total;
        }

        /// <summary>
        /// Stage 0a alias: payments fire once per billing cycle (= 1 in-game
        /// year), so the sum equals total yearly debt. Same value, new name.
        /// </summary>
        public float GetTotalYearlyDebt() => GetTotalMonthlyDebt();

        /// <summary>
        /// Sum of all remaining loan balances (for insolvency check).
        /// </summary>
        public float GetTotalOutstandingPrincipal()
        {
            float total = 0f;
            for (int i = 0; i < _loans.Count; i++)
            {
                if (_loans[i].IsActive)
                    total += _loans[i].RemainingBalance;
            }
            return total;
        }

        /// <summary>
        /// Get the active loan for a specific lot, or null if none.
        /// </summary>
        public ActiveLoan GetLoanForLot(string lotId)
        {
            for (int i = 0; i < _loans.Count; i++)
            {
                if (_loans[i].LotId == lotId && _loans[i].IsActive)
                    return _loans[i];
            }
            return null;
        }

        /// <summary>
        /// Check if a lot has an active loan.
        /// </summary>
        public bool HasLoanOnLot(string lotId)
        {
            return GetLoanForLot(lotId) != null;
        }

        /// <summary>
        /// Get all active (not paid-off) loans.
        /// </summary>
        public List<ActiveLoan> GetAllActive()
        {
            var result = new List<ActiveLoan>();
            for (int i = 0; i < _loans.Count; i++)
            {
                if (_loans[i].IsActive)
                    result.Add(_loans[i]);
            }
            return result;
        }

        /// <summary>
        /// Add a pre-built loan (state restoration only, no validation).
        /// </summary>
        public void AddRestored(ActiveLoan loan)
        {
            if (loan != null) _loans.Add(loan);
        }

        /// <summary>
        /// Clear all loans (game reset / bankruptcy).
        /// </summary>
        public void Clear()
        {
            _loans.Clear();
        }

        // ===============================================================
        // STATIC HELPERS (config filtering)
        // ===============================================================

        /// <summary>
        /// Filter loan configs by credit score and DTI.
        /// Returns only configs the player qualifies for.
        /// </summary>
        public static List<LoanConfig> GetAvailableLoans(
            IReadOnlyList<LoanConfig> allConfigs,
            int creditScore,
            float dti)
        {
            var available = new List<LoanConfig>();
            if (allConfigs == null) return available;

            for (int i = 0; i < allConfigs.Count; i++)
            {
                var config = allConfigs[i];
                if (config == null) continue;

                if (creditScore >= config.MinimumCreditScore && dti <= config.MaxDtiRatio)
                {
                    available.Add(config);
                }
            }
            return available;
        }

        /// <summary>
        /// Find a loan config by ID.
        /// </summary>
        public static LoanConfig FindLoanConfig(
            IReadOnlyList<LoanConfig> configs, string loanId)
        {
            if (configs == null) return null;

            for (int i = 0; i < configs.Count; i++)
            {
                if (configs[i] != null && configs[i].LoanId == loanId)
                    return configs[i];
            }
            return null;
        }
    }
}
