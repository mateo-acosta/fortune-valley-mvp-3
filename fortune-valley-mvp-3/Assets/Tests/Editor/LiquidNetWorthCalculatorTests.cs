using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LiquidNetWorthCalculatorTests
    {
        private const float Tolerance = 0.001f;

        [Test]
        public void TypicalPositive_ReturnsCheckingPlusInvestingMinusLoan()
        {
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 5_000f,
                investingBalance: 10_000f,
                loanPrincipal: 3_000f,
                ccBalance: 0f,
                ccEnabled: false);

            Assert.AreEqual(12_000f, result, Tolerance);
        }

        [Test]
        public void AllAssetsZeroDebts_ReturnsAssetSum()
        {
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 8_000f,
                investingBalance: 12_000f,
                loanPrincipal: 0f,
                ccBalance: 0f,
                ccEnabled: false);

            Assert.AreEqual(20_000f, result, Tolerance);
        }

        [Test]
        public void ZeroAssetsAllDebts_ReturnsNegative()
        {
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 0f,
                investingBalance: 0f,
                loanPrincipal: 50_000f,
                ccBalance: 1_500f,
                ccEnabled: true);

            Assert.AreEqual(-51_500f, result, Tolerance);
        }

        [Test]
        public void CcBalancePresent_FlagOn_SubtractsCcTerm()
        {
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 10_000f,
                investingBalance: 0f,
                loanPrincipal: 0f,
                ccBalance: 1_500f,
                ccEnabled: true);

            Assert.AreEqual(8_500f, result, Tolerance);
        }

        [Test]
        public void CcBalancePresent_FlagOff_IgnoresCcTerm()
        {
            // Same inputs as flag-on case; flag flipped should yield 10_000.
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 10_000f,
                investingBalance: 0f,
                loanPrincipal: 0f,
                ccBalance: 1_500f,
                ccEnabled: false);

            Assert.AreEqual(10_000f, result, Tolerance);
        }

        [Test]
        public void AllZeros_ReturnsZero()
        {
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: 0f,
                investingBalance: 0f,
                loanPrincipal: 0f,
                ccBalance: 0f,
                ccEnabled: false);

            Assert.AreEqual(0f, result, Tolerance);
        }

        [Test]
        public void NegativeChecking_OverdraftStillComputes()
        {
            // Overdrawn checking pulls liquid net worth even further down.
            float result = LiquidNetWorthCalculator.Compute(
                checkingBalance: -200f,
                investingBalance: 1_000f,
                loanPrincipal: 500f,
                ccBalance: 0f,
                ccEnabled: false);

            Assert.AreEqual(300f, result, Tolerance);
        }
    }
}
