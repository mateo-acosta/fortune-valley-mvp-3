using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Result of evaluating a single LoanConfig against player credentials.
    /// Used by CreditExploreSubPanel to display eligible vs ineligible loans.
    /// </summary>
    public readonly struct LoanEligibilityResult
    {
        public LoanConfig Config { get; }
        public bool IsEligible { get; }

        /// <summary>
        /// Human-readable reason for ineligibility (empty if eligible).
        /// Displayed on greyed-out loan cards so students understand requirements.
        /// </summary>
        public string Reason { get; }

        public LoanEligibilityResult(LoanConfig config, bool isEligible, string reason)
        {
            Config = config;
            IsEligible = isEligible;
            Reason = reason;
        }
    }
}
