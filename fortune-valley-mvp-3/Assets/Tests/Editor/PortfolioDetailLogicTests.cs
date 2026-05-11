using NUnit.Framework;
using FortuneValley.UI.Panels.Investing;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for PortfolioDetailView's pure calculation methods.
    /// Tests position ratio, price change, and formatting logic.
    /// </summary>
    [TestFixture]
    public class PortfolioDetailLogicTests
    {
        // ─── Position Ratio ─────────────────────────────────────────────

        [Test]
        public void PositionRatio_SingleHolding_Returns100Percent()
        {
            var result = PortfolioDetailView.CalculatePositionRatio(500f, 500f);
            Assert.AreEqual("100.0%", result);
        }

        [Test]
        public void PositionRatio_TwoHoldings_ReturnsCorrectSplit()
        {
            // Holding is worth 250 out of 1000 total
            var result = PortfolioDetailView.CalculatePositionRatio(250f, 1000f);
            Assert.AreEqual("25.0%", result);
        }

        [Test]
        public void PositionRatio_ZeroPortfolioValue_ReturnsDash()
        {
            var result = PortfolioDetailView.CalculatePositionRatio(100f, 0f);
            Assert.AreEqual("--", result);
        }

        [Test]
        public void PositionRatio_NegativePortfolioValue_ReturnsDash()
        {
            var result = PortfolioDetailView.CalculatePositionRatio(100f, -50f);
            Assert.AreEqual("--", result);
        }

        [Test]
        public void PositionRatio_ZeroHoldingValue_ReturnsZeroPercent()
        {
            var result = PortfolioDetailView.CalculatePositionRatio(0f, 1000f);
            Assert.AreEqual("0.0%", result);
        }

        [Test]
        public void PositionRatio_SmallFraction_FormatsCorrectly()
        {
            // 10 out of 3000 = 0.333...%
            var result = PortfolioDetailView.CalculatePositionRatio(10f, 3000f);
            Assert.AreEqual("0.3%", result);
        }

        // ─── Price Change ───────────────────────────────────────────────

        [Test]
        public void PriceChange_PositiveChange_ReturnsPositive()
        {
            float result = PortfolioDetailView.CalculatePriceChangePercent(110f, 100f);
            Assert.AreEqual(10f, result, 0.01f);
        }

        [Test]
        public void PriceChange_NegativeChange_ReturnsNegative()
        {
            float result = PortfolioDetailView.CalculatePriceChangePercent(90f, 100f);
            Assert.AreEqual(-10f, result, 0.01f);
        }

        [Test]
        public void PriceChange_NoChange_ReturnsZero()
        {
            float result = PortfolioDetailView.CalculatePriceChangePercent(100f, 100f);
            Assert.AreEqual(0f, result, 0.01f);
        }

        [Test]
        public void PriceChange_ZeroPreviousPrice_ReturnsZero()
        {
            float result = PortfolioDetailView.CalculatePriceChangePercent(100f, 0f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void PriceChange_NegativePreviousPrice_ReturnsZero()
        {
            float result = PortfolioDetailView.CalculatePriceChangePercent(100f, -50f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void PriceChange_LargeIncrease_CalculatesCorrectly()
        {
            // Price doubled: 200/100 - 1 = 100%
            float result = PortfolioDetailView.CalculatePriceChangePercent(200f, 100f);
            Assert.AreEqual(100f, result, 0.01f);
        }
    }
}
