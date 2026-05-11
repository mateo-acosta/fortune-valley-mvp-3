namespace FortuneValley.Core
{
    /// <summary>
    /// Pure C# calculator for credit score changes.
    /// Stateless: takes current state and config, returns new score.
    /// Separated from CreditScoreSystem for isolated testing.
    ///
    /// LEARNING DESIGN: Score factors mirror real lender behavior so
    /// students learn what actually affects their credit score.
    /// Two factors today (utilization removed when the credit-card
    /// charging mechanic was disabled):
    ///   1. Loan-payment history (on-time vs missed)
    ///   2. Debt-to-income ratio
    /// </summary>
    public static class CreditScoreCalculator
    {
        /// <summary>
        /// Recalculate credit score based on this cycle's activity.
        /// Called once per billing cycle by MonthlyPaymentDayController.
        /// </summary>
        /// <param name="currentScore">Score before this cycle's adjustment</param>
        /// <param name="config">Scoring rules (weights and thresholds)</param>
        /// <param name="paidOnTime">Did the player meet every loan payment this cycle?</param>
        /// <param name="dti">Debt-to-income ratio (total monthly loan debt / monthly income)</param>
        /// <returns>New clamped credit score</returns>
        public static int Recalculate(
            int currentScore,
            CreditScoringConfig config,
            bool paidOnTime,
            float dti)
        {
            int score = currentScore;

            // Payment history (biggest factor in real lending)
            if (paidOnTime)
            {
                score += config.OnTimePaymentBonus;
            }
            else
            {
                score -= config.MissedPaymentPenalty;
            }

            // Debt-to-income ratio
            if (dti > config.HighDtiThreshold)
            {
                score -= config.HighDtiPenalty;
            }

            // Clamp to valid range
            if (score < config.MinScore) score = config.MinScore;
            if (score > config.MaxScore) score = config.MaxScore;

            return score;
        }
    }
}
