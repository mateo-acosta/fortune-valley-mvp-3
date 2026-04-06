using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages player's money across checking and investing accounts.
    /// Checking: liquid cash for spending, loan payments, CC repayments.
    /// Investing: separate pool for buying/selling shares.
    /// Credit card charges route through GameEvents (not this class).
    /// </summary>
    public class CurrencyManager : MonoBehaviour, ICurrencyService
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Starting Balances")]
        [Tooltip("Money the player starts with in checking")]
        [SerializeField] private float _startingCheckingBalance = 1000f;

        [Tooltip("Money the player starts with in investing")]
        [SerializeField] private float _startingInvestingBalance = 0f;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _checkingBalance;
        private float _investingBalance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current checking account balance (liquid cash).
        /// </summary>
        public float CheckingBalance => _checkingBalance;

        /// <summary>
        /// Current investing account balance.
        /// </summary>
        public float InvestingBalance => _investingBalance;

        /// <summary>
        /// Combined checking + investing balance.
        /// </summary>
        public float TotalLiquidBalance => _checkingBalance + _investingBalance;

        /// <summary>
        /// Alias for CheckingBalance. Used by UI components that read the primary balance.
        /// </summary>
        public float Balance => _checkingBalance;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;

            // Placeholder: deduct credit card charges from checking
            // until CreditCardSystem is built (Phase 1) and takes over this subscription
            GameEvents.OnCreditCardChargeRequested += HandleCreditCardChargePlaceholder;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnCreditCardChargeRequested -= HandleCreditCardChargePlaceholder;
        }

        private void HandleGameStart()
        {
            ResetBalance();
        }

        /// <summary>
        /// Placeholder handler for credit card charges.
        /// Deducts from checking until CreditCardSystem replaces this in Phase 1.
        /// </summary>
        private void HandleCreditCardChargePlaceholder(float amount, string reason)
        {
            TrySpendChecking(amount, reason);
        }

        // ═══════════════════════════════════════════════════════════════
        // CHECKING ACCOUNT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add money to checking account.
        /// Used for: restaurant income, loan proceeds.
        /// </summary>
        public void AddToChecking(float amount, string source = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to add non-positive amount: {amount}");
                return;
            }

            _checkingBalance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Checking +${amount:F2} from {source}. Balance: ${_checkingBalance:F2}");
            }

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, amount);
            GameEvents.RaiseCurrencyChanged(_checkingBalance, amount);
            GameEvents.RaiseIncomeGenerated(amount, source);
        }

        /// <summary>
        /// Try to spend from checking account. Returns true if successful.
        /// Used for: loan payments, CC repayments, loan down payments, investment transfers.
        /// </summary>
        public bool TrySpendChecking(float amount, string reason = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to spend non-positive amount: {amount}");
                return false;
            }

            if (_checkingBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot spend ${amount:F2} for {reason}. Checking: ${_checkingBalance:F2}");
                }
                return false;
            }

            _checkingBalance -= amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Checking -${amount:F2} for {reason}. Balance: ${_checkingBalance:F2}");
            }

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, -amount);
            GameEvents.RaiseCurrencyChanged(_checkingBalance, -amount);
            return true;
        }

        /// <summary>
        /// Check if player can afford an amount from checking.
        /// </summary>
        public bool CanAffordChecking(float amount)
        {
            return _checkingBalance >= amount;
        }

        // ═══════════════════════════════════════════════════════════════
        // INVESTING ACCOUNT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Add money to investing account.
        /// Used for: sell proceeds from shares.
        /// </summary>
        public void AddToInvesting(float amount, string source = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to add non-positive amount to investing: {amount}");
                return;
            }

            _investingBalance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Investing +${amount:F2} from {source}. Balance: ${_investingBalance:F2}");
            }

            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, amount);
        }

        /// <summary>
        /// Try to spend from investing account. Returns true if successful.
        /// Used for: buying shares.
        /// </summary>
        public bool TrySpendInvesting(float amount, string reason = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to spend non-positive from investing: {amount}");
                return false;
            }

            if (_investingBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot spend ${amount:F2} from investing for {reason}. Balance: ${_investingBalance:F2}");
                }
                return false;
            }

            _investingBalance -= amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Investing -${amount:F2} for {reason}. Balance: ${_investingBalance:F2}");
            }

            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, -amount);
            return true;
        }

        /// <summary>
        /// Check if player can afford an amount from investing.
        /// </summary>
        public bool CanAffordInvesting(float amount)
        {
            return _investingBalance >= amount;
        }

        // ═══════════════════════════════════════════════════════════════
        // TRANSFERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Transfer money from checking to investing.
        /// Returns true if successful.
        /// </summary>
        public bool TransferToInvesting(float amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Transfer amount must be positive: {amount}");
                return false;
            }

            if (_checkingBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot transfer ${amount:F2} to investing. Checking: ${_checkingBalance:F2}");
                }
                return false;
            }

            _checkingBalance -= amount;
            _investingBalance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Transferred ${amount:F2} checking -> investing. Checking: ${_checkingBalance:F2}, Investing: ${_investingBalance:F2}");
            }

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, -amount);
            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, amount);
            return true;
        }

        /// <summary>
        /// Transfer money from investing to checking.
        /// Returns true if successful.
        /// </summary>
        public bool TransferFromInvesting(float amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Transfer amount must be positive: {amount}");
                return false;
            }

            if (_investingBalance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot transfer ${amount:F2} from investing. Investing: ${_investingBalance:F2}");
                }
                return false;
            }

            _investingBalance -= amount;
            _checkingBalance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] Transferred ${amount:F2} investing -> checking. Checking: ${_checkingBalance:F2}, Investing: ${_investingBalance:F2}");
            }

            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, -amount);
            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, amount);
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        // RESET / SETUP
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Reset both accounts to starting amounts.
        /// </summary>
        public void ResetBalance()
        {
            _checkingBalance = _startingCheckingBalance;
            _investingBalance = _startingInvestingBalance;

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, 0f);
            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, 0f);
            GameEvents.RaiseCurrencyChanged(_checkingBalance, 0f);
        }

        /// <summary>
        /// Set checking balance directly (testing and state loading).
        /// </summary>
        public void SetCheckingBalance(float amount)
        {
            float delta = amount - _checkingBalance;
            _checkingBalance = amount;
            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, delta);
            GameEvents.RaiseCurrencyChanged(_checkingBalance, delta);
        }

        /// <summary>
        /// Set investing balance directly (testing and state loading).
        /// </summary>
        public void SetInvestingBalance(float amount)
        {
            float delta = amount - _investingBalance;
            _investingBalance = amount;
            GameEvents.RaiseInvestingBalanceChanged(_investingBalance, delta);
        }
    }
}
