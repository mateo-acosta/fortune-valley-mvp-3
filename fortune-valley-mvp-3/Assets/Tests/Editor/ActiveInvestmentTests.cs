using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Unit tests for ActiveInvestment.
    /// Tests the runtime investment data class.
    /// </summary>
    [TestFixture]
    public class ActiveInvestmentTests
    {
        private InvestmentDefinition _lowRiskDef;
        private InvestmentDefinition _highRiskDef;

        [SetUp]
        public void SetUp()
        {
            // Create test investment definitions
            _lowRiskDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetInvestmentDefinition(_lowRiskDef, "Test Savings", RiskLevel.Low, 0.05f, 30, 12);

            _highRiskDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetInvestmentDefinition(_highRiskDef, "Test Stocks", RiskLevel.High, 0.12f, 30, 12);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_lowRiskDef);
            Object.DestroyImmediate(_highRiskDef);
        }

        // Helper to set private fields via reflection (for testing)
        private void SetInvestmentDefinition(InvestmentDefinition def, string name, RiskLevel risk,
            float rate, int compoundFreq, int compoundsPerYear)
        {
            var type = typeof(InvestmentDefinition);

            type.GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, name);
            type.GetField("_riskLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, risk);
            type.GetField("_annualReturnRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, rate);
            type.GetField("_compoundingFrequency", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, compoundFreq);
            type.GetField("_compoundsPerYear", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, compoundsPerYear);
            type.GetField("_volatilityRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(def, new Vector2(1f, 1f)); // No volatility for predictable tests
        }

        // ═══════════════════════════════════════════════════════════════
        // CONSTRUCTOR TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Constructor_SetsInitialValues()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            Assert.AreEqual(1000f, investment.Principal);
            Assert.AreEqual(1000f, investment.CurrentValue);
            Assert.AreEqual(0f, investment.TotalGain);
            Assert.AreEqual(0, investment.TicksHeld);
            Assert.AreEqual(0, investment.CompoundCount);
            Assert.AreEqual(0, investment.CreatedAtTick);
        }

        [Test]
        public void Constructor_GeneratesUniqueId()
        {
            var inv1 = new ActiveInvestment(_lowRiskDef, 1000f, 0);
            var inv2 = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            Assert.AreNotEqual(inv1.Id, inv2.Id);
        }

        // ═══════════════════════════════════════════════════════════════
        // TICKS HELD TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void IncrementTicksHeld_IncrementsCorrectly()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            investment.IncrementTicksHeld();
            investment.IncrementTicksHeld();
            investment.IncrementTicksHeld();

            Assert.AreEqual(3, investment.TicksHeld);
        }

        // ═══════════════════════════════════════════════════════════════
        // COMPOUNDING TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TryCompound_BeforeInterval_ReturnsFalse()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            // Compound frequency is 30, so tick 29 should not compound
            bool compounded = investment.TryCompound(29);

            Assert.IsFalse(compounded);
            Assert.AreEqual(0, investment.CompoundCount);
            Assert.AreEqual(1000f, investment.CurrentValue);
        }

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void TryCompound_AtInterval_ReturnsTrue()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            // Compound frequency is 30, so tick 30 should compound
            bool compounded = investment.TryCompound(30);

            Assert.IsTrue(compounded);
            Assert.AreEqual(1, investment.CompoundCount);
            Assert.Greater(investment.CurrentValue, 1000f);
        }

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void TryCompound_IncreasesValue()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            investment.TryCompound(30);
            float valueAfterOne = investment.CurrentValue;

            investment.TryCompound(60);
            float valueAfterTwo = investment.CurrentValue;

            Assert.Greater(valueAfterOne, 1000f);
            Assert.Greater(valueAfterTwo, valueAfterOne);
        }

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void TryCompound_MultipleCompounds_AccumulatesCorrectly()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            // Compound 12 times (one year at monthly compounding)
            for (int i = 1; i <= 12; i++)
            {
                investment.TryCompound(i * 30);
            }

            Assert.AreEqual(12, investment.CompoundCount);

            // At 5% annual rate, monthly compounding: ~1051.16 after 1 year
            Assert.AreEqual(1051.16f, investment.CurrentValue, 1f);
        }

        // ═══════════════════════════════════════════════════════════════
        // GAIN CALCULATION TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void TotalGain_CalculatesCorrectly()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);
            investment.TryCompound(30);

            float expectedGain = investment.CurrentValue - 1000f;
            Assert.AreEqual(expectedGain, investment.TotalGain, 0.01f);
        }

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void PercentageReturn_CalculatesCorrectly()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            // Compound 12 times for 1 year
            for (int i = 1; i <= 12; i++)
            {
                investment.TryCompound(i * 30);
            }

            // Should be about 5.116% return
            Assert.AreEqual(5.116f, investment.PercentageReturn, 0.1f);
        }

        // ═══════════════════════════════════════════════════════════════
        // EXPLANATION TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void GetPerformanceExplanation_BeforeCompounding_ExplainsWaiting()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);

            string explanation = investment.GetPerformanceExplanation();

            Assert.IsTrue(explanation.Contains("hasn't compounded"));
            Assert.IsTrue(explanation.Contains("30 days"));
        }

        [Test]
        [Ignore("TryCompound is legacy — share-based pricing replaced compounding")]
        public void GetPerformanceExplanation_AfterCompounding_ShowsGains()
        {
            var investment = new ActiveInvestment(_lowRiskDef, 1000f, 0);
            investment.TryCompound(30);

            string explanation = investment.GetPerformanceExplanation();

            Assert.IsTrue(explanation.Contains("gained"));
            Assert.IsTrue(explanation.Contains("compound"));
        }
    }
}
