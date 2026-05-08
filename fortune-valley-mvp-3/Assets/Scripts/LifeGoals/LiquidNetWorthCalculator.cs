namespace FortuneValley.Core
{
    /// <summary>
    /// Pure-static formula for the player's Liquid Net Worth.
    ///
    ///   LiquidNW = Checking + Investing - LoanPrincipal - (CC_debt if enabled)
    ///
    /// The credit-card term only contributes when the CC charging mechanic is
    /// turned on; with CC disabled it is silently ignored. Both NetWorthService
    /// and InsolvencyMonitor pass through this calculator so the formula has
    /// one source of truth.
    ///
    /// LEARNING DESIGN: Liquid Net Worth is the player's "what could I cash
    /// out today" number. Investing in property raises Total NW but reduces
    /// Liquid NW until the property is sold; the difference teaches the
    /// concept of liquidity.
    /// </summary>
    public static class LiquidNetWorthCalculator
    {
        /// <summary>
        /// Compute Liquid Net Worth.
        /// </summary>
        /// <param name="checkingBalance">Cash available in the checking account.</param>
        /// <param name="investingBalance">Current value of the investment portfolio.</param>
        /// <param name="loanPrincipal">Outstanding principal across all active loans.</param>
        /// <param name="ccBalance">Outstanding credit card balance.</param>
        /// <param name="ccEnabled">Whether the CC charging mechanic is on. When false the ccBalance term is ignored.</param>
        public static float Compute(
            float checkingBalance,
            float investingBalance,
            float loanPrincipal,
            float ccBalance,
            bool ccEnabled)
        {
            float ccTerm = ccEnabled ? ccBalance : 0f;
            return checkingBalance + investingBalance - loanPrincipal - ccTerm;
        }
    }
}
