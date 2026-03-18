using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for InvestmentDefinition.SimulateHistory().
    /// Verifies determinism, isolation, variance ordering, and the ScriptableObject mutation guard.
    /// </summary>
    [TestFixture]
    public class InvestmentDefinitionSimulateHistoryTests
    {
        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private InvestmentDefinition CreateDef(
            RiskLevel risk, float annualReturn, float basePrice = 100f,
            InvestmentCategory category = InvestmentCategory.Stock,
            string name = "TestDef")
        {
            var def   = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var type  = typeof(InvestmentDefinition);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("_riskLevel",       flags).SetValue(def, risk);
            type.GetField("_annualReturnRate", flags).SetValue(def, annualReturn);
            type.GetField("_basePricePerShare",flags).SetValue(def, basePrice);
            type.GetField("_category",         flags).SetValue(def, category);
            type.GetField("_displayName",      flags).SetValue(def, name);

            return def;
        }

        // ═══════════════════════════════════════════════════════════════
        // TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void SimulateHistory_ReturnsExactRequestedLength()
        {
            var def = CreateDef(RiskLevel.Low, 0.05f);
            float[] result = def.SimulateHistory(30, 42);

            Assert.AreEqual(30, result.Length, "Should return exactly 30 entries");
            Object.DestroyImmediate(def);
        }

        [Test]
        public void SimulateHistory_SameSeed_IsDeterministic()
        {
            var def = CreateDef(RiskLevel.Medium, 0.10f);

            float[] run1 = def.SimulateHistory(30, 42);
            float[] run2 = def.SimulateHistory(30, 42);

            Assert.AreEqual(run1.Length, run2.Length);
            for (int i = 0; i < run1.Length; i++)
                Assert.AreEqual(run1[i], run2[i], 0.0001f,
                    $"Element {i} differed between identical-seed runs");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void SimulateHistory_DifferentSeeds_ProduceDifferentResults()
        {
            var def = CreateDef(RiskLevel.High, 0.15f);

            float[] run1 = def.SimulateHistory(30, 42);
            float[] run2 = def.SimulateHistory(30, 99);

            bool anyDifference = false;
            for (int i = 0; i < run1.Length; i++)
            {
                if (Mathf.Abs(run1[i] - run2[i]) > 0.0001f)
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifference, "Different seeds should produce different price sequences");
            Object.DestroyImmediate(def);
        }

        [Test]
        public void SimulateHistory_FixedReturn_IsNonDecreasing()
        {
            // Bonds/T-Bills follow a smooth compound curve — each price ≥ the previous
            var def = CreateDef(RiskLevel.Low, 0.05f, 100f, InvestmentCategory.Bond);

            float[] result = def.SimulateHistory(30, 42);

            for (int i = 1; i < result.Length; i++)
                Assert.GreaterOrEqual(result[i], result[i - 1] - 0.0001f,
                    $"Bond price at day {i} dropped unexpectedly: {result[i]} < {result[i - 1]}");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void SimulateHistory_HighRisk_HasGreaterDeviationThanLowRisk()
        {
            // High-risk stocks should show more price movement than low-risk over 30 days
            var lowDef  = CreateDef(RiskLevel.Low,  0.05f, 100f);
            var highDef = CreateDef(RiskLevel.High, 0.15f, 100f);

            float[] lowResult  = lowDef.SimulateHistory(30, 42);
            float[] highResult = highDef.SimulateHistory(30, 42);

            float lowRange  = MaxAbsDeviation(lowResult,  100f);
            float highRange = MaxAbsDeviation(highResult, 100f);

            Assert.Greater(highRange, lowRange,
                $"High-risk max deviation ({highRange:F2}) should exceed low-risk ({lowRange:F2})");

            Object.DestroyImmediate(lowDef);
            Object.DestroyImmediate(highDef);
        }

        [Test]
        public void SimulateHistory_AllValuesAbsoluteFloor()
        {
            // Absolute floor: 20% of base price — no value should go below this
            float basePrice = 100f;
            var def = CreateDef(RiskLevel.High, 0.01f, basePrice);

            float[] result = def.SimulateHistory(30, 42);
            float floor = basePrice * 0.2f;

            foreach (float price in result)
                Assert.GreaterOrEqual(price, floor,
                    $"Price {price} fell below absolute floor {floor}");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void SimulateHistory_DoesNotMutateCurrentPrice()
        {
            // SimulateHistory must be stateless — the ScriptableObject's CurrentPrice
            // should be unchanged after the call (critical mutation guard).
            var def = CreateDef(RiskLevel.High, 0.15f, 100f);
            def.InitializePrice();

            float priceBefore = def.CurrentPrice;
            def.SimulateHistory(30, 42);
            float priceAfter = def.CurrentPrice;

            Assert.AreEqual(priceBefore, priceAfter, 0.0001f,
                "SimulateHistory must not mutate CurrentPrice on the ScriptableObject");

            Object.DestroyImmediate(def);
        }

        // ═══════════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════════

        private static float MaxAbsDeviation(float[] prices, float basePrice)
        {
            float maxDev = 0f;
            foreach (float p in prices)
            {
                float dev = Mathf.Abs(p - basePrice);
                if (dev > maxDev) maxDev = dev;
            }
            return maxDev;
        }
    }
}
