using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's active-loans list (Home tab).
    /// id is the underlying ActiveLoan.LoanId; lotName is the display
    /// name resolved via CityManager so the iframe can render without
    /// a second lookup.
    /// </summary>
    [Serializable]
    public class ActiveLoanRowDTO
    {
        public string id;                  // ActiveLoan.LoanId
        public string lotName;             // CityLotDefinition.DisplayName
        public float balance;              // remaining principal
        public float originalPrincipal;
        public float yearlyPayment;        // ActiveLoan.YearlyPayment (one payment per in-game year)
        public int yearsPaid;              // ActiveLoan.PaymentsMade
        public int termYears;              // ActiveLoan.TermYears (in-game years)
    }
}
