using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for LoanSystem event wiring and integration.
    /// Uses reflection to wire SerializeField dependencies.
    /// </summary>
    [TestFixture]
    public class LoanSystemTests
    {
        private GameObject _go;
        private LoanSystem _loanSystem;
        private CurrencyManager _currencyManager;
        private LoanConfig _basicConfig;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestLoanSystem");
            _loanSystem = _go.AddComponent<LoanSystem>();
            _currencyManager = _go.AddComponent<CurrencyManager>();

            SetPrivateField(_loanSystem, "_currencyManager", _currencyManager);

            _basicConfig = ScriptableObject.CreateInstance<LoanConfig>();
            SetPrivateField(_basicConfig, "_loanId", "basic_12m");
            SetPrivateField(_basicConfig, "_displayName", "Basic 12-Month");
            SetPrivateField(_basicConfig, "_apr", 0.10f);
            SetPrivateField(_basicConfig, "_termMonths", 12);
            SetPrivateField(_basicConfig, "_downPaymentPercent", 0.20f);
            SetPrivateField(_basicConfig, "_minimumCreditScore", 500);
            SetPrivateField(_basicConfig, "_maxDtiRatio", 0.40f);

            var configs = new System.Collections.Generic.List<LoanConfig> { _basicConfig };
            SetPrivateField(_loanSystem, "_availableLoans", configs);

            // Manually invoke OnEnable and simulate game start
            InvokePrivateMethod(_loanSystem, "OnEnable");
            _currencyManager.SetCheckingBalance(5000f);
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivateMethod(_loanSystem, "OnDisable");
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_basicConfig);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // LOAN ORIGINATION VIA EVENT
        // ===============================================================

        [Test]
        public void LoanPurchaseRequested_OriginatesLoan()
        {
            ActiveLoan originatedLoan = null;
            GameEvents.OnLoanOriginated += (loan) => originatedLoan = loan;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            Assert.IsNotNull(originatedLoan);
            Assert.AreEqual("lot1", originatedLoan.LotId);
            Assert.AreEqual(8000f, originatedLoan.Principal, 0.01f); // 10000 - 20%
        }

        [Test]
        public void LoanPurchaseRequested_DeductsDownPayment()
        {
            float balanceBefore = _currencyManager.CheckingBalance;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            // Down payment = 10000 * 0.20 = 2000
            Assert.AreEqual(balanceBefore - 2000f, _currencyManager.CheckingBalance, 0.01f);
        }

        [Test]
        public void LoanPurchaseRequested_InsufficientFundsForDownPayment_NoLoan()
        {
            _currencyManager.SetCheckingBalance(100f); // Not enough for $2000 down

            ActiveLoan originatedLoan = null;
            GameEvents.OnLoanOriginated += (loan) => originatedLoan = loan;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            Assert.IsNull(originatedLoan);
        }

        [Test]
        public void LoanPurchaseRequested_UnknownConfigId_NoLoan()
        {
            ActiveLoan originatedLoan = null;
            GameEvents.OnLoanOriginated += (loan) => originatedLoan = loan;

            GameEvents.RaiseLoanPurchaseRequested("nonexistent", "lot1", 10000f);

            Assert.IsNull(originatedLoan);
        }

        [Test]
        public void LoanPurchaseRequested_DuplicateLot_RefundsDownPayment()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);
            float balanceAfterFirst = _currencyManager.CheckingBalance;

            // Second loan on same lot should be rejected and refund the down payment
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            Assert.AreEqual(balanceAfterFirst, _currencyManager.CheckingBalance, 0.01f);
        }

        // ===============================================================
        // MONTHLY PAYMENTS
        // ===============================================================

        [Test]
        public void ProcessMonthlyPayments_DeductsFromChecking()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);
            float balanceAfterLoan = _currencyManager.CheckingBalance;

            _loanSystem.ProcessMonthlyPayments();

            Assert.Less(_currencyManager.CheckingBalance, balanceAfterLoan);
        }

        [Test]
        public void ProcessMonthlyPayments_FiresPaymentMadeEvent()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            bool paymentFired = false;
            GameEvents.OnLoanPaymentMade += (loan, amount) => paymentFired = true;

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsTrue(paymentFired);
        }

        [Test]
        public void ProcessMonthlyPayments_InsufficientFunds_FiresMissedEvent()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);
            _currencyManager.SetCheckingBalance(0f);

            bool missedFired = false;
            GameEvents.OnLoanPaymentMissed += (loan) => missedFired = true;

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsTrue(missedFired);
        }

        [Test]
        public void ProcessMonthlyPayments_PaidOff_FiresPaidOffEvent()
        {
            // Create a 1-month zero-APR loan for easy payoff
            var shortConfig = ScriptableObject.CreateInstance<LoanConfig>();
            SetPrivateField(shortConfig, "_loanId", "short");
            SetPrivateField(shortConfig, "_displayName", "Short");
            SetPrivateField(shortConfig, "_apr", 0f);
            SetPrivateField(shortConfig, "_termMonths", 1);
            SetPrivateField(shortConfig, "_downPaymentPercent", 0f);
            SetPrivateField(shortConfig, "_minimumCreditScore", 0);
            SetPrivateField(shortConfig, "_maxDtiRatio", 1f);

            var configs = new System.Collections.Generic.List<LoanConfig> { _basicConfig, shortConfig };
            SetPrivateField(_loanSystem, "_availableLoans", configs);

            GameEvents.RaiseLoanPurchaseRequested("short", "lot2", 500f);
            _currencyManager.SetCheckingBalance(1000f);

            bool paidOffFired = false;
            GameEvents.OnLoanPaidOff += (loan) => paidOffFired = true;

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsTrue(paidOffFired);

            Object.DestroyImmediate(shortConfig);
        }

        // ===============================================================
        // QUERY
        // ===============================================================

        [Test]
        public void GetQualifiedLoans_FiltersCorrectly()
        {
            var qualified = _loanSystem.GetQualifiedLoans(700, 0.2f);
            Assert.AreEqual(1, qualified.Count);

            // Below minimum credit score
            qualified = _loanSystem.GetQualifiedLoans(400, 0.2f);
            Assert.AreEqual(0, qualified.Count);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private void InvokePrivateMethod(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(obj, null);
        }
    }
}
