using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages player's money using a single unified balance.
    /// All spending and earning flows through this manager.
    ///
    /// SIMPLIFIED: Previously used dual accounts (Checking/Investing).
    /// Now uses a single balance for easier gameplay - all purchases
    /// draw from the same pool.
    /// </summary>
    public class CurrencyManager : MonoBehaviour, ICurrencyService
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Starting Balance")]
        [Tooltip("Money the player starts with")]
        [SerializeField] private float _startingBalance = 1000f;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _balance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current balance. All purchases use this single pool.
        /// </summary>
        public float Balance => _balance;

        /// <summary>
        /// Alias for Balance (backward compatibility).
        /// </summary>
        public float CheckingBalance => _balance;

        /// <summary>
        /// Alias for Balance (backward compatibility).
        /// </summary>
        public float InvestingBalance => _balance;

        /// <summary>
        /// Alias for Balance (backward compatibility).
        /// </summary>
        public float TotalBalance => _balance;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            ResetBalance();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Reset balance to starting amount.
        /// </summary>
        public void ResetBalance()
        {
            _balance = _startingBalance;

            // Fire events for any listeners
            GameEvents.RaiseCheckingBalanceChanged(_balance, 0f);
            GameEvents.RaiseInvestingBalanceChanged(_balance, 0f);
            GameEvents.RaiseCurrencyChanged(_balance, 0f);
        }

        /// <summary>
        /// Add money to balance.
        /// AccountType parameter kept for backward compatibility but ignored.
        /// </summary>
        public void Add(float amount, AccountType account, string source = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to add non-positive amount: {amount}");
                return;
            }

            _balance += amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] +${amount:F2} from {source}. Balance: ${_balance:F2}");
            }

            GameEvents.RaiseCurrencyChanged(_balance, amount);
            GameEvents.RaiseCheckingBalanceChanged(_balance, amount);
            GameEvents.RaiseIncomeGenerated(amount, source);
        }

        /// <summary>
        /// Add money to balance.
        /// </summary>
        public void Add(float amount, string source = "Unknown")
        {
            Add(amount, AccountType.Checking, source);
        }

        /// <summary>
        /// Try to spend money. Returns true if successful.
        /// AccountType parameter kept for backward compatibility but ignored.
        /// </summary>
        public bool TrySpend(float amount, AccountType account, string reason = "Unknown")
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CurrencyManager] Tried to spend non-positive amount: {amount}");
                return false;
            }

            if (_balance < amount)
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CurrencyManager] Cannot spend ${amount:F2} for {reason}. Balance: ${_balance:F2}");
                }
                return false;
            }

            _balance -= amount;

            if (_logTransactions)
            {
                Debug.Log($"[CurrencyManager] -${amount:F2} for {reason}. Balance: ${_balance:F2}");
            }

            GameEvents.RaiseCurrencyChanged(_balance, -amount);
            GameEvents.RaiseCheckingBalanceChanged(_balance, -amount);
            return true;
        }

        /// <summary>
        /// Try to spend from balance.
        /// </summary>
        public bool TrySpend(float amount, string reason = "Unknown")
        {
            return TrySpend(amount, AccountType.Checking, reason);
        }

        /// <summary>
        /// Transfer between accounts (no-op with single balance, kept for compatibility).
        /// Always returns true since there's only one pool now.
        /// </summary>
        public bool Transfer(float amount, AccountType from, AccountType to)
        {
            // With single balance, transfers are no-ops but we still fire the event
            if (amount <= 0 || from == to)
                return false;

            GameEvents.RaiseTransfer(amount, from, to);
            return true;
        }

        /// <summary>
        /// Check if player can afford an amount.
        /// AccountType parameter kept for backward compatibility but ignored.
        /// </summary>
        public bool CanAfford(float amount, AccountType account)
        {
            return _balance >= amount;
        }

        /// <summary>
        /// Check if player can afford an amount.
        /// </summary>
        public bool CanAfford(float amount)
        {
            return _balance >= amount;
        }

        /// <summary>
        /// Get current balance.
        /// AccountType parameter kept for backward compatibility but ignored.
        /// </summary>
        public float GetBalance(AccountType account)
        {
            return _balance;
        }

        /// <summary>
        /// Set balance directly (use sparingly, mainly for testing).
        /// AccountType parameter kept for backward compatibility but ignored.
        /// </summary>
        public void SetBalance(AccountType account, float amount)
        {
            float delta = amount - _balance;
            _balance = amount;
            GameEvents.RaiseCurrencyChanged(_balance, delta);
            GameEvents.RaiseCheckingBalanceChanged(_balance, delta);
        }

        /// <summary>
        /// Set balance directly.
        /// </summary>
        public void SetBalance(float amount)
        {
            SetBalance(AccountType.Checking, amount);
        }
    }
}
