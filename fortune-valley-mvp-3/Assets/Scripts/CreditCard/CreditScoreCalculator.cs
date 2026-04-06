namespace FortuneValley.Core
{
    /// <summary>
    /// Pure C# calculator for credit score changes.
    /// Stateless: takes current state and config, returns new score.
    /// Separated from CreditCardSystem for isolated testing.
    ///
    /// LEARNING DESIGN: Score factors mirror real FICO components
    /// so students learn what actually affects their credit score.
    /// </summary>
    public static class CreditScoreCalculator
    {
        /// <summary>
        /// Recalculate credit score based on this month's activity.
        /// Called once per billing cycle by MonthlyPaymentDayController.
        /// </summary>
        /// <param name="currentScore">Score before this month's adjustment</param>
        /// <param name="config">Scoring rules (weights and thresholds)</param>
        /// <param name="paidOnTime">Did the player meet minimum payment this cycle?</param>
        /// <param name="utilization">Current credit utilization ratio (0 to 1+)</param>
        /// <param name="dti">Debt-to-income ratio (total monthly debt / monthly income)</param>
        /// <returns>New clamped credit score</returns>
        public static int Recalculate(
            int currentScore,
            CreditScoringConfig config,
            bool paidOnTime,
            float utilization,
            float dti)
        {
            int score = currentScore;

            // Payment history (biggest factor in real FICO)
            if (paidOnTime)
            {
                score += config.OnTimePaymentBonus;
            }
            else
            {
                score -= config.MissedPaymentPenalty;
            }

            // Utilization (second biggest factor)
            if (utilization <= config.LowUtilizationThreshold)
            {
                score += config.LowUtilizationBonus;
            }
            else if (utilization >= config.HighUtilizationThreshold)
            {
                score -= config.HighUtilizationPenalty;
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
