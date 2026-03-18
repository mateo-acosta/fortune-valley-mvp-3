using NUnit.Framework;
using FortuneValley.Core;
using UnityEngine;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Unit tests for CompoundCalculator.
    /// These are pure math tests - no Unity dependencies needed.
    /// </summary>
    [TestFixture]
    public class CompoundCalculatorTests
    {
        // ═══════════════════════════════════════════════════════════════
        // FUTURE VALUE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void FutureValue_WithZeroPrincipal_ReturnsZero()
        {
            float result = CompoundCalculator.FutureValue(0f, 0.05f, 12, 1f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void FutureValue_WithZeroRate_ReturnsPrincipal()
        {
            float principal = 1000f;
            float result = CompoundCalculator.FutureValue(principal, 0f, 12, 1f);
            Assert.AreEqual(principal, result, 0.01f);
        }

        [Test]
        public void FutureValue_OneYearMonthlyCompounding_CalculatesCorrectly()
        {
            // $1000 at 5% compounded monthly for 1 year
            // Expected: 1000 * (1 + 0.05/12)^12 ≈ $1051.16
            float principal = 1000f;
            float rate = 0.05f;
            int compoundsPerYear = 12;
            float years = 1f;

            float result = CompoundCalculator.FutureValue(principal, rate, compoundsPerYear, years);

            Assert.AreEqual(1051.16f, result, 0.1f);
        }

        [Test]
        public void FutureValue_FiveYears_CompoundsCorrectly()
        {
            // $1000 at 10% compounded monthly for 5 years
            // Expected: 1000 * (1 + 0.10/12)^60 ≈ $1645.31
            float principal = 1000f;
            float rate = 0.10f;
            int compoundsPerYear = 12;
            float years = 5f;

            float result = CompoundCalculator.FutureValue(principal, rate, compoundsPerYear, years);

            Assert.AreEqual(1645.31f, result, 1f);
        }

        [Test]
        public void FutureValue_HigherCompoundingFrequency_YieldsMore()
        {
            float principal = 1000f;
            float rate = 0.10f;
            float years = 1f;

            float monthlyResult = CompoundCalculator.FutureValue(principal, rate, 12, years);
            float dailyResult = CompoundCalculator.FutureValue(principal, rate, 365, years);

            Assert.Greater(dailyResult, monthlyResult);
        }

        // ═══════════════════════════════════════════════════════════════
        // YEARS TO DOUBLE TESTS (Rule of 72)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void YearsToDouble_At10Percent_ReturnsApprox7Years()
        {
            // Rule of 72: 72/10 = 7.2 years
            float result = CompoundCalculator.YearsToDouble(0.10f);
            Assert.AreEqual(7.2f, result, 0.1f);
        }

        [Test]
        public void YearsToDouble_At6Percent_ReturnsApprox12Years()
        {
            // Rule of 72: 72/6 = 12 years
            float result = CompoundCalculator.YearsToDouble(0.06f);
            Assert.AreEqual(12f, result, 0.1f);
        }

        [Test]
        public void YearsToDouble_AtZeroRate_ReturnsInfinity()
        {
            float result = CompoundCalculator.YearsToDouble(0f);
            Assert.IsTrue(float.IsPositiveInfinity(result));
        }

        // ═══════════════════════════════════════════════════════════════
        // TOTAL INTEREST TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TotalInterestEarned_CalculatesCorrectly()
        {
            float principal = 1000f;
            float rate = 0.05f;
            int compoundsPerYear = 12;
            float years = 1f;

            float interest = CompoundCalculator.TotalInterestEarned(principal, rate, compoundsPerYear, years);

            // Expected: ~$51.16
            Assert.AreEqual(51.16f, interest, 0.1f);
        }

        // ═══════════════════════════════════════════════════════════════
        // COMPOUNDING ADVANTAGE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void CompoundingAdvantage_IsPositive()
        {
            float principal = 1000f;
            float rate = 0.10f;
            int compoundsPerYear = 12;
            float years = 5f;

            float advantage = CompoundCalculator.CompoundingAdvantage(principal, rate, compoundsPerYear, years);

            Assert.Greater(advantage, 0f);
        }

        [Test]
        public void CompoundingAdvantage_IncreasesOverTime()
        {
            float principal = 1000f;
            float rate = 0.10f;
            int compoundsPerYear = 12;

            float advantage1Year = CompoundCalculator.CompoundingAdvantage(principal, rate, compoundsPerYear, 1f);
            float advantage5Years = CompoundCalculator.CompoundingAdvantage(principal, rate, compoundsPerYear, 5f);
            float advantage10Years = CompoundCalculator.CompoundingAdvantage(principal, rate, compoundsPerYear, 10f);

            Assert.Greater(advantage5Years, advantage1Year);
            Assert.Greater(advantage10Years, advantage5Years);
        }

        // ═══════════════════════════════════════════════════════════════
        // TICKS TO YEARS CONVERSION
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TicksToYears_365Ticks_ReturnsOneYear()
        {
            float result = CompoundCalculator.TicksToYears(365, 365);
            Assert.AreEqual(1f, result, 0.001f);
        }

        [Test]
        public void TicksToYears_30Ticks_ReturnsApproxOneMonth()
        {
            float result = CompoundCalculator.TicksToYears(30, 365);
            Assert.AreEqual(30f / 365f, result, 0.001f);
        }
    }
}
