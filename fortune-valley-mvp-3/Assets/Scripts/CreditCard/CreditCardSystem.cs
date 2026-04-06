using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Manages the player's credit card account.
    /// Subscribes to charge requests, tracks balance, generates statements.
    ///
    /// LEARNING DESIGN: Every purchase goes on credit. Students must
    /// actively decide how much to pay each month. Carrying a balance
    /// costs real money (interest), making the cost of debt visible.
    /// </summary>
    public class CreditCardSystem : MonoBehaviour
    {
        // ===============================================================
        // CONFIGURATION
        // ===============================================================

        [Header("Config")]
        [SerializeField] private CreditCardConfig _config;
        [SerializeField] private CreditScoringConfig _scoringConfig;

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
        /// Access payment history for credit score calculation.
        /// </summary>
        public ActiveCreditCard Card => _card;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnCreditCardChargeRequested += HandleChargeRequested;
            GameEvents.OnDayEnd += HandleDayEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnCreditCardChargeRequested -= HandleChargeRequested;
            GameEvents.OnDayEnd -= HandleDayEnd;
        }

        private void Start()
        {
            if (_config == null)
                Debug.LogError("[CreditCardSystem] _config not wired in Inspector.");
            if (_scoringConfig == null)
                Debug.LogError("[CreditCardSystem] _scoringConfig not wired in Inspector.");
        }

        private void HandleGameStart()
        {
            _card = new ActiveCreditCard();
            _currentCreditScore = _scoringConfig != null ? _scoringConfig.StartingScore : 0;
        }

        // ===============================================================
        // CHARGE HANDLING
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
                {
                    Debug.Log($"[CreditCardSystem] Charged ${amount:F2} for {reason}. Balance: ${_card.CurrentBalance:F2}");
                }

                GameEvents.RaiseCreditCardCharged(amount);
            }
            else
            {
                if (_logTransactions)
                {
                    Debug.Log($"[CreditCardSystem] Charge of ${amount:F2} for {reason} DECLINED. " +
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
        /// </summary>
        public void GenerateStatement()
        {
            if (_card == null || _config == null) return;

            _card.CloseStatement(
                _config.APR,
                _config.MinimumPaymentPercent,
                _config.MinimumPaymentFloor
            );

            if (_logTransactions)
            {
                Debug.Log($"[CreditCardSystem] Statement generated. " +
                          $"Balance: ${_card.StatementBalance:F2}, " +
                          $"Interest: ${_card.InterestAccrued:F2}, " +
                          $"Min due: ${_card.MinimumPaymentDue:F2}");
            }

            GameEvents.RaiseCreditCardStatementReady();
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
                Debug.Log($"[CreditCardSystem] Payment of ${actualPaid:F2} applied. " +
                          $"Remaining balance: ${_card.CurrentBalance:F2}");
            }

            GameEvents.RaiseCreditCardPaymentCompleted(actualPaid);
        }

        // ===============================================================
        // CREDIT SCORE
        // ===============================================================

        /// <summary>
        /// Update credit score using the calculator.
        /// Called by MonthlyPaymentDayController after payment processing.
        /// </summary>
        public void UpdateCreditScore(float dti)
        {
            if (_card == null || _scoringConfig == null) return;

            // Check if the most recent payment was on time
            bool paidOnTime = _card.PaymentHistory.Count > 0 && _card.PaymentHistory[0];
            float utilization = _config != null ? _card.Utilization(_config.CreditLimit) : 0f;

            int newScore = CreditScoreCalculator.Recalculate(
                _currentCreditScore,
                _scoringConfig,
                paidOnTime,
                utilization,
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
    }
}
