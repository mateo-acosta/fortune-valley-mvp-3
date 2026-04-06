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
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
            GameEvents.OnCreditCardPaymentCompleted += HandleCreditCardPayment;
            GameEvents.OnInsurancePurchased += HandleInsurancePurchased;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCreated -= HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
            GameEvents.OnCreditCardPaymentCompleted -= HandleCreditCardPayment;
            GameEvents.OnInsurancePurchased -= HandleInsurancePurchased;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
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
                .AddLineItem("investing", inv.Principal, "outflow")
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
                .AddLineItem("investing", payout, "inflow")
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
                .AddLineItem("checking", amountPaid, "outflow")
                .AddLineItem("credit", amountPaid, "inflow")
                .Build());
        }

        private void HandleInsurancePurchased(string lotId, string policyId)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("insurance_purchase")
                .Instrument(policyId)
                .Category("expense")
                .AddLineItem("credit", 0f, "outflow")
                .Build());
        }

        private void HandleAccidentResolved(string lotId, string accidentId, bool wasCovered, float playerCost)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("accident_occurred")
                .Instrument(lotId)
                .Amount(playerCost)
                .Category("event")
                .AddLineItem("credit", playerCost, "outflow")
                .Build());
        }

        private void HandleLoanOriginated(ActiveLoan loan)
        {
            if (!CanLog()) return;

            TryLog(NewBuilder()
                .Type("loan_taken")
                .Instrument(loan.LotId)
                .Amount(loan.Principal)
                .Category("transfer")
                .AddLineItem("checking", -loan.DownPayment, "outflow")
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
                .AddLineItem("checking", amountPaid, "outflow")
                .Build());
        }
    }
}
