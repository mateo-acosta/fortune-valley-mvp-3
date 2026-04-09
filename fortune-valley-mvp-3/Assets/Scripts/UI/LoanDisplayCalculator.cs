using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI
{
    /// <summary>
    /// Computes display-ready loan values for a given lot price and loan config.
    /// Pure static math with no side effects. Used by CreditExploreSubPanel
    /// to populate the stats table without putting arithmetic in the MonoBehaviour.
    ///
    /// LEARNING DESIGN: Students see exactly how lot price, down payment,
    /// APR, and term combine to determine what they actually pay.
    /// </summary>
    public static class LoanDisplayCalculator
    {
        /// <summary>
        /// Calculate all display values for a loan product applied to a lot.
        /// </summary>
        /// <param name="lotPrice">The lot's BaseCost.</param>
        /// <param name="config">The loan product being evaluated.</param>
        public static LoanDisplayValues Calculate(float lotPrice, LoanConfig config)
        {
            float downPayment = lotPrice * config.DownPaymentPercent;
            float principal = lotPrice - downPayment;

            float monthlyPayment = ActiveLoan.CalculateMonthlyPayment(
                principal, config.APR, config.TermMonths);

            float totalCost = (monthlyPayment * config.TermMonths) + downPayment;
            float aprPercent = config.APR * 100f;

            return new LoanDisplayValues(
                principal,
                downPayment,
                monthlyPayment,
                totalCost,
                aprPercent,
                config.MinimumCreditScore,
                config.TermMonths);
        }
    }
}
