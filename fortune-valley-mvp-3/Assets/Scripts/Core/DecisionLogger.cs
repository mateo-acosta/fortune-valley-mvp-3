using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Subscribes to GameEvents and constructs decision DTOs for the FDLS.
    /// Enqueues each decision via APIClient for batched sending.
    /// Uses DecisionDTOBuilder for DRY DTO construction.
    /// </summary>
    public class DecisionLogger : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;

        private string _sessionId;
        private string _gameMode = "homebase";

        // Cached balances for running_balance in line items (Issue 8A)
        private float _cachedCheckingBalance;
        private float _cachedInvestingBalance;
        private float _cachedCreditBalance;

        /// <summary>
        /// Set the active session ID (received from server on session start).
        /// </summary>
        public void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
        }

        /// <summary>
        /// Set the current game mode for tagging decisions.
        /// </summary>
        public void SetGameMode(string gameMode)
        {
            _gameMode = gameMode;
        }

        private void OnEnable()
        {
            GameEvents.OnInvestmentCreated += HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnRivalUpgradedLot += HandleRivalUpgradedLot;
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardPaymentCompleted += HandleCreditCardPayment;
            GameEvents.OnInsurancePurchased += HandleInsurancePurchased;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnLoanPaymentMissed += HandleLoanPaymentMissed;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnQuestionAnswered += HandleQuestionAnswered;

            // Balance tracking for running_balance in line items
            GameEvents.OnCheckingBalanceChanged += HandleCheckingBalanceChanged;
            GameEvents.OnInvestingBalanceChanged += HandleInvestingBalanceChanged;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardBalanceChanged += HandleCreditBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCreated -= HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRivalUpgradedLot -= HandleRivalUpgradedLot;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardPaymentCompleted -= HandleCreditCardPayment;
            GameEvents.OnInsurancePurchased -= HandleInsurancePurchased;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnLoanPaymentMissed -= HandleLoanPaymentMissed;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnQuestionAnswered -= HandleQuestionAnswered;

            GameEvents.OnCheckingBalanceChanged -= HandleCheckingBalanceChanged;
            GameEvents.OnInvestingBalanceChanged -= HandleInvestingBalanceChanged;
            if (FeatureFlags.CreditCardChargesEnabled)
                GameEvents.OnCreditCardBalanceChanged -= HandleCreditBalanceChanged;
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private bool CanLog()
        {
            return _apiClient != null && _apiClient.CanPersist();
        }

        private DecisionDTOBuilder NewBuilder()
        {
            return new DecisionDTOBuilder(_sessionId, _gameMode);
        }

        private void TryLog(DecisionEventDTO dto)
        {
            _apiClient.EnqueueDecision(dto);
        }

        // ===============================================================
        // HANDLERS
        // ===============================================================

        private void HandleInvestmentCreated(ActiveInvestment inv)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("investment_buy")
                .Instrument(inv.Definition.DisplayName)
                .Amount(inv.Principal)
                .Day(inv.CreatedAtTick)
                .Category("investment")
                .AddLineItem("investing", inv.Principal, "outflow", _cachedInvestingBalance)
                .Build());
        }

        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("investment_sell")
                .Instrument(inv.Definition.DisplayName)
                .Amount(payout)
                .Day(inv.CreatedAtTick)
                .Category("investment")
                .AddLineItem("investing", payout, "inflow", _cachedInvestingBalance)
                .Build());
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (!CanLog()) return;

            string decisionType = owner == Owner.Player ? "lot_purchase" : "rival_lot_taken";
            string category = owner == Owner.Player ? "expense" : "event";

            TryLog(NewBuilder()
                .Type(decisionType)
                .Instrument(lotId)
                .Category(category)
                .Build());
        }

        private void HandleRivalUpgradedLot(string lotId, int newTier)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("rival_lot_upgraded")
                .Instrument(lotId)
                .Category("event")
                .Build());
        }

        private void HandleRestaurantUpgraded(int newLevel)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("franchise_upgrade")
                .Category("expense")
                .Build());
        }

        private void HandleCreditCardPayment(float amountPaid)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("cc_payment")
                .Amount(amountPaid)
                .Category("transfer")
                .AddLineItem("checking", amountPaid, "outflow", _cachedCheckingBalance)
                .AddLineItem("credit", amountPaid, "inflow", _cachedCreditBalance)
                .Build());
        }

        private void HandleInsurancePurchased(string lotId, string policyId)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("insurance_purchase")
                .Instrument(policyId)
                .Category("expense")
                .AddLineItem("credit", 0f, "outflow", _cachedCreditBalance)
                .Build());
        }

        private void HandleAccidentResolved(string lotId, string accidentName, float totalDamageCost, bool wasCovered, float playerCost)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("accident_occurred")
                .Instrument(lotId)
                .Amount(playerCost)
                .Category("event")
                .AddLineItem("credit", playerCost, "outflow", _cachedCreditBalance)
                .Build());
        }

        private void HandleLoanOriginated(ActiveLoan loan)
        {
            if (!CanLog()) return;

            // Loan proceeds now deposit into checking (no down payment). The line item
            // reflects the inflow so the server ledger stays consistent with balances.
            TryLog(NewBuilder()
                .Type("loan_taken")
                .Instrument(loan.LotId)
                .Amount(loan.Principal)
                .Category("transfer")
                .AddLineItem("checking", loan.Principal, "inflow", _cachedCheckingBalance)
                .Build());
        }

        private void HandleLoanPaymentMade(ActiveLoan loan, float amountPaid)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("loan_payment")
                .Instrument(loan.LotId)
                .Amount(amountPaid)
                .Category("expense")
                .AddLineItem("checking", amountPaid, "outflow", _cachedCheckingBalance)
                .Build());
        }

        private void HandleLoanPaymentMissed(ActiveLoan loan)
        {
            if (!CanLog()) return;

            // No money moved (insufficient funds) so no line items. Amount records
            // the scheduled payment so teachers can see what the student was short on.
            TryLog(NewBuilder()
                .Type("loan_payment_missed")
                .Instrument(loan.LotId)
                .Amount(loan.YearlyPayment)
                .Category("event")
                .AddMetaString("loan_id", loan.LoanId)
                .AddMetaString("lot_id", loan.LotId)
                .AddMetaInt("total_missed_payments", loan.MissedPayments)
                .Build());
        }

        private void HandleLoanPaidOff(ActiveLoan loan)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("loan_paid_off")
                .Instrument(loan.LotId)
                .Amount(0f)
                .Category("event")
                .AddMetaString("loan_id", loan.LoanId)
                .AddMetaString("lot_id", loan.LotId)
                .AddMetaFloat("original_principal", loan.Principal)
                .AddMetaInt("term_months", loan.TermYears)
                .AddMetaInt("months_to_payoff", loan.PaymentsMade)
                .Build());
        }

        private void HandleQuestionAnswered(QuestionData question, bool correct, int chosenIndex, int correctIndex, int currentStreak)
        {
            if (!CanLog()) return;
            if (question == null) return;

            // Timeout is encoded as chosenIndex == -1 per QuestionManager.ResolveAnswer.
            bool timedOut = chosenIndex == -1;

            TryLog(NewBuilder()
                .Type("quiz_answer")
                .Instrument(question.id)
                .QuizCategory(question.category)
                .Category("event")
                .AddMetaBool("correct", correct)
                .AddMetaInt("chosen_index", chosenIndex)
                .AddMetaInt("correct_index", correctIndex)
                .AddMetaInt("streak", currentStreak)
                .AddMetaBool("timed_out", timedOut)
                .Build());
        }

        // ===============================================================
        // BALANCE TRACKING
        // ===============================================================

        private void HandleCheckingBalanceChanged(float balance, float delta)
        {
            _cachedCheckingBalance = balance;
        }

        private void HandleInvestingBalanceChanged(float balance, float delta)
        {
            _cachedInvestingBalance = balance;
        }

        private void HandleCreditBalanceChanged(float balance, float delta)
        {
            _cachedCreditBalance = balance;
        }
    }
}
