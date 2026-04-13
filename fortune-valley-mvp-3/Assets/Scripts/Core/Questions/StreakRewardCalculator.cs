using System;

namespace FortuneValley.Core.Questions
{
    /// <summary>
    /// Pure reward math for the QuestionMaster streak. Ceiling-rounds the
    /// geometric-growth value to the nearest multiple of roundingStep.
    /// </summary>
    public static class StreakRewardCalculator
    {
        /// <summary>
        /// Compute reward for a given streak index (0 = first correct answer).
        /// Contract: roundingStep must be &gt;= 1. streak &lt; 0 is clamped to 0.
        /// </summary>
        public static int RewardForStreak(int streak, float baseReward, float multiplier, int roundingStep)
        {
            if (roundingStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundingStep), "roundingStep must be >= 1");
            }
            if (streak < 0) streak = 0;
            if (baseReward <= 0f) return 0;

            // Iterative: each step multiplies the previously-rounded value, then rounds again.
            // This matches the published sequence (25, 30, 35, 45, 55, 65, 75, 90, 105, 125...)
            // rather than base * mult^streak, which would give different values at larger streaks.
            int rounded = CeilToStep(baseReward, roundingStep);
            for (int i = 0; i < streak; i++)
            {
                double next = rounded * (double)multiplier;
                if (next <= 0d) return 0;
                rounded = CeilToStep(next, roundingStep);
            }
            return rounded;
        }

        private static int CeilToStep(double value, int step)
        {
            double quanta = value / step;
            int steps = (int)Math.Ceiling(quanta - 1e-6); // tolerate FP noise so exact step boundaries stay put
            return steps * step;
        }
    }
}
