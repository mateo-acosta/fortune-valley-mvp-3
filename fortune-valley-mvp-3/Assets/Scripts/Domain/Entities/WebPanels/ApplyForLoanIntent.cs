using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from the HTML credit panel for a loan application.
    /// JsonUtility-deserialized by CreditWebBridge.RequestApplyForLoan.
    /// </summary>
    [Serializable]
    public class ApplyForLoanIntent
    {
        public string loanConfigId;     // LoanConfig.LoanId, e.g. "loan-30y"
        public string lotId;            // CityLotDefinition.LotId
        public float price;             // Lot's base cost (price of the purchase)
    }
}
