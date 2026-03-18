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

        // Runtime price state (not serialized)
        private float _currentPrice;
        private float _trendDirection;  // -1 to +1, persists between ticks for momentum
        private int _daysSinceStart;    // tick counter for compound expected price
        private bool _priceInitialized = false;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string DisplayName => _displayName;
        public string Description => _description;
        public InvestmentCategory Category => _category;

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
        /// Randomizes initial trend direction so each investment starts differently.
        /// </summary>
        public void InitializePrice()
        {
            _currentPrice = _basePricePerShare;
            _trendDirection = Random.Range(-1f, 1f);
            _daysSinceStart = 0;
            _priceInitialized = true;
        }

        /// <summary>
        /// Update price using mean-reverting model. Call each tick.
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

            // Daily growth rate from annual return (compound basis)
            float dailyGrowthRate = Mathf.Pow(1f + _annualReturnRate, 1f / 365f) - 1f;

            // Expected price at this point in time (compound growth path)
            float expectedPrice = _basePricePerShare * Mathf.Pow(1f + dailyGrowthRate, _daysSinceStart);

            // Fixed-return instruments (bonds, T-bills): smooth compound curve, no noise
            if (HasFixedReturn)
            {
                _currentPrice = expectedPrice;
                return;
            }

            // Step 1: Maybe reverse trend direction
            float reversalChance = _riskLevel switch
            {
                RiskLevel.Low => 0.20f,
                RiskLevel.Medium => 0.10f,
                RiskLevel.High => 0.05f,
                _ => 0.10f
            };

            if (Random.value < reversalChance)
            {
                _trendDirection = -Mathf.Sign(_trendDirection) * Random.Range(0.5f, 1f);
            }

            // Step 2: Mean-reversion pull toward expected price
            float meanReversionStrength = _riskLevel switch
            {
                RiskLevel.Low => 0.05f,
                RiskLevel.Medium => 0.02f,
                RiskLevel.High => 0.01f,
                _ => 0.02f
            };
            float meanReversion = _currentPrice > 0
                ? (expectedPrice - _currentPrice) / _currentPrice * meanReversionStrength
                : 0f;

            // Step 3: Trend contribution (reduced from old model)
            float trendStrength = _riskLevel switch
            {
                RiskLevel.Low => 0.002f,    // 0.2%
                RiskLevel.Medium => 0.006f, // 0.6%
                RiskLevel.High => 0.012f,   // 1.2%
                _ => 0.006f
            };
            float trend = _trendDirection * trendStrength;

            // Step 4: Small random noise (±0.1%)
            float noise = Random.Range(-0.001f, 0.001f);

            // Step 5: Combine all factors
            float dailyChange = dailyGrowthRate + meanReversion + trend + noise;
            _currentPrice *= (1f + dailyChange);

            // Step 6: Clamp deviation from expected price
            float maxDeviation = _riskLevel switch
            {
                RiskLevel.Low => 0.30f,    // ±30%
                RiskLevel.Medium => 0.80f, // ±80%
                RiskLevel.High => 1.50f,   // ±150%
                _ => 0.80f
            };
            float lowerBound = expectedPrice * (1f - maxDeviation);
            float upperBound = expectedPrice * (1f + maxDeviation);
            _currentPrice = Mathf.Clamp(_currentPrice, lowerBound, upperBound);

            // Absolute floor: 20% of base price (never crash to near-zero)
            _currentPrice = Mathf.Max(_currentPrice, _basePricePerShare * 0.2f);
        }

        /// <summary>
        /// Reset price to base (for game restart).
        /// Also resets trend direction to a random starting value.
        /// </summary>
        public void ResetPrice()
        {
            _currentPrice = _basePricePerShare;
            _trendDirection = Random.Range(-1f, 1f);
            _daysSinceStart = 0;
            _priceInitialized = true;
        }

        /// <summary>
        /// Simulate N days of price history using this definition's price model.
        /// Operates on LOCAL copies only — does not affect _currentPrice, _trendDirection,
        /// or _daysSinceStart. Returns array of length <paramref name="days"/>, oldest first.
        ///
        /// Uses System.Random (not UnityEngine.Random) to avoid corrupting global random state.
        /// </summary>
        public float[] SimulateHistory(int days, int seed)
        {
            var rng    = new System.Random(seed);
            var result = new float[days];

            // Local price state — never touches the ScriptableObject's runtime fields
            float price    = _basePricePerShare;
            float trend    = (float)(rng.NextDouble() * 2.0 - 1.0); // -1 to +1
            int   dayCount = 0;

            float dailyGrowthRate = Mathf.Pow(1f + _annualReturnRate, 1f / 365f) - 1f;

            for (int i = 0; i < days; i++)
            {
                dayCount++;
                float expectedPrice = _basePricePerShare * Mathf.Pow(1f + dailyGrowthRate, dayCount);

                // Bonds/T-Bills: smooth compound curve, no noise
                if (HasFixedReturn)
                {
                    price = expectedPrice;
                }
                else
                {
                    // Step 1: Maybe reverse trend direction
                    float reversalChance = _riskLevel switch
                    {
                        RiskLevel.Low    => 0.20f,
                        RiskLevel.Medium => 0.10f,
                        RiskLevel.High   => 0.05f,
                        _                => 0.10f
                    };

                    if (rng.NextDouble() < reversalChance)
                        trend = -Mathf.Sign(trend) * (float)(0.5 + rng.NextDouble() * 0.5);

                    // Step 2: Mean-reversion pull toward expected price
                    float mrStrength = _riskLevel switch
                    {
                        RiskLevel.Low    => 0.05f,
                        RiskLevel.Medium => 0.02f,
                        RiskLevel.High   => 0.01f,
                        _                => 0.02f
                    };
                    float meanReversion = price > 0
                        ? (expectedPrice - price) / price * mrStrength
                        : 0f;

                    // Step 3: Trend contribution
                    float trendStrength = _riskLevel switch
                    {
                        RiskLevel.Low    => 0.002f,
                        RiskLevel.Medium => 0.006f,
                        RiskLevel.High   => 0.012f,
                        _                => 0.006f
                    };
                    float trendContrib = trend * trendStrength;

                    // Step 4: Small random noise (±0.1%)
                    float noise = (float)(rng.NextDouble() * 0.002 - 0.001);

                    // Step 5: Combine factors
                    float dailyChange = dailyGrowthRate + meanReversion + trendContrib + noise;
                    price *= (1f + dailyChange);

                    // Step 6: Clamp deviation from expected price
                    float maxDeviation = _riskLevel switch
                    {
                        RiskLevel.Low    => 0.30f,
                        RiskLevel.Medium => 0.80f,
                        RiskLevel.High   => 1.50f,
                        _                => 0.80f
                    };
                    float lowerBound = expectedPrice * (1f - maxDeviation);
                    float upperBound = expectedPrice * (1f + maxDeviation);
                    price = Mathf.Clamp(price, lowerBound, upperBound);

                    // Absolute floor: 20% of base price (never crash to near-zero)
                    price = Mathf.Max(price, _basePricePerShare * 0.2f);
                }

                result[i] = price;
            }

            return result;
        }
    }
}
