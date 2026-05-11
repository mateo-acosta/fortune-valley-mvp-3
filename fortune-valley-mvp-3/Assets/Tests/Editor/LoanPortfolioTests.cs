using NUnit.Framework;
using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for LoanPortfolio and ActiveLoan.CalculateYearlyPayment.
    /// </summary>
    [TestFixture]
    public class LoanPortfolioTests
    {
        private LoanPortfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            _portfolio = new LoanPortfolio();
        }

        // ===============================================================
        // AMORTIZATION FORMULA
        // ===============================================================

        [Test]
        public void CalculateMonthlyPayment_StandardCase_ReturnsCorrectAmount()
        {
            // $10,000 at 12% APR for 12 in-game years (yearly amortization).
            // P * (r * (1+r)^n) / ((1+r)^n - 1) = ~$1,614.37/yr.
            float payment = ActiveLoan.CalculateYearlyPayment(10000f, 0.12f, 12);
            Assert.AreEqual(1614.37f, payment, 0.50f);
        }

        [Test]
        public void CalculateMonthlyPayment_ZeroAPR_ReturnsEqualDivision()
        {
            // $12,000 at 0% for 12 months = $1,000/month
            float payment = ActiveLoan.CalculateYearlyPayment(12000f, 0f, 12);
            Assert.AreEqual(1000f, payment, 0.01f);
        }

        [Test]
        public void CalculateMonthlyPayment_OneMonthTerm_ReturnsPrincipalPlusInterest()
        {
            // $1,000 at 12% for 1 in-game year = principal + 1 year of interest = $1,120.
            float payment = ActiveLoan.CalculateYearlyPayment(1000f, 0.12f, 1);
            Assert.AreEqual(1120f, payment, 0.50f);
        }

        [Test]
        public void CalculateMonthlyPayment_ZeroPrincipal_ReturnsZero()
        {
            float payment = ActiveLoan.CalculateYearlyPayment(0f, 0.12f, 12);
            Assert.AreEqual(0f, payment);
        }

        [Test]
        public void CalculateMonthlyPayment_ZeroTerm_ReturnsZero()
        {
            float payment = ActiveLoan.CalculateYearlyPayment(1000f, 0.12f, 0);
            Assert.AreEqual(0f, payment);
        }

        [Test]
        public void CalculateMonthlyPayment_NegativePrincipal_ReturnsZero()
        {
            float payment = ActiveLoan.CalculateYearlyPayment(-1000f, 0.12f, 12);
            Assert.AreEqual(0f, payment);
        }

        [Test]
        public void CalculateMonthlyPayment_TotalCostExceedsPrincipal()
        {
            // Any loan with APR > 0 should cost more than the principal
            float principal = 10000f;
            int term = 24;
            float payment = ActiveLoan.CalculateYearlyPayment(principal, 0.10f, term);
            float totalPaid = payment * term;
            Assert.Greater(totalPaid, principal);
        }

        // ===============================================================
        // ORIGINATION
        // ===============================================================

        [Test]
        public void Originate_CreatesLoanWithCorrectValues()
        {
            var loan = _portfolio.Originate("loan1", "lot1", 10000f, 0.08f, 12, 0.20f, 5);

            Assert.IsNotNull(loan);
            Assert.AreEqual("loan1", loan.LoanId);
            Assert.AreEqual("lot1", loan.LotId);
            Assert.AreEqual(8000f, loan.Principal, 0.01f); // 10000 - 20% down
            Assert.AreEqual(2000f, loan.DownPayment, 0.01f);
            Assert.AreEqual(0.08f, loan.APR);
            Assert.AreEqual(12, loan.TermYears);
            Assert.AreEqual(5, loan.StartTick);
            Assert.IsTrue(loan.IsActive);
            Assert.IsFalse(loan.IsPaidOff);
        }

        [Test]
        public void Originate_RejectsDuplicateLotLoan()
        {
            _portfolio.Originate("loan1", "lot1", 10000f, 0.08f, 12, 0.20f, 0);
            var duplicate = _portfolio.Originate("loan2", "lot1", 5000f, 0.10f, 6, 0.20f, 0);

            Assert.IsNull(duplicate);
            Assert.AreEqual(1, _portfolio.AllLoans.Count);
        }

        [Test]
        public void Originate_FullDownPayment_ReturnsNull()
        {
            // 100% down payment means zero principal = no loan needed
            var loan = _portfolio.Originate("loan1", "lot1", 10000f, 0.08f, 12, 1.0f, 0);
            Assert.IsNull(loan);
        }

        [Test]
        public void Originate_AllowsNewLoanAfterPaidOff()
        {
            var loan = _portfolio.Originate("loan1", "lot1", 1000f, 0f, 1, 0f, 0);
            // Pay it off
            loan.ApplyPayment();
            Assert.IsTrue(loan.IsPaidOff);

            // Should allow a new loan on the same lot
            var newLoan = _portfolio.Originate("loan2", "lot1", 2000f, 0f, 2, 0f, 0);
            Assert.IsNotNull(newLoan);
        }

        // ===============================================================
        // MONTHLY PAYMENTS
        // ===============================================================

        [Test]
        public void ProcessMonthlyPayments_DeductsFromChecking()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);

            float deducted = 0f;
            _portfolio.ProcessYearlyPayments(
                (amount, reason) => { deducted = amount; return true; },
                (loan, paid) => { },
                (loan) => { });

            Assert.AreEqual(100f, deducted, 0.01f); // 1000 / 10 months
        }

        [Test]
        public void ProcessMonthlyPayments_InsufficientFunds_RecordsMissed()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);

            bool missedFired = false;
            _portfolio.ProcessYearlyPayments(
                (amount, reason) => false, // insufficient funds
                (loan, paid) => { },
                (loan) => { missedFired = true; });

            Assert.IsTrue(missedFired);
            Assert.AreEqual(1, _portfolio.AllLoans[0].MissedPayments);
        }

        [Test]
        public void ProcessMonthlyPayments_SkipsPaidOffLoans()
        {
            var loan = _portfolio.Originate("loan1", "lot1", 100f, 0f, 1, 0f, 0);
            // Pay off in one payment
            _portfolio.ProcessYearlyPayments(
                (amount, reason) => true,
                (l, paid) => { },
                (l) => { });

            Assert.IsTrue(loan.IsPaidOff);

            // Second processing should not fire any callbacks
            int callbackCount = 0;
            _portfolio.ProcessYearlyPayments(
                (amount, reason) => { callbackCount++; return true; },
                (l, paid) => { callbackCount++; },
                (l) => { callbackCount++; });

            Assert.AreEqual(0, callbackCount);
        }

        [Test]
        public void ProcessMonthlyPayments_FinalPaymentCoversRemainder()
        {
            // Create a loan where monthly payment doesn't divide evenly
            var loan = _portfolio.Originate("loan1", "lot1", 1000f, 0.10f, 12, 0f, 0);
            float lastPaid = 0f;

            // Process 11 payments
            for (int i = 0; i < 11; i++)
            {
                _portfolio.ProcessYearlyPayments(
                    (amount, reason) => true,
                    (l, paid) => { lastPaid = paid; },
                    (l) => { });
            }

            // 12th payment should be the remainder
            _portfolio.ProcessYearlyPayments(
                (amount, reason) => true,
                (l, paid) => { lastPaid = paid; },
                (l) => { });

            Assert.IsTrue(loan.IsPaidOff);
            Assert.AreEqual(0f, loan.RemainingBalance, 0.01f);
        }

        // ===============================================================
        // QUERIES
        // ===============================================================

        [Test]
        public void GetTotalMonthlyDebt_SumsActiveLoans()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0); // $100/mo
            _portfolio.Originate("loan2", "lot2", 2000f, 0f, 10, 0f, 0); // $200/mo

            Assert.AreEqual(300f, _portfolio.GetTotalYearlyDebt(), 0.01f);
        }

        [Test]
        public void GetTotalOutstandingPrincipal_SumsRemainingBalances()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);
            _portfolio.Originate("loan2", "lot2", 2000f, 0f, 10, 0f, 0);

            Assert.AreEqual(3000f, _portfolio.GetTotalOutstandingPrincipal(), 0.01f);
        }

        [Test]
        public void GetLoanForLot_ReturnsActiveLoan()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);

            Assert.IsNotNull(_portfolio.GetLoanForLot("lot1"));
            Assert.IsNull(_portfolio.GetLoanForLot("lot2"));
        }

        [Test]
        public void HasLoanOnLot_ReturnsTrueForActiveLoan()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);

            Assert.IsTrue(_portfolio.HasLoanOnLot("lot1"));
            Assert.IsFalse(_portfolio.HasLoanOnLot("lot2"));
        }

        [Test]
        public void Clear_RemovesAllLoans()
        {
            _portfolio.Originate("loan1", "lot1", 1000f, 0f, 10, 0f, 0);
            _portfolio.Clear();

            Assert.AreEqual(0, _portfolio.AllLoans.Count);
            Assert.AreEqual(0f, _portfolio.GetTotalYearlyDebt());
        }

        // ===============================================================
        // STATIC HELPERS
        // ===============================================================

        [Test]
        public void GetAvailableLoans_FiltersByCreditScore()
        {
            var configs = CreateTestConfigs();

            // Credit score 700, DTI 0.2 -- should qualify for both
            var available = LoanPortfolio.GetAvailableLoans(configs, 700, 0.2f);
            Assert.AreEqual(2, available.Count);

            // Credit score 500 -- should only qualify for basic (min 500)
            available = LoanPortfolio.GetAvailableLoans(configs, 500, 0.2f);
            Assert.AreEqual(1, available.Count);
            Assert.AreEqual("basic", available[0].LoanId);
        }

        [Test]
        public void GetAvailableLoans_FiltersByDTI()
        {
            var configs = CreateTestConfigs();

            // DTI 0.5 exceeds both maxDtiRatio thresholds
            var available = LoanPortfolio.GetAvailableLoans(configs, 800, 0.5f);
            Assert.AreEqual(0, available.Count);
        }

        [Test]
        public void FindLoanConfig_ReturnsMatchById()
        {
            var configs = CreateTestConfigs();

            Assert.IsNotNull(LoanPortfolio.FindLoanConfig(configs, "basic"));
            Assert.IsNotNull(LoanPortfolio.FindLoanConfig(configs, "premium"));
            Assert.IsNull(LoanPortfolio.FindLoanConfig(configs, "nonexistent"));
        }

        [Test]
        public void GetAvailableLoans_NullConfigs_ReturnsEmpty()
        {
            var available = LoanPortfolio.GetAvailableLoans(null, 700, 0.2f);
            Assert.AreEqual(0, available.Count);
        }

        // ===============================================================
        // ACTIVELOAN ENTITY
        // ===============================================================

        [Test]
        public void ActiveLoan_TotalCost_IncludesDownPayment()
        {
            var loan = new ActiveLoan("l1", "lot1", 8000f, 0.10f, 12,
                ActiveLoan.CalculateYearlyPayment(8000f, 0.10f, 12), 2000f, 0);

            // Total cost = (monthlyPayment * 12) + downPayment
            Assert.Greater(loan.TotalCost, 10000f); // Must exceed purchase price
        }

        [Test]
        public void ActiveLoan_TotalInterest_PositiveForNonZeroAPR()
        {
            float principal = 8000f;
            float mp = ActiveLoan.CalculateYearlyPayment(principal, 0.10f, 12);
            var loan = new ActiveLoan("l1", "lot1", principal, 0.10f, 12, mp, 2000f, 0);

            Assert.Greater(loan.TotalInterest, 0f);
        }

        [Test]
        public void ActiveLoan_TotalInterest_ZeroForZeroAPR()
        {
            float principal = 8000f;
            float mp = ActiveLoan.CalculateYearlyPayment(principal, 0f, 12);
            var loan = new ActiveLoan("l1", "lot1", principal, 0f, 12, mp, 2000f, 0);

            Assert.AreEqual(0f, loan.TotalInterest, 0.01f);
        }

        [Test]
        public void ActiveLoan_PaymentsRemaining_DecreasesAfterPayment()
        {
            var loan = new ActiveLoan("l1", "lot1", 1000f, 0f, 10, 100f, 0f, 0);
            Assert.AreEqual(10, loan.PaymentsRemaining);

            loan.ApplyPayment();
            Assert.AreEqual(9, loan.PaymentsRemaining);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private List<LoanConfig> CreateTestConfigs()
        {
            var basic = UnityEngine.ScriptableObject.CreateInstance<LoanConfig>();
            SetPrivateField(basic, "_loanId", "basic");
            SetPrivateField(basic, "_displayName", "Basic Loan");
            SetPrivateField(basic, "_apr", 0.12f);
            SetPrivateField(basic, "_termYears", 12);
            SetPrivateField(basic, "_downPaymentPercent", 0.20f);
            SetPrivateField(basic, "_minimumCreditScore", 500);
            SetPrivateField(basic, "_maxDtiRatio", 0.40f);

            var premium = UnityEngine.ScriptableObject.CreateInstance<LoanConfig>();
            SetPrivateField(premium, "_loanId", "premium");
            SetPrivateField(premium, "_displayName", "Premium Loan");
            SetPrivateField(premium, "_apr", 0.06f);
            SetPrivateField(premium, "_termYears", 24);
            SetPrivateField(premium, "_downPaymentPercent", 0.10f);
            SetPrivateField(premium, "_minimumCreditScore", 650);
            SetPrivateField(premium, "_maxDtiRatio", 0.35f);

            return new List<LoanConfig> { basic, premium };
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
