using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's loan-product carousel (Explore tab).
    /// Mirrors LoanConfig fields. The iframe's mockState includes
    /// optional 'image' and 'tagline' fields which we do not source
    /// from LoanConfig today; left null/empty so the HTML's existing
    /// fallback rendering applies.
    /// </summary>
    [Serializable]
    public class LoanProductDTO
    {
        public string id;                   // LoanConfig.LoanId
        public string name;                  // LoanConfig.DisplayName
        public float apr;                    // LoanConfig.APR rendered as percent (15 = 15%)
        public int termYears;                // LoanConfig.TermYears (in-game years)
        public float downPaymentPercent;     // LoanConfig.DownPaymentPercent (0..1)
        public int minCreditScore;           // LoanConfig.MinimumCreditScore
        public float maxDtiRatio;            // LoanConfig.MaxDtiRatio (0..1) - per-product DTI cap
        public string image;                 // not sourced today
        public string tagline;               // not sourced today
    }
}
