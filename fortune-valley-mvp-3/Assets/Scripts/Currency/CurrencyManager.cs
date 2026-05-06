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
    ///
    /// Implements IBankruptcyResettable so BankruptcyResetService can clear
    /// the checking balance back to the starting amount during a soft reset.
    /// (Investing portfolio is wiped by InvestmentSystem.OnBankruptcyReset.)
    /// </summary>
    public class CurrencyManager : MonoBehaviour, ICurrencyService, IBankruptcyResettable
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Starting Balances")]
        [Tooltip("Money the player starts with in checking")]
        [SerializeField] private float _startingCheckingBalance = 1000f;

        [Header("Portfolio Reference")]
        [Tooltip("Investment system used to compute investing balance from portfolio value")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("Settings")]
        [Tooltip("Minimum change in portfolio value to fire OnInvestingBalanceChanged")]
        [SerializeField] private float _investingBalanceChangeThreshold = 0.01f;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private float _checkingBalance;
        private float _lastInvestingBalance;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current checking account balance (liquid cash).
        /// </summary>
        public float CheckingBalance => _checkingBalance;

        /// <summary>
        /// Current investing balance (total portfolio market value).
        /// Computed from InvestmentSystem, not a stored cash balance.
        /// </summary>
        public float InvestingBalance =>
            _investmentSystem != null ? _investmentSystem.TotalPortfolioValue : 0f;

        /// <summary>
        /// Combined checking + investing balance.
        /// </summary>
        public float TotalLiquidBalance => _checkingBalance + InvestingBalance;

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
            GameEvents.OnTick += HandleTick;

            // Handle transfer intent events from UI
            GameEvents.OnTransferRequested += HandleTransferRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnTransferRequested -= HandleTransferRequested;
        }

        private void HandleGameStart()
        {
            ResetBalance();
        }

        private void HandleTick(int tickNumber)
        {
            // Update investing balance from live portfolio value and fire event if changed
            float currentPortfolioValue = InvestingBalance;
            float delta = currentPortfolioValue - _lastInvestingBalance;

            if (Mathf.Abs(delta) > _investingBalanceChangeThreshold)
            {
                _lastInvestingBalance = currentPortfolioValue;
                GameEvents.RaiseInvestingBalanceChanged(currentPortfolioValue, delta);
            }
        }

        /// <summary>
        /// Handles transfer intent events fired by UI.
        /// With the new portfolio-based investing balance, transfers between
        /// checking and investing are no longer applicable. Buying/selling
        /// stocks handles the money flow directly.
        /// </summary>
        private void HandleTransferRequested(AccountType from, AccountType to, float amount)
        {
            // Transfers to/from investing are no longer needed.
            // Buying deducts from checking; selling adds to checking.
            // The investing balance is computed from portfolio value.
            Debug.Log($"[CurrencyManager] Transfer request ignored (investing is now portfolio-based). From: {from}, To: {to}, Amount: {amount}");
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
        // INVESTING BALANCE (computed from portfolio value)
        // ═══════════════════════════════════════════════════════════════
        // The investing balance is no longer a cash account. It reflects
        // the total market value of the player's portfolio. Buying
        // deducts from checking; selling adds to checking. The investing
        // balance updates automatically as stock prices change.

        // ═══════════════════════════════════════════════════════════════
        // RESET / SETUP
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// IBankruptcyResettable. On soft bankruptcy, checking returns to the
        /// starting balance. Investments are zeroed out by InvestmentSystem;
        /// CC debt by CreditCardSystem. The orchestrating BankruptcyResetService
        /// fires OnSoftBankruptcyReset after every IBankruptcyResettable runs.
        /// </summary>
        public void OnBankruptcyReset()
        {
            ResetBalance();
        }

        /// <summary>
        /// Reset both accounts to starting amounts.
        /// </summary>
        public void ResetBalance()
        {
            _checkingBalance = _startingCheckingBalance;
            _lastInvestingBalance = 0f;

            GameEvents.RaiseCheckingBalanceChanged(_checkingBalance, 0f);
            GameEvents.RaiseInvestingBalanceChanged(0f, 0f);
            GameEvents.RaiseCurrencyChanged(_checkingBalance, 0f);
        }

        /// <summary>
        /// Restore checking balance from a saved state and refresh investing
        /// so UI components pick up both values after all systems are loaded.
        /// </summary>
        public void ApplyState(float checkingBalance)
        {
            SetCheckingBalance(checkingBalance);
            RefreshInvestingBalance();
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
        /// Force an investing balance update (testing and state loading).
        /// Reads current portfolio value and fires the event.
        /// </summary>
        public void RefreshInvestingBalance()
        {
            float current = InvestingBalance;
            float delta = current - _lastInvestingBalance;
            _lastInvestingBalance = current;
            GameEvents.RaiseInvestingBalanceChanged(current, delta);
        }
    }
}
