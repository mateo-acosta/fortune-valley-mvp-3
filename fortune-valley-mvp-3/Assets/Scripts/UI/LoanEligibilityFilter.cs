using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Evaluates loan eligibility locally in the UI layer using
    /// property-read data from LoanSystem and CreditCardSystem.
    /// No cross-layer method calls -- keeps architecture rules intact.
    ///
    /// LEARNING DESIGN: Students see which loans they qualify for
    /// and exactly what credit score or DTI ratio they need,
    /// making the connection between financial behavior and access to credit.
    /// </summary>
    public static class LoanEligibilityFilter
    {
        /// <summary>
        /// Evaluate each loan config against the player's current credit score and DTI ratio.
        /// Returns one result per config with eligibility and a human-readable reason.
        /// </summary>
        /// <param name="configs">Available loan configs (read from LoanSystem.AvailableLoans property)</param>
        /// <param name="creditScore">Player's current credit score (read from CreditCardSystem.CreditScore property)</param>
        /// <param name="dtiRatio">Current debt-to-income ratio (0.0 to 1.0+)</param>
        public static List<LoanEligibilityResult> Evaluate(
            IReadOnlyList<LoanConfig> configs, int creditScore, float dtiRatio)
        {
            if (configs == null)
                return new List<LoanEligibilityResult>();

            var results = new List<LoanEligibilityResult>(configs.Count);

            for (int i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config == null) continue;

                // Check credit score threshold
                if (creditScore < config.MinimumCreditScore)
                {
                    results.Add(new LoanEligibilityResult(
                        config,
                        false,
                        $"Requires credit score {config.MinimumCreditScore} (yours: {creditScore})"));
                    continue;
                }

                // Check debt-to-income ratio
                if (dtiRatio > config.MaxDtiRatio)
                {
                    results.Add(new LoanEligibilityResult(
                        config,
                        false,
                        $"DTI too high: {dtiRatio:P0} (max: {config.MaxDtiRatio:P0})"));
                    continue;
                }

                // Eligible
                results.Add(new LoanEligibilityResult(config, true, string.Empty));
            }

            return results;
        }
    }
}
