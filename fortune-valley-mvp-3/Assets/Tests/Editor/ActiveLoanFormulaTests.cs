using NUnit.Framework;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    // Locks the yearly amortization formula in ActiveLoan.CalculateMonthlyPayment.
    // Payments fire once per billing cycle (= 1 in-game year), so the formula
    // uses annual rate r and integer years n. Earlier code mixed monthly rate
    // with yearly exponent, producing roughly 7x inflated payments and an
    // unreachable DTI gate - the original bug these tests guard against.
    [TestFixture]
    public class ActiveLoanFormulaTests
    {
        // The tolerance is intentionally loose ($1) so float -> double rounding
        // does not flake the test. The formula is exact in double precision.
        private const float Tolerance = 1f;

        [Test]
        public void BugCase_45k_15pct_5y_ProducesYearlyPaymentAround13425()
        {
            float payment = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 5);
            Assert.AreEqual(13424.65f, payment, Tolerance);
        }

        [Test]
        public void ZeroApr_DividesPrincipalEvenly()
        {
            float payment = ActiveLoan.CalculateMonthlyPayment(45000f, 0f, 5);
            Assert.AreEqual(9000f, payment, Tolerance);
        }

        [Test]
        public void ZeroPrincipal_ReturnsZero()
        {
            Assert.AreEqual(0f, ActiveLoan.CalculateMonthlyPayment(0f, 0.05f, 5));
        }

        [Test]
        public void ZeroTerm_ReturnsZero()
        {
            Assert.AreEqual(0f, ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 0));
        }

        [Test]
        public void OnePeriod_PaysPrincipalPlusOneYearInterest()
        {
            float payment = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 1);
            Assert.AreEqual(51750f, payment, Tolerance);
        }

        [Test]
        public void ThirtyYearTerm_PaymentIsLowerThanFiveYearTerm()
        {
            float fiveYear = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 5);
            float thirtyYear = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 30);
            Assert.Less(thirtyYear, fiveYear);
            Assert.AreEqual(6852.61f, thirtyYear, Tolerance);
        }

        [Test]
        public void YearlyAlias_MatchesMonthlyMethod()
        {
            float a = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 5);
            float b = ActiveLoan.CalculateYearlyPayment(45000f, 0.15f, 5);
            Assert.AreEqual(a, b);
        }
    }
}
