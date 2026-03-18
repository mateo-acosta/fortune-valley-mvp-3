using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Statistical property tests for InvestmentDefinition price model.
    /// Verifies clamp bounds, fixed-return smoothness, risk-variance ordering,
    /// absolute floor, and no divergence.
    /// </summary>
    [TestFixture]
    public class PriceModelTests
    {
        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private InvestmentDefinition CreateDef(RiskLevel risk, float annualReturn,
            float basePrice = 100f, InvestmentCategory category = InvestmentCategory.Stock)
        {
            var def = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var type = typeof(InvestmentDefinition);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("_riskLevel", flags).SetValue(def, risk);
            type.GetField("_annualReturnRate", flags).SetValue(def, annualReturn);
            type.GetField("_basePricePerShare", flags).SetValue(def, basePrice);
            type.GetField("_category", flags).SetValue(def, category);
            type.GetField("_displayName", flags).SetValue(def, $"Test_{risk}");

            def.InitializePrice();
            return def;
        }

        private float RunPriceSimulation(InvestmentDefinition def, int days)
        {
            for (int i = 0; i < days; i++)
                def.UpdatePrice();
            return def.CurrentPrice;
        }

        // ═══════════════════════════════════════════════════════════════
        // CLAMP BOUNDS TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void LowRisk_365Days_PriceWithinClampBounds()
        {
            var def = CreateDef(RiskLevel.Low, 0.05f);
            float finalPrice = RunPriceSimulation(def, 365);

            // Expected: 100 * (1.05) = ~105 after 1 year
            // Low risk clamp: ±30% of expected
            float expected = 100f * Mathf.Pow(1.05f, 1f); // ~105
            float lower = expected * 0.70f;
            float upper = expected * 1.30f;

            Assert.GreaterOrEqual(finalPrice, lower,
                $"Low risk final price {finalPrice} below lower bound {lower}");
            Assert.LessOrEqual(finalPrice, upper,
                $"Low risk final price {finalPrice} above upper bound {upper}");
        }

        [Test]
        public void MediumRisk_365Days_PriceWithinClampBounds()
        {
            var def = CreateDef(RiskLevel.Medium, 0.10f);
            float finalPrice = RunPriceSimulation(def, 365);

            float expected = 100f * Mathf.Pow(1.10f, 1f); // ~110
            float lower = expected * 0.20f;
            float upper = expected * 1.80f;

            Assert.GreaterOrEqual(finalPrice, lower);
            Assert.LessOrEqual(finalPrice, upper);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void HighRisk_365Days_PriceWithinClampBounds()
        {
            var def = CreateDef(RiskLevel.High, 0.15f);
            float finalPrice = RunPriceSimulation(def, 365);

            float expected = 100f * Mathf.Pow(1.15f, 1f); // ~115
            // High risk allows wider deviation but absolute floor at 20% base
            float lower = Mathf.Max(expected * (1f - 1.50f), 100f * 0.2f);
            float upper = expected * (1f + 1.50f);

            Assert.GreaterOrEqual(finalPrice, lower);
            Assert.LessOrEqual(finalPrice, upper);
            Object.DestroyImmediate(def);
        }

        // ═══════════════════════════════════════════════════════════════
        // FIXED RETURN (BONDS/T-BILLS)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void HasFixedReturn_FollowsSmoothCompoundCurve()
        {
            var def = CreateDef(RiskLevel.Low, 0.05f, 100f, InvestmentCategory.Bond);

            // Run 365 days and check price matches expected compound curve exactly
            for (int day = 1; day <= 365; day++)
            {
                def.UpdatePrice();
                float dailyRate = Mathf.Pow(1.05f, 1f / 365f) - 1f;
                float expected = 100f * Mathf.Pow(1f + dailyRate, day);

                Assert.AreEqual(expected, def.CurrentPrice, 0.01f,
                    $"Bond price at day {day} deviated from expected compound curve");
            }

            Object.DestroyImmediate(def);
        }

        [Test]
        public void TBill_HasFixedReturn_NoRandomness()
        {
            // Run the same T-Bill twice — should produce identical prices
            var def1 = CreateDef(RiskLevel.Low, 0.03f, 50f, InvestmentCategory.TBill);
            var def2 = CreateDef(RiskLevel.Low, 0.03f, 50f, InvestmentCategory.TBill);

            for (int i = 0; i < 100; i++)
            {
                def1.UpdatePrice();
                def2.UpdatePrice();
                Assert.AreEqual(def1.CurrentPrice, def2.CurrentPrice, 0.001f);
            }

            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
        }

        // ═══════════════════════════════════════════════════════════════
        // VARIANCE ORDERING
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void LowRiskVariance_LessThan_HighRiskVariance()
        {
            int trials = 100;
            float[] lowPrices = new float[trials];
            float[] highPrices = new float[trials];

            for (int t = 0; t < trials; t++)
            {
                var lowDef = CreateDef(RiskLevel.Low, 0.05f);
                RunPriceSimulation(lowDef, 180);
                lowPrices[t] = lowDef.CurrentPrice;
                Object.DestroyImmediate(lowDef);

                var highDef = CreateDef(RiskLevel.High, 0.15f);
                RunPriceSimulation(highDef, 180);
                highPrices[t] = highDef.CurrentPrice;
                Object.DestroyImmediate(highDef);
            }

            float lowStdDev = StdDev(lowPrices);
            float highStdDev = StdDev(highPrices);

            Assert.Less(lowStdDev, highStdDev,
                $"Low risk stddev ({lowStdDev:F2}) should be less than high risk ({highStdDev:F2})");
        }

        // ═══════════════════════════════════════════════════════════════
        // ABSOLUTE FLOOR
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Price_NeverBelowAbsoluteFloor()
        {
            // High risk with low return to maximize downside pressure
            var def = CreateDef(RiskLevel.High, 0.01f, 100f);

            for (int i = 0; i < 1000; i++)
            {
                def.UpdatePrice();
                Assert.GreaterOrEqual(def.CurrentPrice, 100f * 0.2f,
                    $"Price fell below absolute floor at day {i + 1}");
            }

            Object.DestroyImmediate(def);
        }

        // ═══════════════════════════════════════════════════════════════
        // NO DIVERGENCE
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void NoDivergence_1000Iterations()
        {
            var def = CreateDef(RiskLevel.High, 0.15f, 100f);
            RunPriceSimulation(def, 1000);

            // After ~2.7 years at 15%, expected is ~100 * 1.15^2.74 ≈ ~148
            // With ±150% clamp, absolute range is [20, ~370]
            Assert.GreaterOrEqual(def.CurrentPrice, 20f);
            Assert.LessOrEqual(def.CurrentPrice, 1000f, "Price diverged unreasonably high");

            Object.DestroyImmediate(def);
        }

        // ═══════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════

        private static float StdDev(float[] values)
        {
            float sum = 0f;
            foreach (float v in values) sum += v;
            float mean = sum / values.Length;

            float sumSq = 0f;
            foreach (float v in values)
            {
                float diff = v - mean;
                sumSq += diff * diff;
            }
            return Mathf.Sqrt(sumSq / values.Length);
        }
    }
}
