using System;
using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the player's credit score and the (now-disabled) credit card account.
    /// Subscribes to charge requests, tracks balance, generates statements when the
    /// CC mechanic is enabled. Owns the credit-score state at all times -- the score
    /// flows even with the CC mechanic off, driven by loan-payment behavior and DTI.
    ///
    /// LEARNING DESIGN: The credit score is a live signal that responds to the
    /// player's loan-payment behavior. Pay loans on time and keep DTI low to
    /// raise the score; missed payments and high debt loads lower it.
    ///
    /// Implements IBankruptcyResettable: on soft bankruptcy, the active card
    /// is recreated (zero balance, no history) and credit score returns to the
    /// configured starting value (650 by default).
    /// </summary>
    public class CreditScoreSystem : MonoBehaviour, IBankruptcyResettable
    {
        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        [Header("Config")]
        [SerializeField] private CreditCardConfig _config;
        [SerializeField] private CreditScoringConfig _scoringConfig;

        [Header("Dependencies")]
        [Tooltip("Used to query loan-payment behavior for the credit-score paidOnTime factor.")]
        [SerializeField] private LoanSystem _loanSystem;

        [Header("Debug")]
        [SerializeField] private bool _logTransactions = false;

        // ===============================================================
        // RUNTIME STATE
        // ===============================================================

        private ActiveCreditCard _card;
        private int _currentCreditScore;

        // ===============================================================
        // PUBLIC ACCESSORS
        // ===============================================================

        public float CurrentBalance => _card != null ? _card.CurrentBalance : 0f;
        public float StatementBalance => _card != null ? _card.StatementBalance : 0f;
        public float MinimumPaymentDue => _card != null ? _card.MinimumPaymentDue : 0f;
        public float InterestAccrued => _card != null ? _card.InterestAccrued : 0f;
        public int CreditScore => _currentCreditScore;

        public float AvailableCredit => _card != null && _config != null
            ? _card.AvailableCredit(_config.CreditLimit)
            : 0f;

        public float Utilization => _card != null && _config != null
            ? _card.Utilization(_config.CreditLimit)
            : 0f;

        public float CreditLimit => _config != null ? _config.CreditLimit : 0f;

        /// <summary>
        /// Number of in-game days per billing cycle. Used by MonthlyPaymentDayController.
        /// </summary>
        public int BillingCycleDays => _config != null ? _config.BillingCycleDays : 0;

        /// <summary>
        /// Stage 0a alias: same value, new name. The billing cycle is measured
        /// in gameplay ticks; one full cycle = 30 ticks = 1 in-game year.
        /// </summary>
        public int BillingCycleTicks => BillingCycleDays;

        /// <summary>
        /// Access payment history. Kept for compatibility; the credit-score
        /// calculation now sources paidOnTime from LoanSystem instead.
        /// </summary>
        public ActiveCreditCard Card => _card;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnSaveStateLoaded += HandleSaveStateLoaded;

            // CC charge/day-cycle handling only fires when the CC mechanic is on.
            // Score-related events flow regardless.
            if (FeatureFlags.CreditCardChargesEnabled)
            {
                GameEvents.OnCreditCardChargeRequested += HandleChargeRequested;
                GameEvents.OnDayEnd += HandleDayEnd;
            }

            if (GameEvents.LastLoadedSaveDto != null)
            {
                HandleSaveStateLoaded(GameEvents.LastLoadedSaveDto);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnSaveStateLoaded -= HandleSaveStateLoaded;

            if (FeatureFlags.CreditCardChargesEnabled)
            {
                GameEvents.OnCreditCardChargeRequested -= HandleChargeRequested;
                GameEvents.OnDayEnd -= HandleDayEnd;
            }
        }

        private void HandleSaveStateLoaded(GamePlayerStateDTO dto)
        {
            try { Hydrate(dto); }
            catch (Exception e) { Debug.LogError($"[{nameof(CreditScoreSystem)}] hydrate failed: {e}"); }
        }

        private void Start()
        {
            if (_scoringConfig == null)
                Debug.LogError("[CreditScoreSystem] _scoringConfig not wired in Inspector.");
            if (FeatureFlags.CreditCardChargesEnabled && _config == null)
                Debug.LogError("[CreditScoreSystem] _config not wired in Inspector (CC mechanic enabled).");
        }

        private void HandleGameStart()
        {
            if (GameEvents.LastLoadedSaveDto != null) return;
            ResetCardAndScore();
        }

        /// <summary>
        /// IBankruptcyResettable. Soft reset: clear debt, reset credit score
        /// to the configured starting value, recreate the active card.
        /// </summary>
        public void OnBankruptcyReset()
        {
            ResetCardAndScore();
        }

        private void ResetCardAndScore()
        {
            _card = new ActiveCreditCard();
            _currentCreditScore = _scoringConfig != null ? _scoringConfig.StartingScore : 0;

            // Re-raise initial values so HUD displays update on reset.
            GameEvents.RaiseCreditCardBalanceChanged(0f, 0f);
            GameEvents.RaiseCreditScoreChanged(_currentCreditScore);
        }

        // ===============================================================
        // CHARGE HANDLING (only active when CC mechanic is enabled)
        // ===============================================================

        /// <summary>
        /// Process a credit card charge request.
        /// Called via GameEvents.OnCreditCardChargeRequested.
        /// </summary>
        private void HandleChargeRequested(float amount, string reason)
        {
            if (_card == null || _config == null) return;

            bool success = _card.TryCharge(amount, _config.CreditLimit);

            if (success)
            {
                if (_logTransactions)
                    Debug.Log($"[CreditScoreSystem] Charged ${amount:F2} for {reason}. Balance: ${_card.CurrentBalance:F2}");

                GameEvents.RaiseCreditCardCharged(amount);
                GameEvents.RaiseCreditCardBalanceChanged(_card.CurrentBalance, amount);
            }
            else
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CreditScoreSystem] Charge of ${amount:F2} for {reason} DECLINED. " +
                              $"Balance: ${_card.CurrentBalance:F2}, Limit: ${_config.CreditLimit:F2}");
                }
            }
        }

        // ===============================================================
        // DAY CYCLE
        // ===============================================================

        private void HandleDayEnd(int dayNumber)
        {
            if (_card == null) return;
            _card.AdvanceDay();
        }

        // ===============================================================
        // STATEMENT (called by MonthlyPaymentDayController)
        // ===============================================================

        /// <summary>
        /// Generate a monthly statement. Called by MonthlyPaymentDayController
        /// on payment day. Closes the billing cycle and calculates interest.
        /// No-op when the CC mechanic is disabled.
        /// </summary>
        public void GenerateStatement()
        {
            if (!FeatureFlags.CreditCardChargesEnabled) return;
            if (_card == null || _config == null) return;

            _card.CloseStatement(
                _config.APR,
                _config.MinimumPaymentPercent,
                _config.MinimumPaymentFloor
            );

            if (_logTransactions)
            {
                Debug.Log($"[CreditScoreSystem] Statement generated. " +
                          $"Balance: ${_card.StatementBalance:F2}, " +
                          $"Interest: ${_card.InterestAccrued:F2}, " +
                          $"Min due: ${_card.MinimumPaymentDue:F2}");
            }

            GameEvents.RaiseCreditCardStatementReady(
                _card.StatementBalance,
                _card.MinimumPaymentDue,
                _card.InterestAccrued);
        }

        // ===============================================================
        // PAYMENT (called via GameEvents from popup)
        // ===============================================================

        /// <summary>
        /// Process a credit card payment. Deducts from checking via event,
        /// then applies payment to card balance.
        /// </summary>
        public void ProcessPayment(float amount)
        {
            if (_card == null || amount <= 0) return;

            float actualPaid = _card.ApplyPayment(amount);

            if (_logTransactions)
            {
                Debug.Log($"[CreditScoreSystem] Payment of ${actualPaid:F2} applied. " +
                          $"Remaining balance: ${_card.CurrentBalance:F2}");
            }

            GameEvents.RaiseCreditCardPaymentCompleted(actualPaid);
            GameEvents.RaiseCreditCardBalanceChanged(_card.CurrentBalance, -actualPaid);
        }

        // ===============================================================
        // CREDIT SCORE
        // ===============================================================

        /// <summary>
        /// Update credit score using the calculator.
        /// Called by MonthlyPaymentDayController after loan payment processing.
        /// Sources paidOnTime from LoanSystem (loan-payment history).
        /// </summary>
        public void UpdateCreditScore(float dti)
        {
            if (_scoringConfig == null) return;

            // No missed loan payments this cycle = on-time bonus.
            // Any missed payment = penalty. Defaults to true if LoanSystem
            // is unwired (cannot punish what we cannot measure).
            bool paidOnTime = _loanSystem == null || !_loanSystem.AnyLoanMissedThisCycle();

            int newScore = CreditScoreCalculator.Recalculate(
                _currentCreditScore,
                _scoringConfig,
                paidOnTime,
                dti
            );

            if (newScore != _currentCreditScore)
            {
                _currentCreditScore = newScore;
                GameEvents.RaiseCreditScoreChanged(_currentCreditScore);
            }
        }

        /// <summary>
        /// Set credit score directly (for state loading).
        /// </summary>
        public void SetCreditScore(int score)
        {
            _currentCreditScore = score;
        }

        /// <summary>
        /// Restore credit balance and score from a saved DTO.
        /// Fires both change events so UI components refresh.
        /// Public so EditMode tests can call directly without raising the event.
        /// </summary>
        public void Hydrate(GamePlayerStateDTO dto)
        {
            if (dto == null) return;
            if (_card != null)
            {
                _card.SetBalance(dto.credit_balance);
                GameEvents.RaiseCreditCardBalanceChanged(_card.CurrentBalance, 0f);
            }
            _currentCreditScore = dto.credit_score;
            GameEvents.RaiseCreditScoreChanged(_currentCreditScore);
        }
    }
}
