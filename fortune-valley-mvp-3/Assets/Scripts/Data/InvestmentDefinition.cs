using UnityEngine;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines an investment type (e.g., Savings Account, Stocks, Bonds).
    /// Create one ScriptableObject per investment type in the game.
    ///
    /// LEARNING DESIGN: Each investment type should clearly represent
    /// a different risk/reward profile to teach risk vs return.
    /// </summary>
    [CreateAssetMenu(fileName = "NewInvestment", menuName = "Fortune Valley/Investment Definition")]
    public class InvestmentDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // DISPLAY INFO
        // ═══════════════════════════════════════════════════════════════

        [Header("Display")]
        [Tooltip("Name shown to player (e.g., 'Savings Account')")]
        [SerializeField] private string _displayName;

        [Tooltip("Short description for students")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // ═══════════════════════════════════════════════════════════════
        // FINANCIAL PARAMETERS
        // ═══════════════════════════════════════════════════════════════

        [Header("Category")]
        [Tooltip("Investment category for grouping (Stock, ETF, Bond, TBill)")]
        [SerializeField] private InvestmentCategory _category = InvestmentCategory.Stock;

        [Tooltip("Industry sector (only meaningful for Stocks)")]
        [SerializeField] private Industry _industry = Industry.None;

        [Header("Financial Settings")]
        [Tooltip("Low = stable, Medium = some variance, High = volatile")]
        [SerializeField] private RiskLevel _riskLevel = RiskLevel.Low;

        [Tooltip("Annual return rate (0.05 = 5% per year)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _annualReturnRate = 0.05f;

        [Tooltip("For risky investments: multiplier range for returns. 1.0 = no change. (0.5, 1.5) means -50% to +50% of expected return.")]
        [SerializeField] private Vector2 _volatilityRange = new Vector2(1f, 1f);

        [Tooltip("How many game ticks between compound events")]
        [SerializeField] private int _compoundingFrequency = 30; // ~30 days = monthly

        [Tooltip("How many compound periods per 'year' (affects rate calculation). 12 = monthly, 4 = quarterly.")]
        [SerializeField] private int _compoundsPerYear = 12;

        [Tooltip("Minimum amount player can invest (legacy, kept for compatibility)")]
        [SerializeField] private float _minimumDeposit = 100f;

        // ═══════════════════════════════════════════════════════════════
        // SHARE PRICE SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("Share Price")]
        [Tooltip("Starting price per share")]
        [SerializeField] private float _basePricePerShare = 50f;

        [Tooltip("Shared tuning asset for the stochastic price model. If left null, a runtime default with the same baked-in values is used.")]
        [SerializeField] private StockPriceModelConfig _priceModelConfig;

        // Lazy fallback so existing instrument assets keep working until the
        // shared config is wired. Created once per session, shared across all
        // InvestmentDefinitions that have no explicit config asset.
        private static StockPriceModelConfig _runtimeDefaultConfig;

        // Runtime price state (not serialized, reset at game start via InitializePrice)
        private float _currentPrice;
        private float _trendDirection;  // -1 to +1, persists between ticks for momentum
        private int _daysSinceStart;    // tick counter for compound expected price
        private bool _priceInitialized = false;
        private System.Random _dayRng;  // seeded RNG for deterministic prices

        // Seed multiplier for combining day number with definition name hash
        private const int SeedMultiplier = 31;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string DisplayName => _displayName;
        public string Description => _description;
        public InvestmentCategory Category => _category;
        public Industry Industry => _industry;

        /// <summary>
        /// Bonds and T-Bills have fixed (predictable) returns, unlike stocks/ETFs.
        /// </summary>
        public bool HasFixedReturn => _category == InvestmentCategory.Bond || _category == InvestmentCategory.TBill;
        public RiskLevel RiskLevel => _riskLevel;
        public float AnnualReturnRate => _annualReturnRate;
        public Vector2 VolatilityRange => _volatilityRange;
        public int CompoundingFrequency => _compoundingFrequency;
        public int CompoundsPerYear => _compoundsPerYear;
        public float MinimumDeposit => _minimumDeposit;
        public float BasePricePerShare => _basePricePerShare;

        /// <summary>
        /// Current fluctuating price per share.
        /// </summary>
        public float CurrentPrice
        {
            get
            {
                if (!_priceInitialized)
                    InitializePrice();
                return _currentPrice;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Get a student-friendly explanation of this investment type.
        /// </summary>
        public string GetExplanation()
        {
            string riskDesc = _riskLevel switch
            {
                RiskLevel.Low => "very safe but grows slowly",
                RiskLevel.Medium => "moderately risky with better potential returns",
                RiskLevel.High => "risky - could gain a lot or lose money",
                _ => "unknown risk"
            };

            return $"{_displayName}: {_description}\n" +
                   $"This investment is {riskDesc}.\n" +
                   $"Expected return: ~{_annualReturnRate * 100:F1}% per year.";
        }

        /// <summary>
        /// Calculate expected value after N ticks (for UI projections).
        /// Note: This is theoretical; actual returns may vary due to volatility.
        /// </summary>
        public float ProjectValue(float principal, int ticks)
        {
            // How many compound events in this period?
            int compoundEvents = ticks / _compoundingFrequency;

            if (compoundEvents == 0)
                return principal;

            // Rate per compound period
            float ratePerPeriod = _annualReturnRate / _compoundsPerYear;

            // Compound interest formula: P * (1 + r)^n
            return principal * Mathf.Pow(1f + ratePerPeriod, compoundEvents);
        }

        // ═══════════════════════════════════════════════════════════════
        // PRICE FLUCTUATION METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize price to base value. Call at game start.
        /// Uses a seed derived from the definition name for reproducible initial state.
        /// </summary>
        public void InitializePrice()
        {
            _currentPrice = _basePricePerShare;
            _dayRng = new System.Random(_displayName != null ? _displayName.GetHashCode() : 0);
            _trendDirection = (float)(_dayRng.NextDouble() * 2.0 - 1.0);
            _daysSinceStart = 0;
            _priceInitialized = true;
        }

        /// <summary>
        /// Set the RNG seed for today. Call once per in-game day before UpdatePrice.
        /// All students with the same day number see the same prices.
        /// </summary>
        public void SetDaySeed(int dayNumber)
        {
            // Combine day with definition name for per-instrument determinism
            int seed = dayNumber * SeedMultiplier + (_displayName != null ? _displayName.GetHashCode() : 0);
            _dayRng = new System.Random(seed);
        }

        /// <summary>
        /// Update price using mean-reverting model. Call each tick.
        /// Uses seeded System.Random for deterministic prices across all students.
        /// Call SetDaySeed() once per day before calling UpdatePrice().
        ///
        /// LEARNING DESIGN: Prices follow a compound growth path with realistic
        /// deviations. Low-risk investments hug the expected curve closely;
        /// high-risk investments deviate more but revert over time.
        /// Bonds/T-Bills follow smooth compound growth (no randomness).
        /// </summary>
        public void UpdatePrice()
        {
            if (!_priceInitialized)
                InitializePrice();

            _daysSinceStart++;

            StepPrice(
                ref _currentPrice, ref _trendDirection,
                _daysSinceStart, _dayRng);
        }

        /// <summary>
        /// Reset price to base (for game restart).
        /// Also resets trend direction to a deterministic starting value.
        /// </summary>
        public void ResetPrice()
        {
            _currentPrice = _basePricePerShare;
            _dayRng = new System.Random(_displayName != null ? _displayName.GetHashCode() : 0);
            _trendDirection = (float)(_dayRng.NextDouble() * 2.0 - 1.0);
            _daysSinceStart = 0;
            _priceInitialized = true;
        }

        /// <summary>
        /// Simulate N days of price history using this definition's price model.
        /// Operates on LOCAL copies only -- does not affect _currentPrice, _trendDirection,
        /// or _daysSinceStart. Returns array of length <paramref name="days"/>, oldest first.
        ///
        /// Uses System.Random (not UnityEngine.Random) to avoid corrupting global random state.
        /// </summary>
        public float[] SimulateHistory(int days, int seed)
        {
            var rng    = new System.Random(seed);
            var result = new float[days];

            // Local price state -- never touches the ScriptableObject's runtime fields
            float price = _basePricePerShare;
            float trend = (float)(rng.NextDouble() * 2.0 - 1.0);

            for (int i = 0; i < days; i++)
            {
                StepPrice(ref price, ref trend, i + 1, rng);
                result[i] = price;
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        // SHARED PRICE MODEL
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Single-step price update shared by UpdatePrice and SimulateHistory.
        /// Advances the price by one day. All randomness comes from the provided
        /// System.Random instance so charts stay deterministic across students.
        ///
        /// Model: drift + persistent trend + Gaussian noise + soft cubic mean
        /// reversion + rare jump events. Designed so low-risk assets look smooth
        /// and trending, high-risk assets look noisy and short-term mean-reverting.
        /// </summary>
        private void StepPrice(ref float price, ref float trend, int dayCount, System.Random rng)
        {
            float dailyGrowthRate = Mathf.Pow(1f + _annualReturnRate, 1f / 365f) - 1f;
            float expectedPrice = _basePricePerShare * Mathf.Pow(1f + dailyGrowthRate, dayCount);

            // Fixed-return instruments (bonds, T-bills): smooth compound curve, no noise
            if (HasFixedReturn)
            {
                price = expectedPrice;
                return;
            }

            StockPriceModelConfig cfg = ResolvedPriceModelConfig;

            // Step 1: Trend reversal. Inverted from the previous table to match
            // real-market behavior: low-vol assets carry momentum, high-vol assets
            // revert in the short run.
            float reversalChance = cfg.GetReversalChance(_riskLevel);
            if (rng.NextDouble() < reversalChance)
                trend = -Mathf.Sign(trend) * (float)(0.5 + rng.NextDouble() * 0.5);

            // Step 2: Soft mean reversion. Linear pull within a tolerance band,
            // cubic pull outside it. Replaces the old hard Mathf.Clamp so charts
            // never show a flat ceiling.
            float deviation = expectedPrice > 0 ? (price - expectedPrice) / expectedPrice : 0f;
            float mrStrength = cfg.GetMrStrength(_riskLevel);
            float softBand = cfg.GetSoftBand(_riskLevel);
            float linearPull = -deviation * mrStrength;
            float overshoot = Mathf.Max(0f, Mathf.Abs(deviation) - softBand);
            float cubicPull = -Mathf.Sign(deviation) * overshoot * overshoot * overshoot * cfg.CubicPullCoefficient;
            float meanReversion = linearPull + cubicPull;

            // Step 3: Trend contribution
            float trendContrib = trend * cfg.GetTrendStrength(_riskLevel);

            // Step 4: Gaussian noise scaled by risk. Replaces the old uniform
            // ±0.1% so daily moves have a realistic bell-curve distribution.
            float noise = SampleGaussian(rng) * cfg.GetNoiseSigma(_riskLevel);

            // Step 5: Rare jump events. Configurable daily probability of a
            // sudden price gap, with negative skew so jumps lean slightly
            // bearish (mimicking the asymmetry of real equity returns).
            float jump = 0f;
            if (rng.NextDouble() < cfg.JumpDailyChance)
            {
                float range = Mathf.Max(0f, cfg.JumpMaxMagnitude - cfg.JumpMinMagnitude);
                float magnitude = cfg.JumpMinMagnitude + (float)rng.NextDouble() * range;
                float direction = rng.NextDouble() < cfg.JumpDownProbability ? -1f : 1f;
                jump = magnitude * direction;
            }

            // Step 6: Combine and apply
            float dailyChange = dailyGrowthRate + meanReversion + trendContrib + noise + jump;
            price *= (1f + dailyChange);

            // Absolute floor as a fraction of base price. The soft cubic pull
            // above handles upside containment without a hard ceiling.
            price = Mathf.Max(price, _basePricePerShare * cfg.AbsoluteFloorRatio);
        }

        /// <summary>
        /// Returns the wired StockPriceModelConfig, or a lazily created
        /// shared default whose field defaults match the previously baked-in
        /// constants. Never returns null.
        /// </summary>
        private StockPriceModelConfig ResolvedPriceModelConfig
        {
            get
            {
                if (_priceModelConfig != null) return _priceModelConfig;
                if (_runtimeDefaultConfig == null)
                    _runtimeDefaultConfig = ScriptableObject.CreateInstance<StockPriceModelConfig>();
                return _runtimeDefaultConfig;
            }
        }

        /// <summary>
        /// Sample from a standard normal distribution N(0, 1) using the
        /// Box-Muller transform. Pure value-type math, no allocations.
        /// </summary>
        private static float SampleGaussian(System.Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) *
                           System.Math.Cos(2.0 * System.Math.PI * u2));
        }
    }
}
