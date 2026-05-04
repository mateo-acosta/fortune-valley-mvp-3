using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// Wire payload from Unity to the HTML credit panel iframe.
    /// JsonUtility-serialized; field names match the iframe's mockState
    /// shape so the JS side can swap payload in for mock with no rename.
    /// </summary>
    [Serializable]
    public class CreditPanelDTO
    {
        // Home tab scalars
        public int creditScore;
        public string creditScoreLabel;     // computed bucket label, e.g. "Fair"
        public float ccBalance;
        public float ccLimit;
        public float ccAvailable;
        public float ccUtilization;         // 0..1
        public float totalDebt;
        public float monthlyDebtPayment;
        public float cashOnHand;            // checking balance

        // Lists
        public ActiveLoanRowDTO[] activeLoans;
        public AvailableLotDTO[] availableLots;
        public LoanProductDTO[] loanProducts;
        public HistoryEntryDTO[] history;
    }
}
