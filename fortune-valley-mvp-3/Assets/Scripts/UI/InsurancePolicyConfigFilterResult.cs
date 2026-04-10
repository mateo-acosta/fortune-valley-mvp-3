using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Result of filtering an InsurancePolicyConfig for the Explore tab.
    /// Pairs the config with whether the player already owns this policy type
    /// on all their lots.
    /// </summary>
    public readonly struct InsurancePolicyConfigFilterResult
    {
        public InsurancePolicyConfig Config { get; }
        public bool IsFullyCovered { get; }

        public InsurancePolicyConfigFilterResult(InsurancePolicyConfig config, bool isFullyCovered)
        {
            Config = config;
            IsFullyCovered = isFullyCovered;
        }
    }
}
