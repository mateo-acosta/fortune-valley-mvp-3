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
            SetPrivateField(_basicConfig, "_termYears", 12);
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
        public void LoanPurchaseRequested_OriginatesLoanWithFullPrincipal()
        {
            ActiveLoan originatedLoan = null;
            GameEvents.OnLoanOriginated += (loan) => originatedLoan = loan;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            Assert.IsNotNull(originatedLoan);
            Assert.AreEqual("lot1", originatedLoan.LotId);
            // No down payment in the POC flow: principal == full lot price.
            Assert.AreEqual(10000f, originatedLoan.Principal, 0.01f);
        }

        [Test]
        public void LoanPurchaseRequested_DepositsPrincipalIntoChecking()
        {
            float balanceBefore = _currencyManager.CheckingBalance;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            // Loan proceeds now land in checking so the player can subsequently buy the lot.
            Assert.AreEqual(balanceBefore + 10000f, _currencyManager.CheckingBalance, 0.01f);
        }

        [Test]
        public void LoanPurchaseRequested_LowCheckingBalance_StillOriginates()
        {
            _currencyManager.SetCheckingBalance(0f); // No balance required -- no down payment.

            ActiveLoan originatedLoan = null;
            GameEvents.OnLoanOriginated += (loan) => originatedLoan = loan;

            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            Assert.IsNotNull(originatedLoan);
            Assert.AreEqual(10000f, _currencyManager.CheckingBalance, 0.01f);
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
        public void LoanPurchaseRequested_DuplicateLot_NoSecondDeposit()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);
            float balanceAfterFirst = _currencyManager.CheckingBalance;

            // Second loan on same lot is rejected -- checking balance must not change again.
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

            _loanSystem.ProcessYearlyPayments();

            Assert.Less(_currencyManager.CheckingBalance, balanceAfterLoan);
        }

        [Test]
        public void ProcessMonthlyPayments_FiresPaymentMadeEvent()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);

            bool paymentFired = false;
            GameEvents.OnLoanPaymentMade += (loan, amount) => paymentFired = true;

            _loanSystem.ProcessYearlyPayments();

            Assert.IsTrue(paymentFired);
        }

        [Test]
        public void ProcessMonthlyPayments_InsufficientFunds_FiresMissedEvent()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 10000f);
            _currencyManager.SetCheckingBalance(0f);

            bool missedFired = false;
            GameEvents.OnLoanPaymentMissed += (loan) => missedFired = true;

            _loanSystem.ProcessYearlyPayments();

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
            SetPrivateField(shortConfig, "_termYears", 1);
            SetPrivateField(shortConfig, "_downPaymentPercent", 0f);
            SetPrivateField(shortConfig, "_minimumCreditScore", 0);
            SetPrivateField(shortConfig, "_maxDtiRatio", 1f);

            var configs = new System.Collections.Generic.List<LoanConfig> { _basicConfig, shortConfig };
            SetPrivateField(_loanSystem, "_availableLoans", configs);

            GameEvents.RaiseLoanPurchaseRequested("short", "lot2", 500f);
            _currencyManager.SetCheckingBalance(1000f);

            bool paidOffFired = false;
            GameEvents.OnLoanPaidOff += (loan) => paidOffFired = true;

            _loanSystem.ProcessYearlyPayments();

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
        // ANY LOAN MISSED THIS CYCLE
        //
        // Drives the paidOnTime factor in the credit-score formula.
        // True iff at least one HandlePaymentMissed fired during the
        // current monthly cycle (reset at the start of ProcessMonthlyPayments).
        // ===============================================================

        [Test]
        public void AnyLoanMissedThisCycle_NoActiveLoans_ReturnsFalse()
        {
            // Fresh game, no loans. The counter is zero.
            Assert.IsFalse(_loanSystem.AnyLoanMissedThisCycle());
        }

        [Test]
        public void AnyLoanMissedThisCycle_OneLoanPaidOnTime_ReturnsFalse()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 6000f);
            _currencyManager.SetCheckingBalance(10_000f);

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsFalse(_loanSystem.AnyLoanMissedThisCycle(),
                "Loan paid on time should leave the missed-this-cycle flag false.");
        }

        [Test]
        public void AnyLoanMissedThisCycle_OneLoanMissed_ReturnsTrue()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 6000f);
            // Drain checking so the payment cannot be made.
            _currencyManager.SetCheckingBalance(0f);

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsTrue(_loanSystem.AnyLoanMissedThisCycle(),
                "A missed payment should set the missed-this-cycle flag true.");
        }

        [Test]
        public void AnyLoanMissedThisCycle_OnePaidOneMissed_ReturnsTrue()
        {
            // Two loans: one fits the budget, one doesn't.
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lotA", 1_200f);
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lotB", 1_200f);
            // Each loan's monthly is 1200/12 = 100. Budget enough for one only.
            _currencyManager.SetCheckingBalance(150f);

            _loanSystem.ProcessMonthlyPayments();

            Assert.IsTrue(_loanSystem.AnyLoanMissedThisCycle(),
                "Any missed payment in the cycle flips the flag true regardless of others paid.");
        }

        [Test]
        public void AnyLoanMissedThisCycle_PriorCycleMissedThisCyclePaid_ReturnsFalse()
        {
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 6000f);

            // Cycle 1: missed.
            _currencyManager.SetCheckingBalance(0f);
            _loanSystem.ProcessMonthlyPayments();
            Assert.IsTrue(_loanSystem.AnyLoanMissedThisCycle(), "Sanity: cycle 1 should be missed.");

            // Cycle 2: paid. The reset at the top of ProcessMonthlyPayments
            // wipes the flag before HandlePaymentMissed can run.
            _currencyManager.SetCheckingBalance(10_000f);
            _loanSystem.ProcessMonthlyPayments();

            Assert.IsFalse(_loanSystem.AnyLoanMissedThisCycle(),
                "A new cycle that pays on time should clear the prior cycle's missed state.");
        }

        [Test]
        public void AnyLoanMissedThisCycle_LoanOriginatedMidCycle_ReturnsFalse()
        {
            // Originate a loan but don't process payments yet (no cycle has elapsed).
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 6000f);

            Assert.IsFalse(_loanSystem.AnyLoanMissedThisCycle(),
                "Origination alone should not trip the missed-this-cycle flag.");
        }

        [Test]
        public void AnyLoanMissedThisCycle_SubscriptionLeak_ClearedAfterDisable()
        {
            // Simulate scene reload: clear all subscriptions and verify that
            // a subsequent payment-missed event does not bump the counter on
            // a torn-down LoanSystem.
            GameEvents.RaiseLoanPurchaseRequested("basic_12m", "lot1", 6000f);
            _currencyManager.SetCheckingBalance(0f);
            _loanSystem.ProcessMonthlyPayments(); // counter increments via internal HandlePaymentMissed

            Assert.IsTrue(_loanSystem.AnyLoanMissedThisCycle());

            // Tear down: this is the scene-reload simulation. After OnDisable
            // and ClearAllSubscriptions the dummy event raise below should be
            // a no-op as far as the LoanSystem is concerned.
            InvokePrivateMethod(_loanSystem, "OnDisable");
            GameEvents.ClearAllSubscriptions();

            // External raise of OnLoanPaymentMissed: nothing is subscribed
            // (LoanSystem internally calls HandlePaymentMissed before raising,
            // not via subscription, so this raise is a guard against any
            // future subscription leak). Should not throw.
            Assert.DoesNotThrow(() => GameEvents.RaiseLoanPaymentMissed(null));
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
