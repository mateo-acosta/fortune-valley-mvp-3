using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Passive observer that subscribes to GameEvents and records
    /// financial transactions into a TransactionHistory buffer.
    /// Zero coupling to any manager -- managers do not know this exists.
    ///
    /// Place on the HomebaseSceneManager GameObject.
    /// History sub-panels read from this via [SerializeField] reference.
    /// </summary>
    public class TransactionLog : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Maximum number of transaction records to keep")]
        [SerializeField] private int _capacity = 100;

        private TransactionHistory _history;

        public TransactionHistory History => _history;

        private void Awake()
        {
            _history = new TransactionHistory(_capacity);
        }

        private void OnEnable()
        {
            // Loan events
            GameEvents.OnLoanOriginated += HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade += HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff += HandleLoanPaidOff;
            GameEvents.OnLoanPaymentMissed += HandleLoanPaymentMissed;

            // Credit card events
            GameEvents.OnCreditCardCharged += HandleCreditCardCharged;
            GameEvents.OnCreditCardPaymentCompleted += HandleCreditCardPaymentCompleted;

            // Insurance events
            GameEvents.OnInsurancePurchased += HandleInsurancePurchased;
            GameEvents.OnInsuranceCanceled += HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved += HandleAccidentResolved;
            GameEvents.OnInsurancePremiumCharged += HandleInsurancePremiumCharged;

            // Investment events
            GameEvents.OnInvestmentCreated += HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
        }

        private void OnDisable()
        {
            GameEvents.OnLoanOriginated -= HandleLoanOriginated;
            GameEvents.OnLoanPaymentMade -= HandleLoanPaymentMade;
            GameEvents.OnLoanPaidOff -= HandleLoanPaidOff;
            GameEvents.OnLoanPaymentMissed -= HandleLoanPaymentMissed;

            GameEvents.OnCreditCardCharged -= HandleCreditCardCharged;
            GameEvents.OnCreditCardPaymentCompleted -= HandleCreditCardPaymentCompleted;

            GameEvents.OnInsurancePurchased -= HandleInsurancePurchased;
            GameEvents.OnInsuranceCanceled -= HandleInsuranceCanceled;
            GameEvents.OnAccidentResolved -= HandleAccidentResolved;
            GameEvents.OnInsurancePremiumCharged -= HandleInsurancePremiumCharged;

            GameEvents.OnInvestmentCreated -= HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
        }

        // Loan handlers
        private void HandleLoanOriginated(ActiveLoan loan)
        {
            _history.Record(
                TransactionType.LoanOriginated,
                $"Loan originated: ${loan.Principal:N0} at {loan.APR * 100f:F1}% APR",
                loan.Principal,
                Time.frameCount);
        }

        private void HandleLoanPaymentMade(ActiveLoan loan, float amount)
        {
            _history.Record(
                TransactionType.LoanPayment,
                $"Loan payment: ${amount:N2}",
                amount,
                Time.frameCount);
        }

        private void HandleLoanPaidOff(ActiveLoan loan)
        {
            _history.Record(
                TransactionType.LoanPaidOff,
                $"Loan paid off (was ${loan.Principal:N0})",
                loan.Principal,
                Time.frameCount);
        }

        private void HandleLoanPaymentMissed(ActiveLoan loan)
        {
            _history.Record(
                TransactionType.LoanPaymentMissed,
                $"Loan payment missed: ${loan.MonthlyPayment:N2} due",
                loan.MonthlyPayment,
                Time.frameCount);
        }

        // Credit card handlers
        private void HandleCreditCardCharged(float amount)
        {
            _history.Record(
                TransactionType.CreditCardCharge,
                $"Credit card charge: ${amount:N2}",
                amount,
                Time.frameCount);
        }

        private void HandleCreditCardPaymentCompleted(float amount)
        {
            _history.Record(
                TransactionType.CreditCardPayment,
                $"Credit card payment: ${amount:N2}",
                amount,
                Time.frameCount);
        }

        // Insurance handlers
        private void HandleInsurancePurchased(string lotId, string policyId)
        {
            _history.Record(
                TransactionType.InsurancePurchased,
                $"Insurance purchased: {policyId} for lot {lotId}",
                0f,
                Time.frameCount,
                lotId);
        }

        private void HandleInsuranceCanceled(string lotId, InsurancePolicyType policyType)
        {
            _history.Record(
                TransactionType.InsuranceCanceled,
                $"Insurance canceled: {policyType} for lot {lotId}",
                0f,
                Time.frameCount,
                lotId);
        }

        private void HandleAccidentResolved(
            string lotId, string accidentName, float totalDamage, bool wasCovered, float playerCost)
        {
            string coverageNote = wasCovered ? "covered by insurance" : "uninsured";
            _history.Record(
                TransactionType.AccidentResolved,
                $"{accidentName} at {lotId}: ${totalDamage:N0} damage ({coverageNote}), you paid ${playerCost:N0}",
                playerCost,
                Time.frameCount,
                lotId);
        }

        private void HandleInsurancePremiumCharged(string lotId, string policyId, float amount)
        {
            _history.Record(
                TransactionType.PremiumCharged,
                $"Premium charged: {policyId} on {lotId}",
                amount,
                Time.frameCount,
                lotId);
        }

        // Investment handlers
        private void HandleInvestmentCreated(ActiveInvestment inv)
        {
            _history.Record(
                TransactionType.InvestmentBought,
                $"Bought {inv.NumberOfShares} share(s) of {inv.Definition.DisplayName}",
                inv.NumberOfShares * inv.AveragePurchasePrice,
                Time.frameCount);
        }

        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout)
        {
            _history.Record(
                TransactionType.InvestmentSold,
                $"Sold shares of {inv.Definition.DisplayName} for ${payout:N2}",
                payout,
                Time.frameCount);
        }
    }
}
