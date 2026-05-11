using System.Collections.Generic;

namespace FortuneValley.Domain.Entities
{
    /// <summary>
    /// Runtime state for the player's credit card account.
    /// Tracks balance, billing cycle, payment history, and interest.
    ///
    /// LEARNING DESIGN: Students see exactly how unpaid balances grow
    /// with interest, making the cost of carrying debt tangible.
    /// </summary>
    [System.Serializable]
    public class ActiveCreditCard
    {
        private const int MonthsPerYear = 12;
        private const int DefaultMaxHistory = 12;

        // Current outstanding balance (what the player owes right now)
        private float _currentBalance;

        // Statement balance at last billing cycle close
        private float _statementBalance;

        // Minimum payment due this cycle
        private float _minimumPaymentDue;

        // Interest accrued since last statement
        private float _interestAccrued;

        // Days elapsed in current billing cycle
        private int _daysSinceLastStatement;

        // Payment history: true = on-time, false = missed (most recent first)
        private List<bool> _paymentHistory;

        // Maximum history entries to keep
        private int _maxHistoryEntries;

        public ActiveCreditCard(int maxHistoryEntries = DefaultMaxHistory)
        {
            _maxHistoryEntries = maxHistoryEntries;
            _paymentHistory = new List<bool>();
        }

        // Read-only accessors
        public float CurrentBalance => _currentBalance;
        public float StatementBalance => _statementBalance;
        public float MinimumPaymentDue => _minimumPaymentDue;
        public float InterestAccrued => _interestAccrued;
        public int DaysSinceLastStatement => _daysSinceLastStatement;
        public IReadOnlyList<bool> PaymentHistory => _paymentHistory;

        /// <summary>
        /// Add a charge to the credit card balance.
        /// Returns false if the charge would exceed the given credit limit.
        /// </summary>
        public bool TryCharge(float amount, float creditLimit)
        {
            if (amount <= 0) return false;
            if (_currentBalance + amount > creditLimit) return false;

            _currentBalance += amount;
            return true;
        }

        /// <summary>
        /// Available credit remaining.
        /// </summary>
        public float AvailableCredit(float creditLimit)
        {
            float available = creditLimit - _currentBalance;
            return available > 0 ? available : 0;
        }

        /// <summary>
        /// Current utilization ratio (0 to 1+).
        /// </summary>
        public float Utilization(float creditLimit)
        {
            if (creditLimit <= 0) return 0;
            return _currentBalance / creditLimit;
        }

        /// <summary>
        /// Advance the billing cycle day counter.
        /// </summary>
        public void AdvanceDay()
        {
            _daysSinceLastStatement++;
        }

        /// <summary>
        /// Close the current billing cycle and generate a statement.
        /// Calculates interest on unpaid balance and sets minimum payment.
        /// </summary>
        public void CloseStatement(float apr, float minPaymentPercent, float minPaymentFloor)
        {
            // Calculate interest on the current balance
            // Monthly rate = APR / 12
            float monthlyRate = apr / MonthsPerYear;
            _interestAccrued = _currentBalance * monthlyRate;
            _currentBalance += _interestAccrued;

            // Set statement balance to current balance (including new interest)
            _statementBalance = _currentBalance;

            // Calculate minimum payment
            float percentBasedMin = _statementBalance * minPaymentPercent;
            _minimumPaymentDue = percentBasedMin > minPaymentFloor ? percentBasedMin : minPaymentFloor;

            // Cap minimum at statement balance (don't require more than owed)
            if (_minimumPaymentDue > _statementBalance)
                _minimumPaymentDue = _statementBalance;

            _daysSinceLastStatement = 0;
        }

        /// <summary>
        /// Apply a payment to the credit card balance.
        /// Returns the actual amount paid (capped at current balance).
        /// Records payment as on-time if amount >= minimum payment due.
        /// </summary>
        public float ApplyPayment(float amount)
        {
            if (amount <= 0) return 0;

            // Cap at current balance
            float actualPayment = amount > _currentBalance ? _currentBalance : amount;
            _currentBalance -= actualPayment;

            // Record payment history
            bool onTime = actualPayment >= _minimumPaymentDue;
            RecordPayment(onTime);

            return actualPayment;
        }

        /// <summary>
        /// Record a missed payment (player chose not to pay or had insufficient funds).
        /// </summary>
        public void RecordMissedPayment()
        {
            RecordPayment(false);
        }

        /// <summary>
        /// Count of on-time payments in history.
        /// </summary>
        public int OnTimePaymentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _paymentHistory.Count; i++)
                {
                    if (_paymentHistory[i]) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Count of missed payments in history.
        /// </summary>
        public int MissedPaymentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _paymentHistory.Count; i++)
                {
                    if (!_paymentHistory[i]) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Set the current balance directly (state restoration only).
        /// </summary>
        public void SetBalance(float balance)
        {
            _currentBalance = balance;
        }

        /// <summary>
        /// Reset all state (used on game start or bankruptcy).
        /// </summary>
        public void Reset()
        {
            _currentBalance = 0;
            _statementBalance = 0;
            _minimumPaymentDue = 0;
            _interestAccrued = 0;
            _daysSinceLastStatement = 0;
            _paymentHistory.Clear();
        }

        private void RecordPayment(bool onTime)
        {
            _paymentHistory.Insert(0, onTime);
            while (_paymentHistory.Count > _maxHistoryEntries)
            {
                _paymentHistory.RemoveAt(_paymentHistory.Count - 1);
            }
        }
    }
}
