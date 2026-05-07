using System;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Runtime state for an active loan. Stores all computed values
    /// as primitives (copied from LoanConfig at origination time).
    /// LoanPortfolio manages the collection of these.
    ///
    /// LEARNING DESIGN: Students see their monthly payment, remaining
    /// balance, and total interest paid -- making the cost of borrowing
    /// tangible and comparable across loan options.
    /// </summary>
    public class ActiveLoan
    {
        private const int MonthsPerYear = 12;
        private const float PaidOffThreshold = 0.01f;

        private readonly string _loanId;
        private readonly string _lotId;
        private readonly float _principal;
        private readonly float _apr;
        private readonly int _termMonths;
        private readonly float _monthlyPayment;
        private readonly float _downPayment;
        private readonly int _startDay;

        private float _remainingBalance;
        private int _paymentsMade;
        private int _missedPayments;
        private bool _isPaidOff;

        public ActiveLoan(
            string loanId,
            string lotId,
            float principal,
            float apr,
            int termMonths,
            float monthlyPayment,
            float downPayment,
            int startDay)
        {
            _loanId = loanId;
            _lotId = lotId;
            _principal = principal;
            _apr = apr;
            _termMonths = termMonths;
            _monthlyPayment = monthlyPayment;
            _downPayment = downPayment;
            _startDay = startDay;
            _remainingBalance = principal;
            _paymentsMade = 0;
            _missedPayments = 0;
            _isPaidOff = false;
        }

        // Read-only accessors
        public string LoanId => _loanId;
        public string LotId => _lotId;
        public float Principal => _principal;
        public float APR => _apr;
        public int TermMonths => _termMonths;
        public float MonthlyPayment => _monthlyPayment;
        public float DownPayment => _downPayment;
        public int StartDay => _startDay;
        public float RemainingBalance => _remainingBalance;
        public int PaymentsMade => _paymentsMade;
        public int PaymentsRemaining => _termMonths - _paymentsMade;
        public int MissedPayments => _missedPayments;
        public bool IsPaidOff => _isPaidOff;
        public bool IsActive => !_isPaidOff;

        // Stage 0a aliases. The current "monthly" payment fires once per
        // billing cycle (BillingCycleDays = 30 = 1 in-game year), so it is
        // effectively a yearly payment. Legacy "monthly" naming is kept for
        // backward compat through the alias chain (removed in Stage 0c).
        public float YearlyPayment => _monthlyPayment;
        public int TermTicks => _termMonths;
        public int StartTick => _startDay;

        /// <summary>
        /// Total cost of the loan (all payments + down payment).
        /// </summary>
        public float TotalCost => (_monthlyPayment * _termMonths) + _downPayment;

        /// <summary>
        /// Total interest over the life of the loan.
        /// </summary>
        public float TotalInterest => TotalCost - (_principal + _downPayment);

        /// <summary>
        /// Apply a monthly payment. Returns the amount actually applied.
        /// Final payment may be less than the standard monthly amount.
        /// </summary>
        public float ApplyPayment()
        {
            if (_isPaidOff) return 0f;

            // Final payment: pay only what remains
            float payment = Math.Min(_monthlyPayment, _remainingBalance);
            _remainingBalance -= payment;
            _paymentsMade++;

            if (_remainingBalance <= PaidOffThreshold)
            {
                _remainingBalance = 0f;
                _isPaidOff = true;
            }

            return payment;
        }

        /// <summary>
        /// Record a missed payment (checking account had insufficient funds).
        /// </summary>
        public void RecordMissedPayment()
        {
            _missedPayments++;
        }

        /// <summary>
        /// Reconstruct a loan from a saved state snapshot.
        /// Unlike the normal constructor, this sets remaining_balance and
        /// payments_made to their saved values instead of fresh-loan defaults.
        /// </summary>
        public static ActiveLoan FromSave(
            string loanId,
            string lotId,
            float principal,
            float apr,
            int termMonths,
            float monthlyPayment,
            float downPayment,
            int startDay,
            float remainingBalance,
            int paymentsMade)
        {
            var loan = new ActiveLoan(
                loanId, lotId, principal, apr,
                termMonths, monthlyPayment, downPayment, startDay);
            loan._remainingBalance = remainingBalance;
            loan._paymentsMade = paymentsMade;
            if (remainingBalance <= PaidOffThreshold)
            {
                loan._remainingBalance = 0f;
                loan._isPaidOff = true;
            }
            return loan;
        }

        /// <summary>
        /// Calculate monthly payment using the standard amortization formula.
        /// Uses double precision for accuracy, returns float for storage.
        /// Zero APR is handled as simple division (principal / term).
        /// </summary>
        public static float CalculateMonthlyPayment(float principal, float apr, int termMonths)
        {
            if (principal <= 0f || termMonths <= 0) return 0f;

            // Zero APR: simple equal payments
            if (apr <= 0f)
            {
                return (float)((double)principal / termMonths);
            }

            // Standard amortization: P * [r(1+r)^n] / [(1+r)^n - 1]
            double monthlyRate = (double)apr / MonthsPerYear;
            double compoundFactor = Math.Pow(1.0 + monthlyRate, termMonths);
            double payment = (double)principal * (monthlyRate * compoundFactor) / (compoundFactor - 1.0);

            return (float)payment;
        }

        /// <summary>
        /// Stage 0a alias: same amortization, named for the new tick vocabulary.
        /// </summary>
        public static float CalculateYearlyPayment(float principal, float apr, int termTicks)
            => CalculateMonthlyPayment(principal, apr, termTicks);
    }
}
