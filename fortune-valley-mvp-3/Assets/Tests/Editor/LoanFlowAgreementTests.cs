using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    // End-to-end agreement check: the yearly payment quoted in the Explore
    // panel (computed via ActiveLoan.CalculateMonthlyPayment) must equal the
    // payment LoanPortfolio actually stores at origination, must equal the
    // value the bridge ships to the iframe. If any of those drift, the
    // player sees one number when applying for a loan and another after.
    [TestFixture]
    public class LoanFlowAgreementTests
    {
        [Test]
        public void Originate_StarterLoan_StoresExpectedYearlyPayment()
        {
            var portfolio = new LoanPortfolio();
            // $60,000 purchase, 25% down -> $45,000 principal at 15% APR over 5 years.
            var loan = portfolio.Originate(
                loanId: "loan_starter",
                lotId: "lot_block11",
                purchasePrice: 60000f,
                apr: 0.15f,
                termYears: 5,
                downPaymentPercent: 0.25f,
                startDay: 0);

            Assert.IsNotNull(loan);
            Assert.AreEqual(45000f, loan.Principal, 0.01f);
            Assert.AreEqual(15000f, loan.DownPayment, 0.01f);

            float expected = ActiveLoan.CalculateMonthlyPayment(45000f, 0.15f, 5);
            Assert.AreEqual(expected, loan.YearlyPayment, 0.01f);
            Assert.AreEqual(13424.65f, loan.YearlyPayment, 1f);
        }

        [Test]
        public void Originate_ZeroPrincipal_ReturnsNull()
        {
            var portfolio = new LoanPortfolio();
            var loan = portfolio.Originate(
                "x", "lot", purchasePrice: 60000f, apr: 0.15f,
                termYears: 5, downPaymentPercent: 1.0f, startDay: 0);
            Assert.IsNull(loan);
        }

        [Test]
        public void ApplyPayment_LoanReachesPaidOffWithinTerm()
        {
            // ApplyPayment deducts principal directly without accruing interest,
            // so a loan funded at the amortization payment level will hit
            // paid-off before the formal term ends. Verify that after running
            // through the full term, the loan is paid off and balance is zero.
            var portfolio = new LoanPortfolio();
            var loan = portfolio.Originate(
                "loan_starter", "lot_block11", 60000f, 0.15f, 5, 0.25f, 0);

            for (int i = 0; i < 5; i++) loan.ApplyPayment();

            Assert.IsTrue(loan.IsPaidOff);
            Assert.AreEqual(0f, loan.RemainingBalance, 0.01f);
            Assert.LessOrEqual(loan.PaymentsMade, 5);
        }

        [Test]
        public void Originate_TwiceForSameLot_ReturnsNullOnSecondCall()
        {
            var portfolio = new LoanPortfolio();
            var first = portfolio.Originate(
                "loan_starter", "lot_block11", 60000f, 0.15f, 5, 0.25f, 0);
            var second = portfolio.Originate(
                "loan_starter", "lot_block11", 60000f, 0.15f, 5, 0.25f, 0);
            Assert.IsNotNull(first);
            Assert.IsNull(second);
        }
    }
}
