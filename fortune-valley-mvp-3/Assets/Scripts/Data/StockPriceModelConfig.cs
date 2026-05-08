using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Shared tuning asset for the stochastic stock price model.
    /// One asset is referenced by every InvestmentDefinition so Game Feel
    /// changes can be made without recompiling.
    ///
    /// Per-risk tables are author-time tables for Low / Medium / High
    /// volatility classes. Global jump and clamp parameters apply to
    /// every Stock or ETF instrument regardless of risk.
    ///
    /// Default values match the inline constants previously baked into
    /// InvestmentDefinition.StepPrice so behavior is preserved if the
    /// config is left at defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "StockPriceModelConfig", menuName = "Fortune Valley/Stock Price Model Config")]
    public class StockPriceModelConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // PER-RISK PARAMETERS
        // ═══════════════════════════════════════════════════════════════

        [Header("Trend Reversal Chance (per day)")]
        [Tooltip("Daily probability that the persistent trend flips sign. Higher = more short-term mean reversion.")]
        [Range(0f, 1f)] [SerializeField] private float _reversalChanceLow = 0.05f;
        [Range(0f, 1f)] [SerializeField] private float _reversalChanceMedium = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float _reversalChanceHigh = 0.20f;

        [Header("Mean Reversion Strength")]
        [Tooltip("How strongly price is pulled toward the expected compound curve each day.")]
        [Range(0f, 0.2f)] [SerializeField] private float _mrStrengthLow = 0.05f;
        [Range(0f, 0.2f)] [SerializeField] private float _mrStrengthMedium = 0.02f;
        [Range(0f, 0.2f)] [SerializeField] private float _mrStrengthHigh = 0.01f;

        [Header("Soft Band (allowable deviation from expected)")]
        [Tooltip("Fractional deviation from expected price before cubic pull kicks in. 0.40 = 40% above or below.")]
        [Range(0f, 2f)] [SerializeField] private float _softBandLow = 0.15f;
        [Range(0f, 2f)] [SerializeField] private float _softBandMedium = 0.40f;
        [Range(0f, 2f)] [SerializeField] private float _softBandHigh = 0.80f;

        [Header("Trend Strength")]
        [Tooltip("Daily contribution of the persistent trend factor to the price change.")]
        [Range(0f, 0.05f)] [SerializeField] private float _trendStrengthLow = 0.002f;
        [Range(0f, 0.05f)] [SerializeField] private float _trendStrengthMedium = 0.006f;
        [Range(0f, 0.05f)] [SerializeField] private float _trendStrengthHigh = 0.012f;

        [Header("Daily Noise Sigma (Gaussian)")]
        [Tooltip("Standard deviation of the daily noise term. 0.015 = ~1.5% typical daily noise.")]
        [Range(0f, 0.1f)] [SerializeField] private float _noiseSigmaLow = 0.005f;
        [Range(0f, 0.1f)] [SerializeField] private float _noiseSigmaMedium = 0.015f;
        [Range(0f, 0.1f)] [SerializeField] private float _noiseSigmaHigh = 0.030f;

        // ═══════════════════════════════════════════════════════════════
        // GLOBAL PARAMETERS
        // ═══════════════════════════════════════════════════════════════

        [Header("Jump Events")]
        [Tooltip("Daily probability of a sudden price gap (0.01 = 1% of days).")]
        [Range(0f, 0.1f)] [SerializeField] private float _jumpDailyChance = 0.01f;

        [Tooltip("Smallest jump size as a fraction of price.")]
        [Range(0f, 0.5f)] [SerializeField] private float _jumpMinMagnitude = 0.03f;

        [Tooltip("Largest jump size as a fraction of price.")]
        [Range(0f, 0.5f)] [SerializeField] private float _jumpMaxMagnitude = 0.08f;

        [Tooltip("Probability that a jump is downward. 0.6 = 60% bearish, mimicking real-market negative skew.")]
        [Range(0f, 1f)] [SerializeField] private float _jumpDownProbability = 0.6f;

        [Header("Soft Clamp")]
        [Tooltip("Coefficient on the cubic mean-reversion pull applied beyond the soft band.")]
        [Range(0f, 5f)] [SerializeField] private float _cubicPullCoefficient = 0.5f;

        [Header("Absolute Floor")]
        [Tooltip("Hard lower bound as a fraction of base price. Prevents stocks from going to zero.")]
        [Range(0f, 1f)] [SerializeField] private float _absoluteFloorRatio = 0.2f;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public float JumpDailyChance => _jumpDailyChance;
        public float JumpMinMagnitude => _jumpMinMagnitude;
        public float JumpMaxMagnitude => _jumpMaxMagnitude;
        public float JumpDownProbability => _jumpDownProbability;
        public float CubicPullCoefficient => _cubicPullCoefficient;
        public float AbsoluteFloorRatio => _absoluteFloorRatio;

        public float GetReversalChance(RiskLevel risk) => risk switch
        {
            RiskLevel.Low    => _reversalChanceLow,
            RiskLevel.Medium => _reversalChanceMedium,
            RiskLevel.High   => _reversalChanceHigh,
            _                => _reversalChanceMedium
        };

        public float GetMrStrength(RiskLevel risk) => risk switch
        {
            RiskLevel.Low    => _mrStrengthLow,
            RiskLevel.Medium => _mrStrengthMedium,
            RiskLevel.High   => _mrStrengthHigh,
            _                => _mrStrengthMedium
        };

        public float GetSoftBand(RiskLevel risk) => risk switch
        {
            RiskLevel.Low    => _softBandLow,
            RiskLevel.Medium => _softBandMedium,
            RiskLevel.High   => _softBandHigh,
            _                => _softBandMedium
        };

        public float GetTrendStrength(RiskLevel risk) => risk switch
        {
            RiskLevel.Low    => _trendStrengthLow,
            RiskLevel.Medium => _trendStrengthMedium,
            RiskLevel.High   => _trendStrengthHigh,
            _                => _trendStrengthMedium
        };

        public float GetNoiseSigma(RiskLevel risk) => risk switch
        {
            RiskLevel.Low    => _noiseSigmaLow,
            RiskLevel.Medium => _noiseSigmaMedium,
            RiskLevel.High   => _noiseSigmaHigh,
            _                => _noiseSigmaMedium
        };
    }
}
