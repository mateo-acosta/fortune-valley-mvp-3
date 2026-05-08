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
        // Home tab scalars. Loans-only since the credit-card mechanic was
        // disabled; cc{Balance,Limit,Available,Utilization} were removed
        // from this payload along with the corresponding HTML widgets.
        public int creditScore;
        public string creditScoreLabel;     // computed bucket label, e.g. "Fair"
        public float totalDebt;
        public float monthlyDebtPayment;
        public float cashOnHand;            // checking balance

        // Lists
        public ActiveLoanRowDTO[] activeLoans;
        public AvailableLotDTO[] availableLots;
        public LoanProductDTO[] loanProducts;
        public HistoryEntryDTO[] history;

        // One-shot pre-selection signal. The bridge sets this when
        // OnLotLoanExploreRequested fires (e.g. lot click -> "buy on loan")
        // so the iframe lands on the Explore tab with the requested lot
        // already highlighted. Empty string / null means no pre-selection.
        // Bridge clears it after the push so the next push does not re-fire.
        public string selectedLotId;
    }
}
